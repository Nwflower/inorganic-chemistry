"""Produce compact versions of the generated assets from archived source images.

Archive input files are kept under the work order; running this does not fetch assets
or modify the game directory. The 16x sources are exact nearest-neighbor pixel art.
"""
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
WORK = ROOT / "ArtSource/WorkOrders/completed/2026-10-31-compact-stations"
INPUT = WORK / "originals"
OUT = ROOT / "Textures/Items"
HIGH = ROOT / "ArtSource/Items/16x"
PREVIEW = ROOT / "ArtSource/Previews"

MACHINES = (
    "elem_electrolysis_cell", "elem_air_separator", "elem_oxyhydrogen_welder",
    "elem_rust_extractor", "elem_reduction_furnace",
    "elem_blast_furnace", "elem_chemical_reactor",
    "elem_arc_furnace", "elem_chip_fab",
)
SMALL_MACHINE_SIZES = {
    "elem_rust_extractor": (32, 16),
    "elem_chip_fab": (32, 16),
    "elem_blast_furnace": (16, 32),
}
CHEMICALS = (
    "elem_hydrochloric_acid", "elem_nitric_acid", "elem_sodium_hydroxide", "elem_ammonia",
)


def pixel_chemical_bottle(name):
    """Reuse the vanilla oval-top bottle silhouette and draw a discrete contents band."""
    refs = ROOT / "ArtSource/References/native"
    image = Image.open(refs / "med_bottle_red.png").convert("RGBA")
    alpha = image.getchannel("A").point(lambda value: 255 if value >= 128 else 0)
    image.putalpha(alpha)
    palette = {
        "elem_hydrochloric_acid": ((166, 47, 54), (103, 31, 43), (234, 103, 87), (255, 223, 187)),
        "elem_nitric_acid": ((206, 79, 27), (117, 42, 28), (255, 163, 38), (255, 228, 161)),
        "elem_sodium_hydroxide": ((40, 105, 177), (24, 57, 111), (89, 164, 220), (214, 239, 246)),
        "elem_ammonia": ((37, 143, 146), (22, 79, 92), (85, 207, 190), (207, 250, 226)),
    }
    liquid, shadow, highlight, mark = palette[name]
    pixels = image.load()

    # The reference bottle has an elliptical stopper top, a foreshortened shoulder,
    # and an oval bottom rim. Keep that contour and its hard-edged vanilla glass pixels.
    for y in range(18, 26):
        left, right = (4, 11) if y == 18 else (3, 12)
        for x in range(left, right + 1):
            if pixels[x, y][3] == 0:
                continue
            if y == 18:
                color = highlight if 5 <= x <= 10 else liquid
            elif y == 19:
                color = liquid if x not in (3, 12) else shadow
            elif y in (24, 25):
                color = shadow if x in (3, 12) or y == 25 else liquid
            else:
                color = liquid
                if x == 4:
                    color = highlight
                elif x in (11, 12):
                    color = shadow
            pixels[x, y] = (*color, 255)

    # Small, recognizable contents marks; each remains on the same pixel grid.
    marks = {
        "elem_hydrochloric_acid": ((6, 21), (9, 21), (7, 22), (8, 22), (8, 23)),
        "elem_nitric_acid": ((7, 21), (6, 22), (7, 22), (8, 22), (7, 23)),
        "elem_sodium_hydroxide": ((8, 21), (7, 22), (8, 22), (9, 22), (8, 23)),
        "elem_ammonia": ((6, 21), (9, 21), (7, 22), (8, 22), (8, 23)),
    }
    for x, y in marks[name]:
        if pixels[x, y][3] > 0:
            pixels[x, y] = (*mark, 255)
    return fill_grid_sample_points(image)


def fill_grid_sample_points(image):
    """The game's Mod sprite loader samples the center of each 16x16 cell.

    A single transparent sampled pixel makes that whole inventory cell unoccupied.
    Preserve the silhouette while setting any missing sample to the nearest opaque
    color. This explicitly marks every cell in each compact rectangular footprint.
    """
    image = image.convert("RGBA")
    pixels = image.load()
    opaque = [(x, y) for y in range(image.height) for x in range(image.width) if pixels[x, y][3]]
    if not opaque:
        raise ValueError("Empty sprite")
    for y in range(8, image.height, 16):
        for x in range(8, image.width, 16):
            if pixels[x, y][3] == 0:
                nearest = min(opaque, key=lambda p: (p[0]-x)**2 + (p[1]-y)**2)
                pixels[x, y] = pixels[nearest[0], nearest[1]]
    return image


def save(name, image):
    image.save(OUT / f"{name}.png")
    image.resize((image.width * 16, image.height * 16), Image.Resampling.NEAREST).save(HIGH / f"{name}.png")


def machine_preview():
    # Draw both sizes at the same pixel scale, so the footprint reduction is visible.
    active = {entry.get("sprite") for entry in json.loads((ROOT / "Data/machines.json").read_text(encoding="utf-8"))["machines"]}
    names = [name for name in MACHINES if name in active]
    zoom, tile, columns = 4, 48*4, 4
    rows = (len(names) + columns - 1) // columns
    cell_w, cell_h = 2*tile + 12, tile + 34
    canvas = Image.new("RGB", (columns*cell_w+12, rows*cell_h+12), "#262c34")
    draw = ImageDraw.Draw(canvas)
    font = ImageFont.load_default()
    for i, name in enumerate(names):
        x, y = 12 + (i%columns)*cell_w, 12 + (i//columns)*cell_h
        draw.text((x, y), name.removeprefix("elem_"), fill="#f1efe0", font=font)
        for side, folder in enumerate((INPUT, OUT)):
            image = Image.open(folder/f"{name}.png").convert("RGBA")
            image = image.resize((image.width*zoom, image.height*zoom), Image.Resampling.NEAREST)
            xx = x + side*(tile+6)
            # Fixed 48x48 grid shows the same 16-pixel game cells on both sides.
            for grid_y in range(3):
                for grid_x in range(3):
                    draw.rectangle((xx+grid_x*64, y+20+grid_y*64, xx+(grid_x+1)*64-1, y+20+(grid_y+1)*64-1),
                                   fill="#363e47" if (grid_x+grid_y)%2 else "#303740", outline="#4b5259")
            canvas.paste(image, (xx, y+20), image)
    canvas.save(PREVIEW / "compact-stations-before-after.png")


def chemical_preview():
    refs = ROOT / "ArtSource/References/native"
    names = ("med_bottle_red", "med_bottle_blue", "med_bottle_orange") + CHEMICALS
    zoom, columns = 8, 7
    canvas = Image.new("RGB", (columns*150+12, 325), "#292f37")
    draw = ImageDraw.Draw(canvas)
    font = ImageFont.load_default()
    for i, name in enumerate(names):
        x, y = 12 + i*150, 20
        image = Image.open((refs if i < 3 else OUT) / f"{name}.png").convert("RGBA")
        image = image.resize((image.width*zoom, image.height*zoom), Image.Resampling.NEAREST)
        canvas.paste(image, (x, y), image)
        draw.text((x, 285), name.removeprefix("elem_"), fill="#f1efe0", font=font)
    canvas.save(PREVIEW / "native-chem-comparison-after.png")


def main():
    PREVIEW.mkdir(parents=True, exist_ok=True)
    for name in MACHINES:
        old = Image.open(INPUT / f"{name}.png").convert("RGBA")
        if old.size != (48, 48):
            raise ValueError(f"{name}: expected archived 48x48 image, got {old.size}")
        target = SMALL_MACHINE_SIZES.get(name, (32, 32))
        if target == (32, 32):
            image = old.resize(target, Image.Resampling.NEAREST)
        else:
            # These were already narrow 2-3-cell shapes; crop empty margins and
            # fit their silhouette into an honest two-cell rectangular footprint.
            content = old.crop(old.getbbox())
            scale = min((target[0] - 2) / content.width, (target[1] - 2) / content.height)
            content = content.resize((max(1, round(content.width * scale)),
                                      max(1, round(content.height * scale))), Image.Resampling.NEAREST)
            image = Image.new("RGBA", target, (0, 0, 0, 0))
            image.alpha_composite(content, ((target[0]-content.width)//2, (target[1]-content.height)//2))
        save(name, fill_grid_sample_points(image))
    for name in CHEMICALS:
        save(name, pixel_chemical_bottle(name))
    # An open-centre frame formerly sampled transparent and became a zero-cell item.
    frame = Image.open(INPUT / "elem_machine_frame.png").convert("RGBA")
    draw = ImageDraw.Draw(frame)
    draw.line((5, 5, 10, 10), fill=(102, 121, 117, 255), width=1)
    draw.line((10, 5, 5, 10), fill=(78, 91, 89, 255), width=1)
    save("elem_machine_frame", fill_grid_sample_points(frame))
    machine_preview()
    chemical_preview()
    print(f"Saved {len(MACHINES)} compact machines, {len(CHEMICALS)} bottles and one corrected frame")


if __name__ == "__main__":
    main()
