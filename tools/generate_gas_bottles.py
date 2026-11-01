"""Build 1x2 gas-cylinder sprites from the original gas tank artwork.

The master is a new gas-cylinder design; vanilla objects are references for the
pixel palette, not source silhouettes. Gas labels and colors share one geometry.
"""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
MASTER = ROOT / 'ArtSource/WorkOrders/completed/2026-11-01-gas-cylinder-style/gas-cylinder-master.png'
TEX = ROOT / 'Textures/Items'
HIGH = ROOT / 'ArtSource/Items/16x'
PREVIEW = ROOT / 'ArtSource/Previews'
RESAMPLE = Image.Resampling.NEAREST

GASES = {
    'hydrogen': ('#568f96', '#b9d8d0', 'H2'),
    'oxygen': ('#536f91', '#bccfd9', 'O2'),
    'nitrogen': ('#6f8c72', '#ced9be', 'N2'),
    'chlorine': ('#969359', '#e0d7a9', 'Cl2'),
}
# Six-row labels fit the eight-pixel plate. The small final glyph is subscript 2.
FORMULAS = {
    'H2':  ('101000', '101000', '111011', '101001', '101010', '000011'),
    'O2':  ('111000', '101000', '101011', '101001', '111010', '000011'),
    'N2':  ('1001000', '1101000', '1011011', '1001001', '1001010', '0000011'),
    'Cl2': ('11101000', '10001000', '10001011', '10001001', '11101010', '00000011'),
}


def generate():
    master = Image.open(MASTER).convert('RGBA').resize((16, 32), RESAMPLE)
    alpha = master.getchannel('A').point(lambda value: 255 if value >= 128 else 0)
    # Game sprites have small palettes and hard edges. Quantize only after sizing;
    # the original transparent master remains untouched in ArtSource.
    template = master.convert('RGB').quantize(colors=20, dither=Image.Dither.NONE).convert('RGBA')
    template.putalpha(alpha)
    images = []
    for gas, (paint, highlight, formula) in GASES.items():
        image = template.copy()
        draw = ImageDraw.Draw(image)
        # A small painted identification panel leaves the tank body and rim visible.
        draw.rectangle((4, 15, 11, 22), fill=paint)
        draw.line((4, 15, 11, 15), fill=highlight)
        left = (16 - len(FORMULAS[formula][0])) // 2
        for row, line in enumerate(FORMULAS[formula]):
            for column, bit in enumerate(line):
                if bit == '1':
                    draw.point((column + left, row + 16), fill='#efe9ca')
        name = f'elem_{gas}_cylinder.png'
        image.save(TEX / name)
        image.resize((256, 512), RESAMPLE).save(HIGH / name)
        images.append(image)

    PREVIEW.mkdir(parents=True, exist_ok=True)
    preview = Image.new('RGB', (4 * 140, 340), '#282e36')
    draw = ImageDraw.Draw(preview)
    for index, ((gas, _), image) in enumerate(zip(GASES.items(), images)):
        sprite = image.resize((128, 256), RESAMPLE)
        preview.paste(sprite, (index * 140 + 6, 18), sprite)
        draw.text((index * 140 + 18, 288), gas, fill='#e8e3d5')
    preview.save(PREVIEW / 'gas-cylinders-native-style.png')

    # Keep the project's existing combined gas-and-cell preview current without
    # regenerating or overwriting the unrelated fuel-cell artwork.
    cell = Image.open(TEX / 'elem_hydrogen_fuel_cell.png').convert('RGBA')
    combined = Image.new('RGB', (5 * 100, 220), '#282e36')
    for index, image in enumerate(images + [cell]):
        sprite = image.resize((80, 160), RESAMPLE)
        combined.paste(sprite, (index * 100 + 10, 20), sprite)
    combined.save(PREVIEW / 'vanilla-inspired-gases-and-cell.png')
    return images


if __name__ == '__main__':
    generate()
    print('Wrote four gas-cylinder sprites and their shared preview.')
