"""Deterministic native-grid machines, bottles, fuel cells and laboratory rack."""
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
TEX = ROOT / "Textures" / "Items"
HIGH = ROOT / "ArtSource" / "Items" / "16x"
COMP = ROOT / "ArtSource" / "Items" / "components"
REF = ROOT / "ArtSource" / "References" / "native"
PREVIEW = ROOT / "ArtSource" / "Previews"
WORKORDER = ROOT / "ArtSource" / "WorkOrders" / "completed" / "2026-11-01-native-fit"
MACHINE_ART = ROOT / "ArtSource/WorkOrders/completed/2026-11-02-machine-redraw"
MACHINES = ("air_separator", "rust_extractor", "chip_fab")
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


def refine(name, width, height, colors, workorder=WORKORDER):
    source = Image.open(workorder / (name + "-master.png")).convert("RGBA")
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


def machines():
    from refine_machine_states import variants
    for name in MACHINES:
        refine(name, 48, 48, 20, MACHINE_ART)
        source = Image.open(TEX / ("elem_" + name + ".png")).convert("RGBA")
        for suffix, state in variants(name, source).items():
            save("elem_" + name + suffix, state)
    states = Image.new("RGBA", (4 * 210, 3 * 230), "#282e36")
    state_draw = ImageDraw.Draw(states)
    for row, name in enumerate(MACHINES):
        for col, suffix in enumerate(("", "_powered", "_loaded", "_powered_loaded")):
            if name == "air_separator" and col > 1:
                continue
            im = Image.open(TEX / ("elem_" + name + suffix + ".png")).convert("RGBA")
            state_draw.text((col * 210 + 8, row * 230 + 8), name + (suffix or "_empty_off"), fill="#e3dfcb")
            states.alpha_composite(im.resize((192, 192), Image.Resampling.NEAREST), (col * 210 + 8, row * 230 + 30))
    states.convert("RGB").save(PREVIEW / "machine-state-variants.png")
    air = Image.new("RGBA", (3 * 224, 310), "#282e36")
    air_draw = ImageDraw.Draw(air)
    for col, (label, path) in enumerate((
        ("BEFORE", MACHINE_ART / "air_separator-detail-before.png"),
        ("SIMPLIFIED / OFF", TEX / "elem_air_separator.png"),
        ("SIMPLIFIED / POWERED", TEX / "elem_air_separator_powered.png"),
    )):
        im = Image.open(path).convert("RGBA")
        air_draw.text((col * 224 + 16, 12), label, fill="#e3dfcb")
        air.alpha_composite(im.resize((192, 192), Image.Resampling.NEAREST), (col * 224 + 16, 36))
        air.alpha_composite(im, (col * 224 + 88, 250))
    air.convert("RGB").save(PREVIEW / "air-separator-simplified.png")
    # Show the actual shipped pixels, including a 1:1 row for readability.
    sheet = Image.new("RGBA", (720, 580), "#282e36")
    draw = ImageDraw.Draw(sheet)
    for index, name in enumerate(MACHINES):
        x = index * 240
        draw.text((x + 12, 10), name, fill="#e3dfcb")
        before = Image.open(MACHINE_ART / (name + "-before.png")).convert("RGBA")
        after = Image.open(TEX / ("elem_" + name + ".png")).convert("RGBA")
        draw.text((x + 12, 32), "BEFORE / 4x", fill="#aeb5b4")
        sheet.alpha_composite(before.resize((192, 192), Image.Resampling.NEAREST), (x + 24, 50))
        draw.text((x + 12, 258), "AFTER / 4x", fill="#aeb5b4")
        sheet.alpha_composite(after.resize((192, 192), Image.Resampling.NEAREST), (x + 24, 276))
        draw.text((x + 12, 490), "AFTER / 1x", fill="#aeb5b4")
        sheet.alpha_composite(after, (x + 96, 516))
    sheet.convert("RGB").save(PREVIEW / "machine-redraw-comparison.png")


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


# These masters were drawn against the supplied detailed rack reference. Keep
# the four vessels separate: count changes must not move or redraw the frame.
RACK_ART = ROOT / "ArtSource/WorkOrders/completed/2026-11-02-rack-glassware"
RACK_VESSELS = (
    ("amber_flask", 35, 26, 18, 24, ("#ba713a", "#914421", "#ce9353")),
    ("red_round_flask", 11, 52, 18, 19, ("#923c3d", "#69272f", "#b15c50")),
    ("blue_beaker", 36, 52, 18, 17, ("#326990", "#234767", "#5787a8")),
    ("teal_cylinder", 15, 26, 8, 22, ("#467e83", "#2c535f", "#71a1a1")),
)


def beaker(base, name, x, bottom, width, height, liquid_colors):
    source = Image.open(RACK_ART / (name + ".png")).convert("RGBA")
    alpha = source.getchannel("A").point(lambda value: 255 if value >= 128 else 0)
    source.putalpha(alpha)
    source = source.crop(alpha.getbbox())
    # Normalize to the designed tiny silhouette; generated master aspect ratios
    # vary and must not turn the round flask or graduated cylinder into a sliver.
    source = source.resize((width, height), Image.Resampling.NEAREST)
    # Share all metal/glass shades with the existing rack and allow only three
    # extra colours per liquid; the complete rack stays within the 32-colour cap.
    colors = sorted({p[:3] for p in base.getdata() if p[3]})
    colors += [(218, 216, 194), (174, 182, 177)]
    colors += [tuple(bytes.fromhex(value[1:])) for value in liquid_colors]
    palette = Image.new("P", (1, 1))
    palette.putpalette([channel for color in colors for channel in color] +
                       list(colors[0]) * (256 - len(colors)))
    alpha = source.getchannel("A")
    source = source.convert("RGB").quantize(palette=palette, dither=Image.Dither.NONE).convert("RGBA")
    source.putalpha(alpha)
    layer = Image.new("RGBA", base.size)
    layer.alpha_composite(source, (x + (width - source.width) // 2, bottom + 1 - source.height))
    return layer


def rack():
    # One unchanging metal frame, four separately saved transparent contents.
    master = COMP / "laboratory_rack_empty.png"
    base = Image.open(master if master.exists() else TEX / "elem_laboratory_bay.png").convert("RGBA")
    if not master.exists():
        base.save(master)
    contents = [beaker(base, *vessel) for vessel in RACK_VESSELS]
    for i, content in enumerate(contents, 1):
        content.save(COMP / f"laboratory_beaker_{i}.png")
    for state in range(5):
        image = base.copy()
        for content in contents[:state]:
            image.alpha_composite(content)
        save("elem_laboratory_bay" + ("_" + str(state) if state else ""), image)


def generate():
    COMP.mkdir(parents=True, exist_ok=True)
    machines()
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
    import sys
    if "--machines-only" in sys.argv:
        machines()
    elif "--rack-only" in sys.argv:
        rack()
        lab = Image.new("RGBA", (5 * 200, 246), "#282e36")
        for state in range(5):
            name = "elem_laboratory_bay" + ("_" + str(state) if state else "")
            image = Image.open(TEX / (name + ".png")).convert("RGBA")
            lab.alpha_composite(image.resize((192, 192), Image.Resampling.NEAREST), (state * 200 + 4, 34))
        lab.convert("RGB").save(PREVIEW / "laboratory-rack-state-variants.png")
    else:
        generate()
