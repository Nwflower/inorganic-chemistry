"""Deterministic native-grid bottles, fuel-cell states and shared laboratory rack."""
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
TEX = ROOT / "Textures" / "Items"
HIGH = ROOT / "ArtSource" / "Items" / "16x"
COMP = ROOT / "ArtSource" / "Items" / "components"
REF = ROOT / "ArtSource" / "References" / "native"
PREVIEW = ROOT / "ArtSource" / "Previews"
WORKORDER = ROOT / "ArtSource" / "WorkOrders" / "completed" / "2026-11-01-native-fit"
REFINEMENTS = {
    "coke": (16, 16, 8),
    "brine": (16, 32, 10), "hydrogen_peroxide": (16, 16, 10),
    "bleaching_powder": (16, 32, 10), "electrolysis_cell": (48, 48, 14),
    "chemical_reactor": (48, 48, 14),
}

GASES = {
    "hydrogen": ("H2", "#5b9394", "#c2d8ce"),
    "oxygen": ("O2", "#587797", "#c2d3db"),
    "nitrogen": ("N2", "#748f75", "#cedbc4"),
    "chlorine": ("Cl2", "#9a985d", "#e2d8ac"),
}
GLYPHS = {
    "H": ("101", "101", "111", "101", "101"),
    "O": ("111", "101", "101", "101", "111"),
    "N": ("101", "111", "111", "111", "101"),
    "C": ("111", "100", "100", "100", "111"),
    "l": ("10", "10", "10", "10", "11"),
    "2": ("111", "001", "111", "100", "111"),
}


def save(name, image):
    image.save(TEX / (name + ".png"))
    image.resize((image.width * 16, image.height * 16), Image.Resampling.NEAREST).save(
        HIGH / (name + ".png")
    )


def refine(name, width, height, colors):
    source = Image.open(WORKORDER / (name + "-master.png")).convert("RGBA")
    alpha = source.getchannel("A").point(lambda value: 255 if value >= 128 else 0)
    bounds = alpha.getbbox()
    if bounds is None:
        raise ValueError(f"Empty art source: {name}")
    source.putalpha(alpha)
    source = source.crop(bounds)
    scale = min((width - 2) / source.width, (height - 2) / source.height)
    source = source.resize((round(source.width * scale), round(source.height * scale)), Image.Resampling.NEAREST)
    alpha = source.getchannel("A")
    source = source.convert("RGB").quantize(colors=colors, dither=Image.Dither.NONE).convert("RGBA")
    source.putalpha(alpha)
    image = Image.new("RGBA", (width, height))
    image.alpha_composite(source, ((width - source.width) // 2, (height - source.height) // 2))
    save("elem_" + name, image)


def bottle(formula, band, highlight):
    # One cell wide, three tall. Vanilla 1-cell items such as long_module3 keep a
    # 14-of-16 pixel body with a one-pixel margin, and about 85% of the total
    # height, so the bottle matches that fill instead of leaving wide gaps.
    image = Image.new("RGBA", (16, 48))
    draw = ImageDraw.Draw(image)
    outline = "#292638"
    draw.rectangle((6, 3, 9, 4), fill="#aeb5b4", outline=outline)
    draw.rectangle((5, 5, 10, 6), fill="#8c969b", outline=outline)
    draw.rectangle((6, 7, 9, 9), fill="#a9b4b4")
    draw.polygon(((5, 9), (10, 9), (13, 12), (14, 15), (14, 41),
                  (13, 44), (2, 44), (1, 41), (1, 15), (2, 12)),
                 fill="#78858c", outline=outline)
    draw.polygon(((4, 11), (11, 11), (13, 14), (13, 40), (12, 42),
                  (3, 42), (2, 40), (2, 14)), fill="#b2bbba")
    draw.line((2, 15, 2, 40), fill="#d0d1c7")
    draw.line((13, 14, 13, 40), fill="#66717f")
    draw.rectangle((1, 22, 14, 36), fill=band, outline=outline)
    draw.line((2, 23, 13, 23), fill=highlight)
    draw.line((2, 40, 13, 40), fill="#626d77")
    draw.line((3, 43, 12, 43), fill="#47485b")
    width = sum(len(GLYPHS[char][0]) for char in formula) + len(formula) - 1
    x0 = (16 - width) // 2
    for char in formula:
        for y, row in enumerate(GLYPHS[char]):
            for x, bit in enumerate(row):
                if bit == "1":
                    draw.point((x0 + x, 27 + y), fill="#f1ead2")
        x0 += len(GLYPHS[char][0]) + 1
    return image


def fuel_cell(state):
    # The shell and outline are identical to the installed game's plasma cell.
    image = Image.open(REF / "plasma_fuel_cell.png").convert("RGBA")
    image.putalpha(image.getchannel("A").point(lambda value: 255 if value >= 128 else 0))
    draw = ImageDraw.Draw(image)
    draw.rectangle((12, 10, 51, 21), fill="#27334a")
    draw.rectangle((13, 11, 50, 19), fill="#374258")
    draw.line((13, 20, 50, 20), fill="#171c2e")
    if state != "empty":
        draw.rectangle((16, 12, 47, 18), fill="#4c8587")
        draw.line((17, 13, 45, 13), fill="#99c0b9")
        draw.line((17, 17, 46, 17), fill="#335e6e")
        for x in (21, 30, 39):
            draw.rectangle((x, 14, x + 2, 16), fill="#b5d6c7")
        if state == "oxidized":
            draw.line((18, 18, 24, 18), fill="#bbad6c")
            draw.line((34, 18, 44, 18), fill="#bbad6c")
            draw.rectangle((28, 14, 31, 16), fill="#dfc782")
    return image


def beaker(x, bottom, color, neck=False):
    image = Image.new("RGBA", (64, 64))
    d = ImageDraw.Draw(image)
    top = bottom - (16 if neck else 13)
    edge = "#393345"
    if neck:
        d.rectangle((x + 5, top, x + 9, top + 4), fill="#c5c6be", outline=edge)
        d.polygon(((x + 5, top + 4), (x + 9, top + 4), (x + 14, bottom - 2),
                   (x + 12, bottom), (x + 2, bottom), (x, bottom - 2)),
                  fill="#858b95", outline=edge)
    else:
        d.rectangle((x + 2, top, x + 12, bottom), fill="#858b95", outline=edge)
        d.rectangle((x + 1, top, x + 13, top + 1), fill="#b5bbc0", outline=edge)
    d.rectangle((x + 3, bottom - 7, x + 11, bottom - 2), fill=color)
    d.line((x + 4, bottom - 8, x + 4, bottom - 3), fill="#ced2c9")
    d.line((x + 3, bottom - 1, x + 11, bottom - 1), fill=edge)
    return image


def rack():
    # One unchanging metal frame, four separately saved transparent contents.
    master = COMP / "laboratory_rack_empty.png"
    base = Image.open(master if master.exists() else TEX / "elem_laboratory_bay.png").convert("RGBA")
    if not master.exists():
        base.save(master)
    contents = [beaker(30, 24, "#a3754f", True),
                beaker(12, 49, "#a45a4c", True),
                beaker(34, 49, "#597b89"),
                beaker(12, 24, "#617e8b")]
    for i, content in enumerate(contents, 1):
        content.save(COMP / f"laboratory_beaker_{i}.png")
    for state in range(5):
        image = base.copy()
        for content in contents[:state]:
            image.alpha_composite(content)
        save("elem_laboratory_bay" + ("_" + str(state) if state else ""), image)


def generate():
    COMP.mkdir(parents=True, exist_ok=True)
    for name, (width, height, colors) in REFINEMENTS.items():
        refine(name, width, height, colors)
    for gas, (formula, band, highlight) in GASES.items():
        save("elem_" + gas + "_cylinder", bottle(formula, band, highlight))
    for state, suffix in (("empty", ""), ("hydrogen", "_h2"), ("oxidized", "_h2_o2")):
        save("elem_hydrogen_fuel_cell" + suffix, fuel_cell(state))
    rack()
    images = [Image.open(TEX / f"elem_{gas}_cylinder.png").convert("RGBA") for gas in GASES]
    images.append(fuel_cell("oxidized"))
    sheet = Image.new("RGBA", (4 * 172 + 280, 300), "#282e36")
    for index, image in enumerate(images):
        large = image.resize((image.width * 4, image.height * 4), Image.Resampling.NEAREST)
        cell_width = 280 if index == 4 else 172
        x = 4 * 172 if index == 4 else index * 172
        sheet.alpha_composite(large, (x + (cell_width - large.width) // 2, 20))
    sheet.convert("RGB").save(PREVIEW / "vanilla-inspired-gases-and-cell.png")
    lab = Image.new("RGBA", (5 * 200, 246), "#282e36")
    for state in range(5):
        name = "elem_laboratory_bay" + ("_" + str(state) if state else "")
        image = Image.open(TEX / (name + ".png")).convert("RGBA")
        lab.alpha_composite(image.resize((192, 192), Image.Resampling.NEAREST),
                            (state * 200 + 4, 34))
    lab.convert("RGB").save(PREVIEW / "laboratory-rack-state-variants.png")
    return images[:4]


if __name__ == "__main__":
    generate()
