from pathlib import Path
import re
import subprocess

root = Path(__file__).resolve().parents[1]
metadata = subprocess.check_output(
    ["rg", "--no-heading", "--no-filename", "^guid: ", "Assets", "Library/PackageCache", "-g", "*.meta"],
    cwd=root, text=True, encoding="utf-8")
guids = set(re.findall(r"guid: (\w+)", metadata))
files = list((root / "Assets/Scenes").glob("*.unity")) + list((root / "Assets/Prefabs").rglob("*.prefab"))
failures = []
for path in files:
    text = path.read_text(encoding="utf-8-sig")
    ids = set(re.findall(r"^--- !u!\d+ &(-?\d+)", text, re.M))
    missing_guids = {g for g in re.findall(r"guid: (\w+)", text) if g not in guids and not g.startswith("000000")}
    missing_ids = {i for i in re.findall(r"\{fileID: (-?\d+)\}", text) if i != "0" and i not in ids}
    if missing_guids or missing_ids:
        failures.append((str(path.relative_to(root)), sorted(missing_guids), sorted(missing_ids)))
if failures:
    for path, missing_guids, missing_ids in failures:
        print(path, "missing GUIDs:", missing_guids, "missing local IDs:", missing_ids)
    raise SystemExit(1)
print(f"PASS: GUIDs and local object references in {len(files)} scenes/prefabs")
