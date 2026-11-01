from pathlib import Path
from collections import Counter
from PIL import Image
import sys

ROOT = Path(__file__).resolve().parents[1]
TEX = ROOT / "Textures" / "Items"
HIGH = ROOT / "ArtSource" / "Items" / "16x"
MAX_COLORS = 32

EXPECTED = {
    "elem_electrolysis_cell.png": (48, 48),
    "elem_hydrogen_fuel_cell.png": (16, 32),
    "elem_laboratory_bay.png": (64, 64),
    "elem_hydrogen_cylinder.png": (32, 48),
    "elem_oxygen_cylinder.png": (32, 48),
    "elem_nitrogen_cylinder.png": (32, 48),
    "elem_chlorine_cylinder.png": (32, 48),
    "elem_iron_oxide.png": (48, 32),
    "elem_silica.png": (48, 32),
    "elem_fertilizer.png": (32, 32),
}
for state in range(1, 5):
    EXPECTED[f"elem_laboratory_bay_{state}.png"] = (64, 64)
for name in (
    "elem_coke", "elem_salt", "elem_hydrogen_peroxide",
    "elem_silicon", "elem_mod_chip",
):
    EXPECTED[f"{name}.png"] = (16, 16)
for name in (
    "elem_sodium_hydroxide", "elem_hydrochloric_acid", "elem_ammonia", "elem_nitric_acid",
    "elem_brine", "elem_bleaching_powder",
):
    EXPECTED[f"{name}.png"] = (16, 32)
for name in (
    "elem_air_separator", "elem_oxyhydrogen_welder", "elem_rust_extractor",
    "elem_chemical_reactor",
    "elem_arc_furnace", "elem_chip_fab",
):
    EXPECTED[f"{name}.png"] = (48, 48)

failures = []
print(f"{'file':38} {'runtime':9} {'colors':6} {'alpha':6} {'high-res':14} status")
for name, (w, h) in EXPECTED.items():
    path = TEX / name
    if not path.exists():
        print(f"{name:38} {'MISSING':9}")
        failures.append(f"{name}: missing runtime texture")
        continue
    im = Image.open(path).convert("RGBA")
    colors = len(Counter((r, g, b) for r, g, b, a in im.getdata() if a > 0))
    alpha_values = set(im.getchannel("A").getdata())
    hard_alpha = alpha_values.issubset({0, 255})
    size_ok = im.size == (w, h)

    high = HIGH / name
    high_ok = False
    high_note = "missing"
    if high.exists():
        him = Image.open(high)
        high_ok = (him.size == (w * 16, h * 16) and
                   him.convert("RGBA").resize((w, h), Image.Resampling.NEAREST).tobytes() == im.tobytes())
        high_note = f"{him.size[0]}x{him.size[1]}"

    status = "OK"
    if not size_ok:
        status = "SIZE"
        failures.append(f"{name}: size {im.size} != {(w, h)}")
    if not hard_alpha:
        status += "/ALPHA"
        failures.append(f"{name}: soft alpha")
    if colors > MAX_COLORS:
        status += "/COLORS"
        failures.append(f"{name}: {colors} colors > {MAX_COLORS}")
    if not high_ok:
        status += "/NOSOURCE"
        failures.append(f"{name}: high-res source missing, wrong size, or does not reproduce runtime ({high_note})")
    # The game's mod-sprite shape loader samples the center of each 16x16 cell.
    # Gas tanks set a full 2x3 shape in code because their valves do not fill both
    # cells of the first row. Minerals use the native ore's irregular mask below.
    uses_explicit_shape = (name.startswith("elem_laboratory_bay")
                           or name.endswith("_cylinder.png")
                           or name in {"elem_iron_oxide.png", "elem_silica.png"})
    if not uses_explicit_shape and size_ok:
        missing_cells = [(x // 16, y // 16) for y in range(8, h, 16) for x in range(8, w, 16)
                         if im.getpixel((x, y))[3] == 0]
        if missing_cells:
            status += "/SHAPE"
            failures.append(f"{name}: transparent sampled inventory cells {missing_cells}")

    print(f"{name:38} {im.size[0]}x{im.size[1]:<4} {colors:<6} {str(hard_alpha):6} {high_note:14} {status}")

# Minerals retain the vanilla ore's landscape 3x2 bounds and irregular center mask.
native_ore_mask = (False, True, False, True, True, True)
for name in ("elem_iron_oxide.png", "elem_silica.png"):
    im = Image.open(TEX / name).convert("RGBA")
    mask = tuple(im.getpixel((x, y))[3] > 0 for y in (8, 24) for x in (8, 24, 40))
    if mask != native_ore_mask:
        failures.append(f"{name}: native 3x2 ore mask changed: {mask}")

print()
if failures:
    print("FAIL:")
    for failure in failures:
        print(" -", failure)
    sys.exit(1)
print("PASS: textures have correct size, hard alpha, low palette, matching 16x source and valid sampled inventory cells.")
