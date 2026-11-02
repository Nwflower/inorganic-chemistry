"""Build machinery, gas-cylinder and fuel-cell sprites for the mod.

The references are extracted from the user's installed game. Drawing stays on the
native 16-pixel grid; runtime sprites and enlarged editable sources match exactly.
"""
from pathlib import Path
from PIL import Image, ImageDraw
import random
import sys

ROOT = Path(__file__).resolve().parents[1]
REFS = ROOT / 'ArtSource/References/native'
WORKORDER = ROOT / 'ArtSource/WorkOrders/completed/2026-11-01-electronic-beaker-laboratory-rack'
TEX = ROOT / 'Textures/Items'
HIGH = ROOT / 'ArtSource/Items/16x'
PREVIEW = ROOT / 'ArtSource/Previews'
RESAMPLE = Image.Resampling.NEAREST
RACK_CROP = (25, 25, 1230, 1230)

def base(name):
    image = Image.open(REFS / (name + '.png')).convert('RGBA')
    image.putalpha(image.getchannel('A').point(lambda x: 255 if x >= 128 else 0))
    return image

def save(name, image):
    image.save(TEX / (name + '.png'))
    image.resize((image.width * 16, image.height * 16), RESAMPLE).save(HIGH / (name + '.png'))

def generated_icon(master_name, size, crop_bounds=None):
    master = Image.open(WORKORDER / master_name).convert('RGBA')
    alpha = master.getchannel('A').point(lambda value: 255 if value >= 128 else 0)
    master.putalpha(alpha)
    if crop_bounds is None:
        bounds = alpha.getbbox()
        if bounds is None:
            raise ValueError(f'{master_name}: image has no opaque pixels')
    else:
        bounds = crop_bounds
    content = master.crop(bounds)
    limit = size - 4
    scale = min(limit / content.width, limit / content.height)
    target = (max(1, round(content.width * scale)), max(1, round(content.height * scale)))
    content = content.resize(target, RESAMPLE)
    colors = content.convert('RGB').quantize(colors=28, dither=Image.Dither.NONE).convert('RGBA')
    colors.putalpha(content.getchannel('A'))
    icon = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    icon.alpha_composite(colors, ((size - target[0]) // 2, (size - target[1]) // 2))
    return icon


def generate_fertilizer():
    image = Image.new('RGBA', (32, 32), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    outline = '#292735'
    draw.polygon(((7, 2), (24, 2), (23, 6), (28, 10), (30, 24), (26, 29),
                  (6, 29), (2, 24), (4, 10), (9, 6)), fill='#b69a69', outline=outline)
    draw.polygon(((9, 5), (23, 5), (26, 10), (6, 10)), fill='#d1b982', outline=outline)
    draw.polygon(((4, 11), (8, 12), (8, 26), (6, 27), (3, 24)), fill='#8f7650')
    draw.polygon(((24, 11), (28, 11), (29, 24), (26, 27), (24, 25)), fill='#d0b783')

    for y in (12, 25, 27):
        draw.line((8, y, 24, y), fill='#a3875b')
    for x in (9, 12, 15, 24, 26):
        draw.line((x, 11, x, 27), fill='#c8ad79')
    for x, y in ((10, 13), (13, 13), (21, 13), (11, 26), (17, 26), (23, 26)):
        draw.point((x, y), fill='#e0ca96')

    draw.rectangle((10, 15, 22, 23), fill='#d6d1a8', outline=outline)
    draw.line((13, 21, 16, 18, 20, 20), fill='#61745b', width=2)
    draw.point((14, 18), fill='#839466')
    draw.point((18, 17), fill='#9aa36a')
    draw.point((20, 20), fill='#9aa36a')

    TEX.mkdir(parents=True, exist_ok=True)
    HIGH.mkdir(parents=True, exist_ok=True)
    PREVIEW.mkdir(parents=True, exist_ok=True)
    save('elem_fertilizer', image)
    preview = Image.new('RGB', (160, 160), '#282e36')
    preview.paste(image.resize((128, 128), RESAMPLE), (16, 8), image.resize((128, 128), RESAMPLE))
    preview.save(PREVIEW / 'nitrogen-fertilizer.png')


# Minerals keep the native ore's landscape 3x2 silhouette, including its two
# transparent top corners.
def tint_mineral(image, tint):
    pixels = image.load()
    for y in range(image.height):
        for x in range(image.width):
            r, g, b, a = pixels[x, y]
            if a and max(r, g, b) > 75:
                pixels[x, y] = (round(r * .48 + tint[0] * .52), round(g * .48 + tint[1] * .52),
                                round(b * .48 + tint[2] * .52), a)
    return image


def generate_minerals():
    ore = base('ore1')
    TEX.mkdir(parents=True, exist_ok=True)
    HIGH.mkdir(parents=True, exist_ok=True)
    minerals = []
    for name, tint in (('elem_silica', (205, 198, 168)), ('elem_iron_oxide', (172, 104, 80))):
        image = tint_mineral(ore.copy(), tint)
        save(name, image)
        minerals.append((name, image))
    return ore, minerals


def save_mineral_preview(ore, minerals):
    PREVIEW.mkdir(parents=True, exist_ok=True)
    preview = Image.new('RGB', (3 * 220, 280), '#282e36')
    draw = ImageDraw.Draw(preview)
    for index, (name, image) in enumerate([('vanilla ore', ore)] + minerals):
        scale = 3 if name == 'vanilla ore' else 4
        sprite = image.resize((image.width * scale, image.height * scale), RESAMPLE)
        preview.paste(sprite, (index * 220 + (220 - sprite.width) // 2, 22), sprite)
        draw.text((index * 220 + 10, 258), name.removeprefix('elem_'), fill='white')
    preview.save(PREVIEW / 'vanilla-inspired-minerals.png')


if __name__ == '__main__' and '--minerals-only' in sys.argv:
    ore, minerals = generate_minerals()
    save_mineral_preview(ore, minerals)
    print('Wrote silica and iron oxide as landscape 3x2 mineral sprites.')
    sys.exit(0)

if __name__ == '__main__' and '--fertilizer-only' in sys.argv:
    generate_fertilizer()
    print('Wrote nitrogen fertilizer as a 2x2 woven cloth sack.')
    sys.exit(0)


# The current bottle, fuel-cell and rack artwork is regenerated by
# generate_native_fit_assets() at the end of this script.

ore, minerals = generate_minerals()

# Coke reuses the vanilla flux silhouette, recoloured into a black carbon ramp.
flux = base('flux_agent')
coke = flux.copy()
carbon = ((30, 30, 36), (46, 46, 54), (68, 68, 78), (96, 96, 108))
pixels = coke.load()
for y in range(coke.height):
    for x in range(coke.width):
        r, g, b, a = pixels[x, y]
        if not a or (r, g, b) == (41, 34, 52):
            continue
        level = round((r * .299 + g * .587 + b * .114) / 255 * (len(carbon) - 1))
        pixels[x, y] = carbon[level] + (a,)
save('elem_coke', coke)

# Keep the recognisable bodies, outlines and palette of actual vanilla machinery;
# small controls depict the function of each machine.
specs = {
    'elem_electrolysis_cell': ('evaporator', 'electrolysis'),
    'elem_air_separator': ('furnace', 'air'),
    'elem_oxyhydrogen_welder': ('furnace', 'weld'),
    'elem_rust_extractor': ('evaporator', 'rust'),
    'elem_arc_furnace': ('furnace', 'heat'),
    'elem_chemical_reactor': ('chem_finisher', 'electronic_beaker'),
    'elem_chip_fab': ('chem_finisher', 'chip'),
}
machines = []
for name, (source, kind) in specs.items():
    image = generated_icon('electronic-beaker-master.png', 48) if kind == 'electronic_beaker' else base(source)
    d = ImageDraw.Draw(image)
    if kind == 'electrolysis':
        d.rectangle((14, 16, 18, 29), fill='#6489a8', outline='#222332')
        d.rectangle((28, 16, 32, 29), fill='#7aa49e', outline='#222332')
        d.line((16, 18, 16, 26), fill='#c6dbe0')
        d.line((30, 18, 30, 26), fill='#c1ded3')
    elif kind == 'air':
        d.rectangle((17, 20, 30, 29), fill='#3c5259', outline='#222332')
        for y in (22, 25, 28): d.line((19, y, 28, y), fill='#8da8a0')
    elif kind == 'weld':
        d.rectangle((13, 19, 18, 31), fill='#557d9c', outline='#222332')
        d.rectangle((29, 19, 34, 31), fill='#a16050', outline='#222332')
        d.line((19, 29, 24, 24, 28, 27), fill='#d5b77b', width=2)
    elif kind == 'rust':
        d.rectangle((14, 17, 32, 28), fill='#825447', outline='#222332')
        for x, y in ((17, 20), (23, 24), (29, 19)): d.point((x, y), fill='#c19a71')
    elif kind == 'heat':
        d.rectangle((15, 20, 32, 30), fill='#4b3033', outline='#222332')
        d.rectangle((18, 23, 29, 27), fill='#b77246')
        d.line((20, 24, 27, 24), fill='#e2bb78')
    elif kind == 'electronic_beaker':
        pass
    else:
        d.rectangle((26, 17, 35, 28), fill='#394d55', outline='#222332')
        d.rectangle((29, 20, 32, 24), fill='#8ea99d')
        for y in (19, 22, 25):
            d.point((27, y), fill='#d0c8a5'); d.point((34, y), fill='#d0c8a5')
    save(name, image)
    machines.append((name, image))

laboratory_states = []
for sprite, master_name, label in (
    ('elem_laboratory_bay', 'laboratory-rack-0-items-master.png', '0 items'),
    ('elem_laboratory_bay_1', 'laboratory-rack-1-item-master.png', '1 item'),
    ('elem_laboratory_bay_2', 'laboratory-rack-2-items-master.png', '2 items'),
    ('elem_laboratory_bay_3', 'laboratory-rack-3-items-master.png', '3 items'),
    ('elem_laboratory_bay_4', 'laboratory-rack-master.png', '4+ items'),
):
    image = generated_icon(master_name, 64, RACK_CROP)
    save(sprite, image)
    laboratory_states.append((label, image))

PREVIEW.mkdir(parents=True, exist_ok=True)
preview = Image.new('RGB', (7 * 180, 250), '#282e36')
d = ImageDraw.Draw(preview)
for i, (name, image) in enumerate(machines):
    sprite = image.resize((144, 144), RESAMPLE)
    preview.paste(sprite, (i * 180 + 10, 30), sprite)
    d.text((i * 180 + 10, 185), name.removeprefix('elem_'), fill='white')
preview.save(PREVIEW / 'vanilla-inspired-machines.png')
save_mineral_preview(ore, minerals)
lab_preview = Image.new('RGB', (5 * 200, 246), '#282e36')
draw = ImageDraw.Draw(lab_preview)
for index, (label, image) in enumerate(laboratory_states):
    sprite = image.resize((192, 192), RESAMPLE)
    x = index * 200 + 4
    draw.text((x + 4, 8), label, fill='white')
    lab_preview.paste(sprite, (x, 34), sprite)
    draw.rectangle((x, 34, x + 191, 225), outline='#59636d')
lab_preview.save(PREVIEW / 'laboratory-rack-state-variants.png')
# Re-apply the current shared silhouettes after legacy base compositions.
from generate_native_fit_assets import generate as generate_native_fit_assets

generate_native_fit_assets()
print('Wrote native-inspired machines, shared-frame rack states, narrow bottles and fuel-cell states.')
