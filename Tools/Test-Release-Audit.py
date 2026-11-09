#!/usr/bin/env python3
"""Offline fixture tests for private-release audit; no user data."""
import importlib.util
from pathlib import Path
from tempfile import TemporaryDirectory
from zipfile import ZipFile, ZIP_DEFLATED
import hashlib
import sys

p = Path(__file__).with_name('Audit-Release-Evidence.py')
spec = importlib.util.spec_from_file_location('smm_release_audit', p)
a = importlib.util.module_from_spec(spec)
spec.loader.exec_module(a)


def expect_bad(fn):
    try: fn()
    except ValueError: return
    raise AssertionError('Expected refusal')


def fixture(directory):
    root = Path(directory)
    root.mkdir(parents=True, exist_ok=True)
    (root/'Source').mkdir()
    (root/'bin/Release/net462').mkdir(parents=True)
    yaml = b'Id: DEMO\nVersion: 0.5.8\nModule: SmartMetadataMatcher.dll\n'
    dll = b'MZ' + b'\0'*256
    (root/'extension.yaml').write_bytes(yaml)
    (root/'Source/Plugin.cs').write_text('[assembly: AssemblyVersion("0.5.8.0")]')
    (root/'SmartMetadataMatcher.csproj').write_text('''<Project><PropertyGroup><TargetFramework>net462</TargetFramework><Deterministic>true</Deterministic><GenerateAssemblyInfo>false</GenerateAssemblyInfo><Optimize>true</Optimize></PropertyGroup><ItemGroup><PackageReference Include="PlayniteSDK" Version="[6.18.0]"/><PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies.net462" Version="[1.0.3]" PrivateAssets="all"/></ItemGroup></Project>''')
    (root/'bin/Release/net462/SmartMetadataMatcher.dll').write_bytes(dll)
    return root, yaml, dll


def make_zip(root, contents):
    target = root.parent/'candidate.pext'
    with ZipFile(target, 'w', ZIP_DEFLATED) as z:
        for name, data in contents: z.writestr(name, data)
    return target, hashlib.sha256(target.read_bytes()).hexdigest()


def main():
    count = 0
    with TemporaryDirectory() as d:
        root, yml, dll = fixture(Path(d)/'project')
        a.project_check(root); count+=1
        package, hashval = make_zip(root, [('SmartMetadataMatcher.dll',dll),('extension.yaml',yml)])
        a.package_check(root,package,hashval); count+=1
        expect_bad(lambda:a.package_check(root,package,'0'*64)); count+=1
        package,h=make_zip(root,[('SmartMetadataMatcher.dll',dll),('extension.yaml',yml),('private.log',b'x')])
        expect_bad(lambda:a.package_check(root,package,h));count+=1
        package,h=make_zip(root,[('SmartMetadataMatcher.dll',dll),('extension.yaml',yml),('extension.yaml',yml)])
        expect_bad(lambda:a.package_check(root,package,h));count+=1
        package,h=make_zip(root,[('SmartMetadataMatcher.dll',dll+b'changed'),('extension.yaml',yml)])
        expect_bad(lambda:a.package_check(root,package,h));count+=1
        package,h=make_zip(root,[('SmartMetadataMatcher.dll',dll),('extension.yaml',yml+b'mismatch')])
        expect_bad(lambda:a.package_check(root,package,h));count+=1
        package,h=make_zip(root,[('SmartMetadataMatcher.dll',b'ELF'),('extension.yaml',yml)])
        expect_bad(lambda:a.package_check(root,package,h));count+=1
        f = root/'SmartMetadataMatcher.csproj'
        original = f.read_text()
        f.write_text(original.replace('[6.18.0]','6.18.0'))
        expect_bad(lambda:a.project_check(root));count+=1
        f.write_text(original.replace('net462</TargetFramework>','net8.0</TargetFramework>'))
        expect_bad(lambda:a.project_check(root));count+=1
        f.write_text(original)
        (root/'extension.yaml').write_text('Version: 0.5.7\n')
        expect_bad(lambda:a.project_check(root));count+=1
    print('PASS: '+str(count)+' private release audit positive/negative offline fixtures; no network or Playnite data')

if __name__=='__main__':
    try:main()
    except Exception as err:
        print('STOP: release audit self-tests: '+type(err).__name__+': '+str(err),file=sys.stderr)
        sys.exit(1)
