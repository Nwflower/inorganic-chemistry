from pathlib import Path

import numpy as np
from PIL import Image

root = Path(__file__).resolve().parents[1]
src_16x = root / "ArtSource" / "Items" / "16x"
targets = [
    root / "Textures" / "Items",
    root / "bin" / "Release" / "netstandard2.1" / "Textures" / "Items",
]
for source in sorted(src_16x.glob("*.png")):
    name = source.name
    big = Image.open(source).convert("RGBA")
    if big.width % 16 or big.height % 16:
        raise ValueError(f"{name}: 16x source dimensions must be divisible by 16, got {big.size}")
    lw, lh = big.width // 16, big.height // 16
    small = big.resize((lw, lh), Image.Resampling.NEAREST)
    a = np.array(small)
    # Image-model output may have soft alpha; the game uses point filtering, so harden it.
    a[:, :, 3] = np.where(a[:, :, 3] >= 128, 255, 0)
    small = Image.fromarray(a, "RGBA")
    for tdir in targets:
        if not tdir.exists():
            print("skip missing dir:", tdir)
            continue
        small.save(tdir / name)
        print("overwrote:", tdir / name, small.size)
