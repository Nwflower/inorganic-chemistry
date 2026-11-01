"""Build machinery, gas-cylinder and fuel-cell sprites for the mod.

The references are extracted from the user's installed game. Drawing stays on the
native 16-pixel grid; runtime sprites and enlarged editable sources match exactly.
"""
from pathlib import Path
from PIL import Image, ImageDraw
from generate_gas_bottles import generate as generate_gas_cylinders

ROOT = Path(__file__).resolve().parents[1]
REFS = ROOT / 'ArtSource/References/native'
TEX = ROOT / 'Textures/Items'
HIGH = ROOT / 'ArtSource/Items/16x'
PREVIEW = ROOT / 'ArtSource/Previews'
RESAMPLE = Image.Resampling.NEAREST

def base(name):
    image = Image.open(REFS / (name + '.png')).convert('RGBA')
    image.putalpha(image.getchannel('A').point(lambda x: 255 if x >= 128 else 0))
    return image

def save(name, image):
    image.save(TEX / (name + '.png'))
    image.resize((image.width * 16, image.height * 16), RESAMPLE).save(HIGH / (name + '.png'))

gas_images = generate_gas_cylinders()

# The vanilla converter is already a 1x2 plug-in power item.
cell = base('current_converter')
d = ImageDraw.Draw(cell)
d.rectangle((5, 5, 10, 7), fill='#6a9995')
d.rectangle((6, 13, 9, 15), fill='#73b7a6')
d.rectangle((6, 23, 9, 25), fill='#c1a15f')
save('elem_hydrogen_fuel_cell', cell)

# Follow the vanilla 3x2 ore silhouette rather than shrinking rocks to one cell.
# The irregular occupied shape is preserved; colors distinguish sand from oxide.
ore = base('ore1')
minerals = []
for name, tint in (('elem_silica', (190, 183, 151)), ('elem_iron_oxide', (155, 90, 68))):
    image = ore.copy()
    pixels = image.load()
    for y in range(image.height):
        for x in range(image.width):
            r, g, b, a = pixels[x, y]
            if a and max(r, g, b) > 75:
                # Preserve the game's shading while tinting its mineral highlights.
                pixels[x, y] = (round(r * .48 + tint[0] * .52), round(g * .48 + tint[1] * .52),
                                round(b * .48 + tint[2] * .52), a)
    save(name, image)
    minerals.append((name, image))

# Keep the recognisable bodies, outlines and palette of actual vanilla machinery;
# small controls depict the function of each machine.
specs = {
    'elem_electrolysis_cell': ('evaporator', 'electrolysis'),
    'elem_air_separator': ('furnace', 'air'),
    'elem_oxyhydrogen_welder': ('furnace', 'weld'),
    'elem_rust_extractor': ('evaporator', 'rust'),
    'elem_arc_furnace': ('furnace', 'heat'),
    'elem_chemical_reactor': ('chem_finisher', 'reactor'),
    'elem_chip_fab': ('chem_finisher', 'chip'),
}
machines = []
for name, (source, kind) in specs.items():
    image = base(source)
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
    elif kind == 'reactor':
        d.rectangle((26, 15, 34, 30), fill='#4e7977', outline='#222332')
        d.line((28, 19, 32, 19), fill='#b6d3b9')
        d.line((28, 24, 32, 24), fill='#b6d3b9')
    else:
        d.rectangle((26, 17, 35, 28), fill='#394d55', outline='#222332')
        d.rectangle((29, 20, 32, 24), fill='#8ea99d')
        for y in (19, 22, 25):
            d.point((27, y), fill='#d0c8a5'); d.point((34, y), fill='#d0c8a5')
    save(name, image)
    machines.append((name, image))

PREVIEW.mkdir(parents=True, exist_ok=True)
preview = Image.new('RGB', (7 * 180, 250), '#282e36')
d = ImageDraw.Draw(preview)
for i, (name, image) in enumerate(machines):
    sprite = image.resize((144, 144), RESAMPLE)
    preview.paste(sprite, (i * 180 + 10, 30), sprite)
    d.text((i * 180 + 10, 185), name.removeprefix('elem_'), fill='white')
preview.save(PREVIEW / 'vanilla-inspired-machines.png')
preview = Image.new('RGB', (5 * 100, 220), '#282e36')
for i, image in enumerate(gas_images + [cell]):
    sprite = image.resize((80, 160), RESAMPLE)
    preview.paste(sprite, (i * 100 + 10, 20), sprite)
preview.save(PREVIEW / 'vanilla-inspired-gases-and-cell.png')
preview = Image.new('RGB', (3 * 220, 190), '#282e36')
d = ImageDraw.Draw(preview)
for i, (name, image) in enumerate([('vanilla ore', ore)] + minerals):
    sprite = image.resize((192, 128), RESAMPLE)
    preview.paste(sprite, (i * 220 + 10, 22), sprite)
    d.text((i * 220 + 10, 158), name.removeprefix('elem_'), fill='white')
preview.save(PREVIEW / 'vanilla-inspired-minerals.png')
print('Wrote seven machines, four gas cylinders and one fuel cell.')
