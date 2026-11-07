#!/usr/bin/env python3
"""Local-only evidence gate for a PRIVATE Smart Metadata Matcher .pext.

Reads project source, the existing Release DLL/package, and Git metadata.
Does not read Playnite data, credentials, ROMs or journal files. Network-free.
A requested report is written only to a caller-selected path outside the source.
Not a binary-security certification or a live crash-recovery test.
"""
import argparse
import hashlib
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path
from zipfile import ZipFile, BadZipFile

VERSION_RE = re.compile(r"(?m)^Version:\s*(\d+\.\d+\.\d+)\s*$")
EXPECTED_PACKAGE = frozenset(("SmartMetadataMatcher.dll", "extension.yaml"))
BLOCKED_TRACKED_EXTENSIONS = frozenset((".pext", ".pdb", ".zip", ".log", ".csv", ".db", ".sqlite", ".dpapi", ".env"))


def sha256(data):
    return hashlib.sha256(data).hexdigest().upper()


def failure(message):
    raise ValueError(message)


def package_check(root, package, expected_digest):
    if not package.is_file() or package.suffix.lower() != ".pext":
        failure("A readable private .pext package is required")
    if not re.fullmatch(r"[a-fA-F0-9]{64}", expected_digest):
        failure("Expected package SHA-256 must be a 64-character hex digest")
    raw = package.read_bytes()
    digest = sha256(raw)
    if digest != expected_digest.upper():
        failure("Package checksum does not match the verified release log")
    try:
        with ZipFile(package) as z:
            infos = z.infolist()
            if len(infos) != 2 or set(x.filename for x in infos) != EXPECTED_PACKAGE:
                failure("Package must contain the approved DLL and manifest exactly once")
            if any(x.flag_bits & 1 for x in infos) or any(x.file_size > 32*1024*1024 or x.file_size < 1 for x in infos):
                failure("Encrypted, empty or oversized package member")
            if any((x.external_attr >> 16) & 0o170000 == 0o120000 for x in infos):
                failure("Symlink member forbidden")
            if z.testzip() is not None:
                failure("ZIP member CRC mismatch")
            dll = z.read("SmartMetadataMatcher.dll")
            yaml = z.read("extension.yaml")
    except (BadZipFile, OSError) as err:
        failure("Cannot verify package integrity: " + type(err).__name__)
    if dll[:2] != b"MZ":
        failure("Packaged DLL lacks Windows PE header")
    expected_dll = root / "bin/Release/net462/SmartMetadataMatcher.dll"
    expected_yaml = root / "extension.yaml"
    if not expected_dll.is_file() or dll != expected_dll.read_bytes():
        failure("Compiled Release DLL and packaged DLL differ")
    if not expected_yaml.is_file() or yaml != expected_yaml.read_bytes():
        failure("Tracked manifest and packaged manifest differ")
    return {"sha256": digest, "dll_sha256": sha256(dll), "manifest_sha256": sha256(yaml), "members": sorted(EXPECTED_PACKAGE)}


def project_check(root):
    manifest = (root / "extension.yaml").read_text(encoding="utf-8-sig")
    plugin = (root / "Source/Plugin.cs").read_text(encoding="utf-8-sig")
    match = VERSION_RE.search(manifest)
    if not match:
        failure("Missing version in extension manifest")
    ver = match.group(1)
    if not re.search(r'AssemblyVersion\("'+re.escape(ver)+r'\.0"\)', plugin):
        failure("Assembly version does not match manifest version")
    try:
        tree = ET.parse(root / "SmartMetadataMatcher.csproj")
        props = {str(x.tag).rsplit('}', 1)[-1]: (x.text or '').strip() for x in tree.iter()}
        packages = []
        for ref in tree.iter():
            if str(ref.tag).rsplit('}', 1)[-1] != "PackageReference":
                continue
            name, spec = ref.attrib.get("Include", ""), ref.attrib.get("Version", "")
            if not name or not re.fullmatch(r'\[\d+(?:\.\d+){1,3}\]', spec):
                failure("NuGet package reference is not precisely version-pinned")
            packages.append({"name": name, "version": spec.strip("[]")})
    except (ET.ParseError, OSError) as err:
        failure("Cannot inspect project references: " + type(err).__name__)
    if len(packages) != 2 or {p['name'] for p in packages} != {
        "PlayniteSDK", "Microsoft.NETFramework.ReferenceAssemblies.net462"}:
        failure("Unexpected package dependencies; review required")
    if props.get("TargetFramework") != "net462" or props.get("Deterministic", "").lower() != "true":
        failure("Target framework or deterministic build setting changed")
    if props.get("GenerateAssemblyInfo", "").lower() != "false" or props.get("Optimize", "").lower() != "true":
        failure("Release/assembly settings changed")
    return {"version": ver, "framework": "net462", "deterministic_setting": True, "declared_packages": packages}


def run_git(root, *args):
    result = subprocess.run(['git', *args], cwd=str(root), stdout=subprocess.PIPE,
                            stderr=subprocess.DEVNULL, text=True, check=True)
    return result.stdout.strip()


def git_check(root):
    if run_git(root, 'rev-parse', '--is-inside-work-tree') != 'true':
        failure('Project must be in a Git repository')
    head = run_git(root, 'rev-parse', 'HEAD')
    branch = run_git(root, 'branch', '--show-current')
    remote_names = run_git(root, 'remote')
    rollback = run_git(root, 'rev-list', '-n', '1', 'v0.5.0-private-stable')
    if branch != 'main' or remote_names or not rollback.startswith('c1b08d1'):
        failure('Private-only branch/remote/stable-rollback safety gate failed')
    index = run_git(root, 'ls-files', '-s')
    # -z is NUL-delimited, so strip() must not be used when inspecting filenames.
    raw = subprocess.run(['git', 'ls-files', '-z'], cwd=str(root),
                         stdout=subprocess.PIPE, stderr=subprocess.DEVNULL, check=True).stdout
    names = [x.decode('utf-8', errors='replace') for x in raw.split(b'\0') if x]
    for name in names:
        p = Path(name)
        if p.suffix.lower() in BLOCKED_TRACKED_EXTENSIONS or p.name.lower().startswith('.env'):
            failure('Disallowed private/build file is tracked in Git')
    if any(line.startswith('120000 ') for line in index.splitlines()):
        failure('Git tracked symlink requires an explicit review')
    return {'commit': head, 'branch': branch, 'remote_count': 0,
            'stable_rollback_commit': rollback, 'tracked_file_count': len(names),
            'clean': not bool(run_git(root, 'status', '--porcelain'))}


def perform(root, package, expected_digest):
    root = root.resolve()
    if not (root / 'SmartMetadataMatcher.csproj').is_file():
        failure('Expected Smart Metadata Matcher project root')
    project = project_check(root)
    pkg = package_check(root, package.resolve(), expected_digest)
    repo = git_check(root)
    return {'milestone': 'private-release-evidence', 'schema_version': 1,
            'product': 'Smart Metadata Matcher', 'plugin_version': project['version'],
            'package': pkg, 'project': project, 'git': repo,
            'acceptance': {
                'source_and_package_byte_identity': 'VERIFIED_LOCALLY',
                'git_local_only': 'VERIFIED_LOCALLY',
                'dependency_versions_declared_exact': 'VERIFIED_LOCALLY',
                'transitive_dependency_supply_chain': 'NOT_VERIFIED',
                'compiled_binary_security': 'NOT_VERIFIED',
                'real_playnite_process_crash_recovery': 'NOT_VERIFIED',
                'public_release_approved': False,
            }}


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--package', required=True, type=Path)
    ap.add_argument('--expected-package-sha256', required=True)
    ap.add_argument('--report', type=Path, default=None)
    args = ap.parse_args()
    root = Path(__file__).resolve().parent.parent
    report = perform(root, args.package, args.expected_package_sha256)
    if args.report is not None:
        dest = args.report.resolve()
        if root == dest or root in dest.parents:
            failure('Private report must be outside the source tree')
        if dest.exists():
            failure('Will not overwrite an existing private audit report')
        dest.parent.mkdir(parents=True, exist_ok=True)
        dest.write_text(json.dumps(report, indent=2, sort_keys=True)+'\n', encoding='utf-8')
    print('PASS: package SHA-256, exact built DLL/YAML identity, dependency pins, private Git provenance')
    print('PASS: plugin v'+report['plugin_version']+'; '+str(report['git']['tracked_file_count'])+' tracked files; no remote')
    print('GIT: '+report['git']['commit'][:12]+(' (clean)' if report['git']['clean'] else ' (expected patch pending)'))
    print('NOT VERIFIED: transitive dependencies, binary security, live crash recovery, public readiness')
    if args.report:
        print('REPORT: '+str(args.report))


if __name__ == '__main__':
    try:
        main()
    except (ValueError, OSError, subprocess.CalledProcessError) as err:
        print('STOP: release evidence audit failed: '+str(err), file=sys.stderr)
        sys.exit(1)
