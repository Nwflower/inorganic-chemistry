"""Reduce the approved silicon illustration to the game's crisp 16px icon grid."""
from pathlib import Path
from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource/WorkOrders/completed/2026-11-01-silicon-redraw/generated-silicon.png"
RUNTIME = ROOT / "Textures/Items/elem_silicon.png"
HIGH_RES = ROOT / "ArtSource/Items/16x/elem_silicon.png"


def generate():
    original = Image.open(SOURCE).convert("RGBA")
    alpha = original.getchannel("A").point(lambda value: 255 if value >= 240 else 0)
    bounds = alpha.getbbox()
    if bounds is None:
        raise ValueError("Generated silicon illustration has no opaque pixels")

    crop = original.crop(bounds)
    crop.putalpha(alpha.crop(bounds))
    scale = min(14 / crop.width, 8 / crop.height)
    size = (max(1, round(crop.width * scale)), max(1, round(crop.height * scale)))
    crop = crop.resize(size, Image.Resampling.NEAREST)

    quantized = crop.convert("RGB").quantize(colors=24, dither=Image.Dither.NONE).convert("RGBA")
    quantized.putalpha(crop.getchannel("A"))
    icon = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    icon.alpha_composite(quantized, ((16 - size[0]) // 2, (16 - size[1]) // 2))

    RUNTIME.parent.mkdir(parents=True, exist_ok=True)
    HIGH_RES.parent.mkdir(parents=True, exist_ok=True)
    icon.save(RUNTIME)
    icon.resize((256, 256), Image.Resampling.NEAREST).save(HIGH_RES)


if __name__ == "__main__":
    generate()
    print("Wrote the silicon inventory sprite and 16x pixel source.")
