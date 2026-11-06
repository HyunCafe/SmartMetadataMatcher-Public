#!/usr/bin/env python3
"""Offline negative/positive fixtures for Verify-Package (no Playnite or network)."""
from pathlib import Path
from tempfile import TemporaryDirectory
from zipfile import ZipFile, ZIP_DEFLATED
import importlib.util
import sys
import warnings

# Keep the working Git tree clean: this fixture imports the verifier by path.
sys.dont_write_bytecode = True

p=Path(__file__).with_name('Verify-Package.py')
spec=importlib.util.spec_from_file_location('smm_package_check',p)
module=importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
checks=0


def run(case,expected_pass,root,files):
    global checks
    archive=root/'preview.pext'
    with warnings.catch_warnings():
        warnings.simplefilter('ignore',UserWarning)
        with ZipFile(archive,'w',compression=ZIP_DEFLATED) as z:
            for key,value in files:
                z.writestr(key,value)
    try:
        module.verify(archive,root)
        passed=True
    except ValueError:
        passed=False
    checks+=1
    if passed!=expected_pass:
        raise AssertionError(case+': expected '+str(expected_pass)+', got '+str(passed))


with TemporaryDirectory(prefix='smm-qa-') as tmp:
    root=Path(tmp)
    path=root/'bin/Release/net462'
    path.mkdir(parents=True)
    dll=b'MZ'+b'\x00'*98+b'clean-version'
    yaml=b'Id: sample\nVersion: 0.4.32\n'
    (path/'SmartMetadataMatcher.dll').write_bytes(dll)
    (root/'extension.yaml').write_bytes(yaml)
    original=[('SmartMetadataMatcher.dll',dll),('extension.yaml',yaml)]
    run('correct package',True,root,original)
    run('extra member',False,root,original+[('private.json',b'{}')])
    run('duplicate member',False,root,original+[('extension.yaml',yaml)])
    run('missing member',False,root,original[:1])
    run('modified DLL',False,root,[('SmartMetadataMatcher.dll',dll+b'?'),('extension.yaml',yaml)])
    run('modified YAML',False,root,[('SmartMetadataMatcher.dll',dll),('extension.yaml',yaml+b'bad')])
    bad=b'MZ'+b'c:\\Users\\someone\\private.ini'
    (path/'SmartMetadataMatcher.dll').write_bytes(bad)
    run('absolute user path',False,root,[('SmartMetadataMatcher.dll',bad),('extension.yaml',yaml)])
    (path/'SmartMetadataMatcher.dll').write_bytes(b'not a PE')
    run('not a DLL',False,root,[('SmartMetadataMatcher.dll',b'not a PE'),('extension.yaml',yaml)])
print('PASS:',checks,'package-integrity positive and negative fixtures; no network or Playnite access')
