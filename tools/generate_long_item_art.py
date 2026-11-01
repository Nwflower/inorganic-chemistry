"""Give the brine and bleaching-powder packages their 1x2 inventory shape."""
from pathlib import Path
from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
WORK = ROOT / "ArtSource/WorkOrders/completed/2026-11-01-long-reagent-packages/originals"
RUNTIME = ROOT / "Textures/Items"
HIGH_RES = ROOT / "ArtSource/Items/16x"
ITEMS = ("elem_brine", "elem_bleaching_powder")


def generate_one(name):
    original = Image.open(WORK / f"{name}.png").convert("RGBA")
    alpha = original.getchannel("A").point(lambda value: 255 if value >= 128 else 0)
    bounds = alpha.getbbox()
    if bounds is None:
        raise ValueError(f"{name}: source has no opaque pixels")

    package = original.crop(bounds).resize((14, 28), Image.Resampling.NEAREST)
    package.putalpha(package.getchannel("A").point(lambda value: 255 if value >= 128 else 0))
    icon = Image.new("RGBA", (16, 32), (0, 0, 0, 0))
    icon.alpha_composite(package, (1, 2))
    icon.save(RUNTIME / f"{name}.png")
    icon.resize((256, 512), Image.Resampling.NEAREST).save(HIGH_RES / f"{name}.png")


if __name__ == "__main__":
    for item in ITEMS:
        generate_one(item)
    print(f"Wrote {len(ITEMS)} tall reagent sprites and 16x pixel sources.")
