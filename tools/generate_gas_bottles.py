"""Build 2x3 gas-canister sprites from the shared gas tank artwork.

The master is a new gas-cylinder design; vanilla objects are references for the
pixel palette, not source silhouettes. Gas labels and colors share one geometry.
"""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
MASTER = ROOT / 'ArtSource/WorkOrders/completed/2026-11-01-gas-canister-2x3/gas-canister-master.png'
TEX = ROOT / 'Textures/Items'
HIGH = ROOT / 'ArtSource/Items/16x'
PREVIEW = ROOT / 'ArtSource/Previews'
RESAMPLE = Image.Resampling.NEAREST

GASES = {
    'hydrogen': ('#568f96', '#b9d8d0', 'H2'),
    'oxygen': ('#536f91', '#bccfd9', 'O2'),
    'nitrogen': ('#6f8c72', '#ced9be', 'N2'),
    'chlorine': ('#969359', '#e0d7a9', 'Cl'),
}
# Six-row labels are rendered at 2x; chlorine intentionally uses "Cl" only.
FORMULAS = {
    'H2':  ('101000', '101000', '111011', '101001', '101010', '000011'),
    'O2':  ('111000', '101000', '101011', '101001', '111010', '000011'),
    'N2':  ('1001000', '1101000', '1011011', '1001001', '1001010', '0000011'),
    'Cl':  ('11100', '10001', '10001', '10001', '11101', '00001'),
}


def generate():
    master = Image.open(MASTER).convert('RGBA')
    # The generated image has wide side margins. Crop them symmetrically so the
    # tank occupies the 2x3 icon while retaining its straight-sided silhouette.
    left = round(master.width * 0.16)
    master = master.crop((left, 0, master.width - left, master.height)).resize((32, 48), RESAMPLE)
    alpha = master.getchannel('A').point(lambda value: 255 if value >= 128 else 0)
    # Game sprites have small palettes and hard edges. Quantize only after sizing;
    # the original transparent master remains untouched in ArtSource.
    template = master.convert('RGB').quantize(colors=20, dither=Image.Dither.NONE).convert('RGBA')
    template.putalpha(alpha)
    images = []
    for gas, (paint, highlight, formula) in GASES.items():
        image = template.copy()
        draw = ImageDraw.Draw(image)
        # The identification panel leaves the steel body and base visible.
        draw.rectangle((6, 22, 25, 36), fill=paint)
        draw.line((6, 22, 25, 22), fill=highlight)
        left = (32 - len(FORMULAS[formula][0]) * 2) // 2
        for row, line in enumerate(FORMULAS[formula]):
            for column, bit in enumerate(line):
                if bit == '1':
                    x, y = column * 2 + left, row * 2 + 24
                    draw.rectangle((x, y, x + 1, y + 1), fill='#efe9ca')
        name = f'elem_{gas}_cylinder.png'
        image.save(TEX / name)
        image.resize((512, 768), RESAMPLE).save(HIGH / name)
        images.append(image)

    PREVIEW.mkdir(parents=True, exist_ok=True)
    preview = Image.new('RGB', (4 * 160, 320), '#282e36')
    draw = ImageDraw.Draw(preview)
    for index, ((gas, (_, _, label)), image) in enumerate(zip(GASES.items(), images)):
        sprite = image.resize((160, 240), RESAMPLE)
        preview.paste(sprite, (index * 160, 18), sprite)
        draw.text((index * 160 + 20, 278), f'{gas} ({label})', fill='#e8e3d5')
    preview.save(PREVIEW / 'gas-cylinders-native-style.png')

    # Keep the project's existing combined gas-and-cell preview current without
    # regenerating or overwriting the unrelated fuel-cell artwork.
    cell = Image.open(TEX / 'elem_hydrogen_fuel_cell.png').convert('RGBA')
    combined = Image.new('RGB', (5 * 150, 235), '#282e36')
    for index, image in enumerate(images + [cell]):
        sprite = image.resize((image.width * 4, image.height * 4), RESAMPLE)
        combined.paste(sprite, (index * 150 + (150 - sprite.width) // 2, 20), sprite)
    combined.save(PREVIEW / 'vanilla-inspired-gases-and-cell.png')
    return images


if __name__ == '__main__':
    generate()
    print('Wrote four gas-cylinder sprites and their shared preview.')
