"""Generate the manual's page-turn, zoom and close icons.

The game ships the vanilla paper chrome sprites (paper_buttons_close1 and friends) but this
build has no PaperAssetCatalog, so DefaultChromeSprite() resolves to nothing and every chrome
button renders as an empty blue square. A mod-owned sprite key always resolves through
LoadedMod.PaperSprites, so the book points at these files instead.

Run from the mod folder: python tools/generate_paper_buttons.py
"""
from pathlib import Path
from PIL import Image, ImageDraw

OUT = Path(__file__).resolve().parents[1] / 'Textures' / 'Paper'
INK = (88, 88, 88, 255)
SUPERSAMPLE = 4


def canvas(width, height):
    image = Image.new('RGBA', (width * SUPERSAMPLE, height * SUPERSAMPLE), (0, 0, 0, 0))
    return image, ImageDraw.Draw(image)


def finish(image, width, height, name):
    image = image.resize((width, height), Image.LANCZOS)
    image.save(OUT / name)
    print('wrote', name, image.size)


def stroke(width, height):
    # One design pixel of the vanilla 14x15 icon is two display pixels here.
    return max(4, round(min(width, height) * 0.14)) * SUPERSAMPLE


def close():
    width, height = 28, 30
    image, draw = canvas(width, height)
    pad = 7 * SUPERSAMPLE
    line = stroke(width, height)
    draw.line([pad, pad, width * SUPERSAMPLE - pad, height * SUPERSAMPLE - pad], fill=INK, width=line)
    draw.line([width * SUPERSAMPLE - pad, pad, pad, height * SUPERSAMPLE - pad], fill=INK, width=line)
    finish(image, width, height, 'elem_paper_close.png')


def zoom():
    width, height = 28, 30
    image, draw = canvas(width, height)
    line = stroke(width, height)
    radius = 8 * SUPERSAMPLE
    centre = (12 * SUPERSAMPLE + SUPERSAMPLE // 2, 12 * SUPERSAMPLE + SUPERSAMPLE // 2)
    draw.ellipse([centre[0] - radius, centre[1] - radius, centre[0] + radius, centre[1] + radius],
                 outline=INK, width=line)
    draw.line([centre[0] + radius * 0.7, centre[1] + radius * 0.7,
               22 * SUPERSAMPLE, 24 * SUPERSAMPLE], fill=INK, width=line)
    finish(image, width, height, 'elem_paper_zoom.png')


def arrow(name, width, height, direction):
    image, draw = canvas(width, height)
    line = stroke(width, height)
    inset = 8 * SUPERSAMPLE
    top = inset
    bottom = height * SUPERSAMPLE - inset
    middle = height * SUPERSAMPLE / 2
    tip = width * SUPERSAMPLE - inset if direction > 0 else inset
    tail = inset if direction > 0 else width * SUPERSAMPLE - inset
    draw.line([tail, top, tip, middle], fill=INK, width=line)
    draw.line([tip, middle, tail, bottom], fill=INK, width=line)
    finish(image, width, height, name)


if __name__ == '__main__':
    OUT.mkdir(parents=True, exist_ok=True)
    close()
    zoom()
    arrow('elem_paper_flip_left.png', 32, 32, -1)
    arrow('elem_paper_flip_right.png', 28, 30, 1)
