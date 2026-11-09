#!/usr/bin/env python3
"""Disposable positive/negative package-integrity fixtures; no Playnite access."""
import importlib.util
import tempfile
from pathlib import Path
from zipfile import ZipFile

spec = importlib.util.spec_from_file_location("public_check", Path(__file__).with_name("Public-Release-Check.py"))
check = importlib.util.module_from_spec(spec)
spec.loader.exec_module(check)
with tempfile.TemporaryDirectory() as directory:
    root = Path(directory)
    check.ROOT = root
    dll = root / "bin/Release/net462/SmartMetadataMatcher.dll"
    dll.parent.mkdir(parents=True)
    dll.write_bytes(b"fixture-build-bytes")
    (root / "extension.yaml").write_bytes(b"Version: 0.5.17\n")
    package = root / "candidate.pext"
    def pack(binary=b"fixture-build-bytes", manifest=b"Version: 0.5.17\n", extra=False):
        with ZipFile(package, "w") as archive:
            archive.writestr("SmartMetadataMatcher.dll", binary)
            archive.writestr("extension.yaml", manifest)
            if extra:
                archive.writestr("unexpected.txt", "fixture")
    pack()
    assert check.package(package) == []
    pack(binary=b"different-build")
    assert "Packaged DLL differs from built Release DLL" in check.package(package)
    pack(manifest=b"Version: 0.0.0\n")
    assert "Packaged YAML differs from source" in check.package(package)
    pack(extra=True)
    assert "Unexpected packaged members" in check.package(package)
    pack()
    dll.unlink()
    assert "Release DLL missing for byte comparison" in check.package(package)
print("PASS: package identity, altered DLL/YAML, unexpected members and missing-build fixtures")
