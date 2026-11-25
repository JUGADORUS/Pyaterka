"""Copy only release importer/player settings back, guarding concurrent source edits."""
from pathlib import Path
import hashlib
import json
import re
import shutil

root = Path(__file__).resolve().parents[1]
copy = root / "LocalChecks/WebGLProject"
snapshot = json.loads((root / "LocalChecks/webgl-source-snapshot.json").read_text())
paths = [
    "Assets/Materials/asphalt.jpg.meta", "Assets/Materials/rostov.png.meta",
    "Assets/Materials/5opka.png.meta", "Assets/Materials/NoExit.png.meta",
    "Assets/Materials/damageVignette.png.meta", "Assets/dollar.png.meta",
    "Assets/clock.png.meta", "Assets/heart.png.meta",
    *[p.relative_to(root).as_posix() for p in (root / "Assets/Sounds").glob("*.meta")],
]
pending = []
for relative in paths:
    source, built = root / relative, copy / relative
    if not built.is_file() or source.read_bytes() == built.read_bytes():
        continue
    if hashlib.sha256(source.read_bytes()).hexdigest() != snapshot.get(relative):
        raise RuntimeError(f"Source changed during build; inspect before syncing: {relative}")
    pending.append((source, built))
for source, built in pending:
    shutil.copy2(built, source)
    print(source.relative_to(root))

# Merge WebGL entries only: preserve settings for all other platforms.
settings_path = root / "ProjectSettings/ProjectSettings.asset"
settings = settings_path.read_text()
built_settings = (copy / "ProjectSettings/ProjectSettings.asset").read_text()
for key in ("scriptingBackend", "il2cppCodeGeneration", "managedStrippingLevel"):
    pattern = rf"(?m)^  {key}:[^\n]*(?:\n    [^\n]*)*"
    built_block = re.search(pattern, built_settings).group()
    value = re.search(r"(?m)^    WebGL: (\d+)$", built_block).group(1)
    block = re.search(pattern, settings).group()
    if re.search(r"(?m)^    WebGL:", block):
        updated = re.sub(r"(?m)^    WebGL: .*", f"    WebGL: {value}", block)
    else:
        updated = block.replace(": {}", ":") + f"\n    WebGL: {value}"
    settings = settings.replace(block, updated, 1)
if settings != settings_path.read_text():
    settings_path.write_text(settings)
    print("Merged WebGL player settings")
print(f"Synced {len(pending)} release settings files; original source media untouched")
