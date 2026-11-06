#!/usr/bin/env python3
"""Read-only, high-confidence v0.5.0 private release-candidate guard.

Not a source security audit, transitive dependency audit or binary certification.
Does not access network, Git, Playnite, credentials, or modify files.
"""
from pathlib import Path
import re
import sys
import tempfile
import shutil
import xml.etree.ElementTree as ET

EXPECTED_VERSION = "0.5.0"
EXPECTED_GUID = "11D77564-F56F-5BF3-9CBB-7BB0CB427537"
EXPECTED_PACKAGES = {"PlayniteSDK": "[6.18.0]", "Microsoft.NETFramework.ReferenceAssemblies.net462": "[1.0.3]"}
PRIVATE_PATTERNS = (
    re.compile(r"(?i)\b[a-z]:\\(?:users|documents and settings)\\[^\s\"']+"),
    re.compile(r"(?i)/home/[a-z0-9_.-]+/"),
    re.compile(r"(?i)/users/[a-z0-9_.-]+/"),
    re.compile(r"(?i)gh[pousr]_[a-z0-9]{30,}"),
    re.compile(r"(?i)\b(?:sk-[a-z0-9]{24,}|AKIA[0-9A-Z]{16})\b"),
)


def audit(root):
    root = Path(root)
    problems = []
    try:
        manifest = (root / "extension.yaml").read_text(encoding="utf-8-sig")
        plugin = (root / "Source/Plugin.cs").read_text(encoding="utf-8-sig")
        project = ET.parse(root / "SmartMetadataMatcher.csproj").getroot()
        test_project = ET.parse(root / "Tests/NameRulesTests.csproj").getroot()
        build = (root / "Tools/Build-Preview.ps1").read_text(encoding="utf-8-sig")
        ignore = (root / ".gitignore").read_text(encoding="utf-8-sig")
        notes = (root / "RELEASE_READINESS.md").read_text(encoding="utf-8-sig")
    except (OSError, ET.ParseError, UnicodeError) as err:
        return ["Missing or unreadable source/project input: " + str(type(err).__name__)]
    if not re.search(r"(?m)^Version:\s*"+re.escape(EXPECTED_VERSION)+r"\s*$", manifest):
        problems.append("RC manifest version differs")
    if not re.search(r"(?m)^Id:\s*"+EXPECTED_GUID+r"\s*$", manifest):
        problems.append("Playnite plugin GUID changed")
    if "Name: Smart Metadata Matcher" not in manifest or "Author: HyunCafe" not in manifest:
        problems.append("Unexpected extension identity")
    if 'AssemblyVersion("0.5.0.0")' not in plugin or 'AssemblyFileVersion("0.5.0.0")' not in plugin:
        problems.append("Assembly version is not aligned with RC")
    if 'AssemblyCompany("HyunCafe")' not in plugin:
        problems.append("Unexpected assembly publisher")
    if 'private RC2' not in (root / "Source/CatalogWindow.cs").read_text(encoding="utf-8-sig"):
        problems.append("Dashboard private RC2 label missing")
    def properties(tree):
        return {n.tag: (n.text or "").strip() for n in tree.iter() if n.tag in
                ("TargetFramework", "GenerateAssemblyInfo", "Deterministic", "Optimize", "DebugType", "DebugSymbols")}
    p = properties(project)
    for key,value in (('TargetFramework','net462'),('GenerateAssemblyInfo','false'),('Deterministic','true'),('Optimize','true')):
        if p.get(key)!=value: problems.append("Unsafe/unexpected build property: " + key)
    if not any(n.tag=='DebugType' and (n.text or '').strip()=='none' and
               n.attrib.get('Condition') == "'$(Configuration)' == 'Release'" for n in project.iter()):
        problems.append("Release PDB suppression not configured")
    if not any(n.tag=='DebugSymbols' and (n.text or '').strip()=='false' for n in project.iter()):
        problems.append("Release debug symbols suppression not configured")
    for tree,label,wanted in ((project,"extension",EXPECTED_PACKAGES),
                              (test_project,"tests",{"Microsoft.NETFramework.ReferenceAssemblies.net462":"[1.0.3]"})):
        packages = tree.findall('.//PackageReference')
        actual = {n.attrib.get("Include"):n.attrib.get("Version") for n in packages}
        if len(packages)!=len(wanted) or actual!=wanted:
            problems.append("Unexpected/unpinned NuGet dependency in "+label)
        if any(n.tag in ('Exec','Import','UsingTask') for n in tree.iter()):
            problems.append("Custom MSBuild executable/import in "+label)
    for required in ("Tools/Verify-RC.py", "Tools/Verify-Package.py", "Tools/Test-Package-Integrity.py"):
        if required not in build or not (root / required).is_file():
            problems.append("Missing build-time verification: " + required)
    if 'private-rc2-' not in build:
        problems.append("Release package must be labeled private RC2")
    for pattern in ("*.csv", "*.log", "*.env", "*.pext", "*.pdb", "bin/", "obj/", "__pycache__/", "*.py[cod]"):
        if pattern not in ignore:
            problems.append("Privacy/build exclusion missing: " + pattern)
    if "25 independently strict-ready" not in notes or "independent undo" not in notes:
        problems.append("Release readiness document omits required gate")
    for p in list((root / 'Source').glob('*.cs')) + [root/'Tools/Build-Preview.ps1']:
        try: content=p.read_text(encoding='utf-8-sig')
        except (OSError, UnicodeError):
            problems.append("Unreadable source: " + p.name)
            continue
        for expr in PRIVATE_PATTERNS:
            if expr.search(content):
                problems.append("Potential private path or secret in " + p.name)
                break
    return problems


def self_test():
    # Pure pattern checks + a disposable full source mirror. No modifications to project.
    root = Path(__file__).resolve().parent.parent
    assert not audit(root), audit(root)
    for sample in (r'C:\Users\example\Documents\private.txt', '/home/privateuser/secrets',
                   'ghp_'+'a'*35, 'AKIA'+'A'*16):
        assert any(p.search(sample) for p in PRIVATE_PATTERNS), sample
    with tempfile.TemporaryDirectory(prefix='smm-rc-audit-') as folder:
        shadow=Path(folder)
        for relative in (".gitignore","extension.yaml","RELEASE_READINESS.md","SmartMetadataMatcher.csproj",
                         "Tests/NameRulesTests.csproj","Source/Plugin.cs","Source/CatalogWindow.cs",
                         "Tools/Build-Preview.ps1","Tools/Verify-RC.py","Tools/Verify-Package.py",
                         "Tools/Test-Package-Integrity.py"):
            target=shadow/relative;target.parent.mkdir(parents=True,exist_ok=True)
            shutil.copyfile(root/relative,target)
        assert not audit(shadow), audit(shadow)
        p=shadow/'extension.yaml';p.write_text(p.read_text().replace('Version: 0.5.0','Version: 0.4.32'))
        assert any('version' in x for x in audit(shadow))
        p.write_text(p.read_text().replace('Version: 0.4.32','Version: 0.5.0'))
        p=shadow/'Source/Plugin.cs';p.write_text(p.read_text()+ '\n// C:\\Users\\privateuser\\secrets.txt\n')
        assert any('private path' in x for x in audit(shadow))
        p.write_text(p.read_text().replace('// C:\\Users\\privateuser\\secrets.txt',''))
        p=shadow/'SmartMetadataMatcher.csproj';p.write_text(p.read_text().replace('Version="[6.18.0]"','Version="6.18.0"'))
        assert any('NuGet dependency' in x for x in audit(shadow))
    print('PASS: RC source/privacy guard positive and negative fixtures')

if __name__ == '__main__':
    if len(sys.argv)!=2 or sys.argv[1] not in ('--check','--self-test'):
        raise SystemExit('Usage: python Tools/Verify-RC.py --check|--self-test')
    if sys.argv[1]=='--self-test': self_test()
    else:
        issues=audit(Path(__file__).resolve().parent.parent)
        if issues:
            for item in issues: print('STOP:',item)
            sys.exit(1)
        print('PASS: private RC manifest, pinned package refs, release mode, source privacy and build gates')
        print('LIMIT: does not verify transitive dependencies, compiled binary behavior or Playnite runtime')
