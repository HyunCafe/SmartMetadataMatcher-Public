#!/usr/bin/env python3
"""Read-only candidate source and package audit. NOT a security certification."""
import re,sys,hashlib,zipfile,xml.etree.ElementTree as ET
from pathlib import Path
ROOT=Path(__file__).resolve().parent.parent
REQUIRED=("README.md","CHANGELOG.md","LICENSE","SECURITY.md","PRIVACY.md","extension.yaml",
"SmartMetadataMatcher.csproj","Tests/NameRulesTests.csproj","Manifests/installer.yaml",
"Manifests/addon-database.yaml","Docs/RELEASE_GATE.md")
ID="11D77564-F56F-5BF3-9CBB-7BB0CB427537"
EXCLUDED={"bin","obj",".git",".vs","__pycache__"}
PII=[re.compile(r"(?i)[a-z]:\\Users\\[^\\\s]+"),re.compile(r"(?i)/home/[^/\s]+/"),
re.compile(r"(?i)(?:gh[pousr]_[a-z0-9]{30,}|sk-[a-z0-9]{24,}|AKIA[A-Z0-9]{16})")]
def check():
    issues=[]
    for name in REQUIRED:
        if not (ROOT/name).is_file():issues.append("Missing: "+name)
    if issues:return issues
    manifest=(ROOT/'extension.yaml').read_text()
    db=(ROOT/'Manifests/addon-database.yaml').read_text()
    installer=(ROOT/'Manifests/installer.yaml').read_text()
    versions=re.findall(r"(?m)^Version:\s*([0-9]+\.[0-9]+\.[0-9]+)\s*$",manifest)
    if versions!=["0.5.17"]:issues.append("Manifest release-candidate version mismatch")
    for name,body,expr in (("extension",manifest,r"(?m)^Id:\s*['\"]?([^\s'\"]+)"),
                           ("installer",installer,r"(?m)^AddonId:\s*['\"]?([^\s'\"]+)"),
                           ("database",db,r"(?m)^AddonId:\s*['\"]?([^\s'\"]+)")):
        found=re.search(expr,body)
        if not found or found.group(1)!=ID:issues.append("Inconsistent ID: "+name)
    if 'Type: GenericPlugin' not in manifest or 'Type: Generic' not in db:
        issues.append("Extension and add-on type mismatched")
    tree=ET.parse(ROOT/'SmartMetadataMatcher.csproj')
    for x in tree.findall('.//Compile'):
        name=x.attrib.get('Include','')
        if name and not (ROOT/name).is_file():issues.append('Missing compile source: '+name)
    for file in list((ROOT/'Source').rglob('*.cs'))+[ROOT/'README.md',ROOT/'SECURITY.md',ROOT/'PRIVACY.md']:
        txt=file.read_text(errors='replace')
        if any(p.search(txt) for p in PII):issues.append('Potential secret or personal path: '+str(file.relative_to(ROOT)))
    # Draft manifests must not accidentally be represented as already published.
    if not all('DRAFT ONLY' in (ROOT/f).read_text() for f in ('Manifests/installer.yaml','Manifests/addon-database.yaml')):
        issues.append('Manifest draft warning missing')
    return issues
def package(path):
    issues=[]
    with zipfile.ZipFile(path) as z:
        if z.testzip() is not None:issues.append('CRC failure')
        names=z.namelist()
        if len(names)!=2 or set(names)!={'SmartMetadataMatcher.dll','extension.yaml'}:
            issues.append('Unexpected packaged members')
        else:
            if z.read('extension.yaml')!=(ROOT/'extension.yaml').read_bytes():
                issues.append('Packaged YAML differs from source')
            dll=ROOT/'bin/Release/net462/SmartMetadataMatcher.dll'
            if not dll.is_file():
                issues.append('Release DLL missing for byte comparison')
            elif z.read('SmartMetadataMatcher.dll')!=dll.read_bytes():
                issues.append('Packaged DLL differs from built Release DLL')
    return issues
if __name__=='__main__':
    errs=check()
    if sys.argv[1:2]==['--package'] and len(sys.argv)==3:
        try:errs+=package(Path(sys.argv[2]))
        except Exception as ex:errs.append('Unreadable package: '+str(type(ex).__name__))
    elif len(sys.argv)!=1:
        sys.exit('Usage: python Tools/Public-Release-Check.py [--package file.pext]')
    if errs:
        for x in errs:print('STOP:',x)
        sys.exit(1)
    print('PASS: source inventory, identity, draft manifests, and targeted privacy checks.')
    print('LIMIT: no Windows compilation, dependency provenance, real Playnite recovery, or binary security certification.')
