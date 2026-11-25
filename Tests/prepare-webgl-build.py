"""Prepare a source-only build copy without locking the user's open Unity project."""
from pathlib import Path
import hashlib
import json
import shutil

root = Path(__file__).resolve().parents[1]
target = root / "LocalChecks/WebGLProject"
target.mkdir(parents=True, exist_ok=True)
for name in ("Assets", "ProjectSettings"):
    shutil.copytree(root / name, target / name, dirs_exist_ok=True)
(target / "Packages").mkdir(exist_ok=True)
manifest = json.loads((root / "Packages/manifest.json").read_text())
for name, version in manifest["dependencies"].copy().items():
    cached = root / "Library/PackageCache" / f"{name}@{version}"
    if cached.is_dir():
        manifest["dependencies"][name] = "file:" + cached.as_posix()
(target / "Packages/manifest.json").write_text(json.dumps(manifest, indent=2))
snapshot = {}
for directory in ("Assets", "ProjectSettings"):
    for path in (root / directory).rglob("*"):
        if path.is_file() and (path.suffix == ".meta" or directory == "ProjectSettings"):
            snapshot[path.relative_to(root).as_posix()] = hashlib.sha256(path.read_bytes()).hexdigest()
(root / "LocalChecks/webgl-source-snapshot.json").write_text(json.dumps(snapshot, indent=2))
print(f"Prepared {target}")
