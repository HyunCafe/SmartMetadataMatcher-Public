#!/usr/bin/env python3
"""Read-only source consistency check for Smart Metadata Matcher.

No network access, Git writes, Playnite access, tokens, or file mutations.
"""
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

root = Path(__file__).resolve().parent.parent
problems = []
manifest = (root / "extension.yaml").read_text(encoding="utf-8-sig")
plugin = (root / "Source/Plugin.cs").read_text(encoding="utf-8-sig")
yaml_version = re.search(r"(?m)^Version:\s*(\d+\.\d+\.\d+)\s*$", manifest)
assembly = re.search(r'AssemblyVersion\("([0-9.]+)"\)', plugin)
file_version = re.search(r'AssemblyFileVersion\("([0-9.]+)"\)', plugin)
if not yaml_version or not assembly or not file_version:
    problems.append("Missing manifest/assembly version declaration")
elif assembly.group(1) != yaml_version.group(1)+".0" or file_version.group(1) != assembly.group(1):
    problems.append("Manifest, assembly and file versions differ")

for file, kind in (("CatalogWindow.cs", "CatalogWindow"), ("PreviewWindow.cs", "PreviewWindow")):
    content = (root / "Source" / file).read_text(encoding="utf-8-sig")
    if f"typeof({kind}).Assembly.GetName().Version" not in content:
        problems.append(f"{file}: window must use runtime assembly version")

for project, folder, expected_set in (
    ("SmartMetadataMatcher.csproj", "Source", set(p.as_posix() for p in (root/"Source").glob("*.cs"))),
    ("Tests/NameRulesTests.csproj", "Tests", set(p.as_posix() for p in (root/"Tests").glob("*Tests.cs")))
):
    try:
        tree = ET.parse(root / project)
        includes = [(root / Path(project).parent / elem.attrib["Include"]).resolve()
                    for elem in tree.iter("Compile") if "Include" in elem.attrib]
        missing = [path for path in includes if not path.is_file()]
        if missing:
            problems.append(f"{project}: references missing source files")
        relative = {p.as_posix() for p in includes if p.parent.name == folder}
        if relative != expected_set:
            problems.append(f"{project}: compile inventory differs from {folder} folder")
    except (ET.ParseError, OSError) as ex:
        problems.append(f"{project}: {type(ex).__name__}")

program = (root / "Tests/NameRulesTests.cs").read_text(encoding="utf-8-sig")
for path in (root/"Tests").glob("*Tests.cs"):
    if path.name == "NameRulesTests.cs":
        continue
    # Every suite should be invoked by the main offline test runner.
    stem = path.stem
    if stem + ".Run();" not in program:
        problems.append(f"Test suite not invoked: {stem}")

# Review navigation invariant: draft edits must preserve the current filter.
# IGDB identity adoption is the only context-menu operation that may reset it.
review_source = (root / "Source/CatalogWindow.cs").read_text(encoding="utf-8-sig")
checks = (
    "Window reviewOwner,Action<bool> refreshReview",
    "refreshReview(false);else UpdatePreview();",
    "refreshReview==null?null:new Action(()=>refreshReview(true))",
    "},dialog,identityChanged=>{",
    "if(!identityChanged)",
    "selected.RenameHandling=DraftTitlePreview(selected.SourceRow);",
)
if not all(fragment in review_source for fragment in checks):
    problems.append("Review context actions must distinguish draft edits from IGDB identity changes")
if not re.search(r"if\(!identityChanged\)\s*\{[^{}]*?refreshRows\(\);[^{}]*?return;\s*\}", review_source, re.S):
    problems.append("Draft edits must refresh only the current filtered review view")

if problems:
    for problem in problems:
        print("STOP:", problem)
    sys.exit(1)
print("PASS: source versions, dynamic UI titles, compile inventories and test runner wiring")
print("READ-ONLY audit; does not certify binary security or Playnite runtime behavior")
