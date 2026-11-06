#!/usr/bin/env python3
"""Check the exact built bytes in a PRIVATE Playnite .pext (no network or writes).

Verifies package provenance within the local build only. Does NOT authenticate
third-party dependencies, compiled binary safety or Playnite runtime behavior.
"""
from hashlib import sha256
from pathlib import Path
from zipfile import ZipFile, BadZipFile
import re
import sys

EXPECTED = {'SmartMetadataMatcher.dll', 'extension.yaml'}
PRIVATE_PATTERNS = (
    re.compile(rb'(?i)[a-z]:[\\/](?:users|documents and settings)[\\/][^\x00\s]{2,}'),
    re.compile(rb'(?i)/home/[a-z0-9_.-]+/'),
    re.compile(rb'(?i)/users/[a-z0-9_.-]+/'),
    re.compile(rb'(?i)gh[pousr]_[a-z0-9]{30,}'),
)


def verify(package, source_root):
    package = Path(package)
    source_root = Path(source_root)
    if not package.is_file() or package.suffix.lower() != '.pext':
        raise ValueError('Expected an existing .pext extension')
    with ZipFile(package) as z:
        infos = z.infolist()
        names = [x.filename for x in infos]
        if len(names) != len(EXPECTED) or set(names) != EXPECTED:
            raise ValueError('Package must contain only the DLL and extension.yaml, exactly once')
        if any(x.flag_bits & 1 for x in infos):
            raise ValueError('Encrypted ZIP entries are forbidden')
        if any(x.file_size <= 0 or x.file_size > 32 * 1024 * 1024 for x in infos):
            raise ValueError('Empty or oversized package entry')
        if any(x.create_system == 3 and (x.external_attr >> 16) & 0o170000 == 0o120000
               for x in infos):
            raise ValueError('Symlink entry is forbidden')
        if z.testzip() is not None:
            raise ValueError('Package CRC check failed')
        for name in sorted(EXPECTED):
            packed = z.read(name)
            source = (source_root / ('bin/Release/net462/' if name.endswith('.dll') else '') / name)
            if not source.is_file() or packed != source.read_bytes():
                raise ValueError('Packaged bytes differ from just-built source: ' + name)
            for marker in PRIVATE_PATTERNS:
                if marker.search(packed):
                    raise ValueError('Possible private absolute path or credential marker in: ' + name)
        if z.read('SmartMetadataMatcher.dll')[:2] != b'MZ':
            raise ValueError('Extension payload is not a Windows PE binary')
    return sha256(package.read_bytes()).hexdigest().upper()


def main():
    if len(sys.argv) != 2:
        raise ValueError('Usage: python Tools/Verify-Package.py path/to/build.pext')
    root = Path(__file__).resolve().parent.parent
    digest = verify(sys.argv[1], root)
    print('PASS: exact DLL/YAML package bytes, ZIP inventory, CRC and private-path screening')
    print('PACKAGE SHA256:', digest)
    print('READ-ONLY check; not a compiled-binary security certification')


if __name__ == '__main__':
    try:
        main()
    except (ValueError, OSError, BadZipFile) as e:
        print('STOP: packaged extension verification failed: ' + str(e), file=sys.stderr)
        sys.exit(1)
