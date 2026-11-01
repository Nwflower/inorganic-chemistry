"""Rebuild the medicine cabinet item texture from its archived generated source."""
from collections import deque
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource/WorkOrders/completed/2026-10-31-medicine-cabinet/originals/elem_medicine_cabinet_generated.png"
RUNTIME = ROOT / "Textures/Items/elem_medicine_cabinet.png"
HIGH_RES = ROOT / "ArtSource/Items/16x/elem_medicine_cabinet.png"


def remove_checker_background(image):
    image = image.convert("RGBA")
    pixels = image.load()
    width, height = image.size
    transparent = bytearray(width * height)
    queue = deque()

    def is_background(x, y):
        r, g, b, _ = pixels[x, y]
        return min(r, g, b) >= 165 and max(r, g, b) - min(r, g, b) <= 8

    for x in range(width):
        queue.append((x, 0))
        queue.append((x, height - 1))
    for y in range(height):
        queue.append((0, y))
        queue.append((width - 1, y))

    while queue:
        x, y = queue.popleft()
        index = y * width + x
        if transparent[index] or not is_background(x, y):
            continue
        transparent[index] = 1
        pixels[x, y] = (0, 0, 0, 0)
        if x:
            queue.append((x - 1, y))
        if x + 1 < width:
            queue.append((x + 1, y))
        if y:
            queue.append((x, y - 1))
        if y + 1 < height:
            queue.append((x, y + 1))

    return image


def mark_grid_cells(image):
    pixels = image.load()
    opaque = [(x, y) for y in range(image.height) for x in range(image.width) if pixels[x, y][3]]
    for y in (8, 24):
        for x in (8, 24):
            if pixels[x, y][3] == 0:
                near = min(opaque, key=lambda point: (point[0] - x) ** 2 + (point[1] - y) ** 2)
                pixels[x, y] = pixels[near[0], near[1]]
    return image


def main():
    source = remove_checker_background(Image.open(SOURCE))
    box = source.getchannel("A").getbbox()
    if not box:
        raise ValueError("Generated cabinet source has no opaque pixels")
    source = source.crop(box)
    scale = min(28 / source.width, 28 / source.height)
    size = (max(1, round(source.width * scale)), max(1, round(source.height * scale)))
    source = source.resize(size, Image.Resampling.NEAREST)
    canvas = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    canvas.alpha_composite(source, ((32 - size[0]) // 2, (32 - size[1]) // 2))
    alpha = canvas.getchannel("A").point(lambda value: 255 if value >= 96 else 0)
    rgb = Image.new("RGB", canvas.size, (0, 0, 0))
    rgb.paste(canvas.convert("RGB"), mask=alpha)
    result = rgb.quantize(colors=32, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert("RGBA")
    result.putalpha(alpha)
    result = mark_grid_cells(result)
    RUNTIME.parent.mkdir(parents=True, exist_ok=True)
    HIGH_RES.parent.mkdir(parents=True, exist_ok=True)
    result.save(RUNTIME)
    result.resize((512, 512), Image.Resampling.NEAREST).save(HIGH_RES)
    print(f"Saved {RUNTIME.name} ({result.width}x{result.height})")


if __name__ == "__main__":
    main()
