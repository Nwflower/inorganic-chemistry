import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
NEW = {
    "elem_iron_oxide", "elem_iron_ore", "elem_sponge_iron", "elem_coke", "elem_salt",
    "elem_brine", "elem_sodium_hydroxide", "elem_hydrochloric_acid", "elem_ammonia",
    "elem_nitric_acid", "elem_fertilizer", "elem_hydrogen_peroxide", "elem_bleaching_powder",
    "elem_water_tablet", "elem_silica", "elem_silicon", "elem_polysilicon", "elem_silicon_wafer",
    "elem_integrated_circuit", "elem_mod_chip", "elem_machine_frame", "elem_catalyst",
    "elem_air_separator", "elem_gas_filler", "elem_oxyhydrogen_welder", "elem_rust_extractor",
    "elem_coke_oven", "elem_reduction_furnace", "elem_blast_furnace", "elem_chemical_reactor",
    "elem_tablet_press", "elem_arc_furnace", "elem_crystal_puller", "elem_wafer_saw", "elem_chip_fab",
}

for relative, section in (("Data/items.json", "items"), ("Data/machines.json", "machines")):
    path = ROOT / relative
    text = path.read_text(encoding="utf-8")
    ids = {entry["id"] for entry in json.loads(text)[section] if entry.get("copySpriteFrom")}
    if ids != NEW & ids:
        raise RuntimeError(f"Unexpected id mismatch in {relative}: {ids}")
    lines = []
    current = None
    changed = 0
    for line in text.splitlines(keepends=True):
        match = re.search(r'"id"\s*:\s*"(elem_[^"]+)"', line)
        if match:
            current = match.group(1)
        if current in NEW and '"copySpriteFrom"' in line:
            line = re.sub(r'"copySpriteFrom"\s*:\s*"[^"]+"', f'"sprite": "{current}"', line)
            changed += 1
        lines.append(line)
    path.write_text("".join(lines), encoding="utf-8")
    print(f"{relative}: replaced {changed} vanilla sprite references")
