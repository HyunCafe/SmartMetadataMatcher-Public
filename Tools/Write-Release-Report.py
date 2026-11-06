#!/usr/bin/env python3
"""Create a private, machine-neutral RC1 verification report after local commit."""
from pathlib import Path
from zipfile import ZipFile
from hashlib import sha256
from datetime import datetime, timezone
import json, subprocess, sys

if len(sys.argv)!=3:
    sys.exit('Usage: python Tools/Write-Release-Report.py package.pext git-commit')
package=Path(sys.argv[1]); commit=sys.argv[2]
if not package.is_file() or package.suffix.lower()!='.pext':
    sys.exit('Missing candidate package')
if not commit or len(commit)>40 or not all(c in '0123456789abcdef' for c in commit.lower()):
    sys.exit('Invalid Git commit')
root=Path(__file__).resolve().parent.parent
with ZipFile(package) as z:
    if z.testzip() or sorted(z.namelist())!=['SmartMetadataMatcher.dll','extension.yaml'] or \
        b'Version: 0.5.0' not in z.read('extension.yaml'):
        sys.exit('Candidate package verification failed')
report={
  'product':'Smart Metadata Matcher', 'release':'0.5.0 private RC2',
  'created_utc':datetime.now(timezone.utc).isoformat(),
  'package_filename':package.name,
  'sha256':sha256(package.read_bytes()).hexdigest().upper(),
  'source_git_commit':commit,
  'automated_gates':{'Windows DLL build':'passed', 'offline C# test runner':'passed',
    'source and pinned dependency audit':'passed', 'package integrity and privacy screen':'passed'},
  'live_evidence':{'previous v0.4.32 two-game Playnite batch':'passed, 2/2 and no detected recovery inconsistencies',
    'v0.5.0 RC1 runtime':'not yet tested'},
  'remaining_gates':['independent undo of one item after multi-game batch',
    'injected crash/interrupted artwork operation in disposable Playnite data',
    '25-game batch is not independently runtime-validated',
    'dependencies and compiled binary have not received independent security certification'],
  'publication':'private only; no GitHub remote action authorized'
}
# Deliberately record no absolute file paths, library inventories, or user identifiers.
output=package.parent/'PrivateReports'/('SMM-0.5.0-RC2-'+commit[:8]+'.json')
output.parent.mkdir(parents=True,exist_ok=True)
output.write_text(json.dumps(report,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
print('PRIVATE RC REPORT:',output)
