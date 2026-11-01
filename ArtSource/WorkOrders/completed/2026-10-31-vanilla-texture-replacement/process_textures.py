from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[4]
ATTACH = Path(r"D:\.dsh\attachments\v1\objects")
OUT = ROOT / "Textures" / "Items"
HIGH = ROOT / "ArtSource" / "Items" / "16x"
PREVIEW = ROOT / "ArtSource" / "Previews"
WORK = Path(__file__).resolve().parent

# ChatGPT-generated sources, normalized by DSH. All are independently generated PNGs.
SOURCES = {
    "elem_iron_oxide": "34/34f721b51f70cfdb883b45915b546a6a5047d53468e29c5be3a109da5c894f99",
    "elem_iron_ore": "f8/f850eea81cf9f02a9806624fc3d3c22a932c6d04ee1c597d3aa11c84ded7bb17",
    "elem_sponge_iron": "8a/8a4c9023c0a088099283b476d2da73a5d8c5fd402ee8301921c8ff8b4054c4b6",
    "elem_coke": "13/13e27201327df72afd32df20ae4ab57c1e8eeaf7a2edd622b30f7f34f58a0cb1",
    "elem_salt": "79/792e5e27ec4a2764151fc3fa25d45d0cf7d71506b4a5952ad662e486c1a33bf1",
    "elem_brine": "7a/7aa59969d735e372d8c8b090d2bed1c659f57c67088e7c8c8df43e0fe39f7c8e",
    "elem_sodium_hydroxide": "c3/c344bf39aee9afedc068a35e58a28242b67c8d70463e54e0b19ecf1d462cab37",
    "elem_hydrochloric_acid": "55/55bf32bab6ad8801317795d7a895ed08f57444389707767923b124bd7beedeac",
    "elem_ammonia": "af/aff173a94c77b6c5dab812bfde1f909728dbcce8f49183a5aef868e07aa06fd7",
    "elem_nitric_acid": "e9/e96bce5aa1cc035caae242f98818e804d460867446a375a23d04b46492d7ab1f",
    "elem_fertilizer": "1e/1ee017c1c95058c90e7856feb89b64425030898d948cd382c84bc5a743489058",
    "elem_hydrogen_peroxide": "02/025f195ae25a10f6f9eefbdfca34b65f12400ea4c0643d3cb3a4921b4bc18073",
    "elem_bleaching_powder": "db/dbee3ee7ac6dac3736d0b461cdf840216e194e767b71024155f713bdc2654fd1",
    "elem_water_tablet": "a9/a92aba1e7c78ed816e19ea5a81bc2397f6a39a15f42975d93b0aa8800e6714ea",
    "elem_silica": "5f/5fb6c77147454e9c9236805936e4b4fb1b74862d15c67fbd4d9ed4c9b2ba6fa4",
    "elem_silicon": "56/5662cef5a93d93a1f8ab3eae968fb2554e6bbfd20dbb2fe1a0b064f194142a18",
    "elem_polysilicon": "8c/8ce1c1a23e993b4745b63adad8b734498ea0980ae57b865ccab60c2ef26edc0d",
    "elem_silicon_wafer": "a7/a7c7448ac061f26fbdc16c8f343bf1d6de5ad247f78c5ca32524d5f150da4e73",
    "elem_integrated_circuit": "d0/d0820e3fbd0499a45abf3bab46dc90ec863b5cc4c662bcbfd6ac53b39b7eb9d8",
    "elem_mod_chip": "0d/0dc4c7e37b71ab42933538179f419eaa4432558fca829635f6ff4d438c93ea73",
    "elem_machine_frame": "4c/4ce66dc4791106f928ac1e2f7bdb2c2fe9570a978e6ee1dac04ddbe32a858dce",
    "elem_catalyst": "42/42e40411fc7a55f66a916587d1b1b2a677dda2e78ab57d4c1e576952ee21d177",
    "elem_air_separator": "64/6431769eee7047a1af24cccc807476a00655f6c4784f0ff819618d5338a6f2dd",
    "elem_gas_filler": "13/1320d13eb6b89b5c080045fa046485fd3ea969590cd3a962b6d46e39394963de",
    "elem_oxyhydrogen_welder": "e9/e920b5217f5e51ce3fcb544f02b8f76adf1d19b2a0e7f698197a419dfa6603d4",
    "elem_rust_extractor": "00/00a9d4340fe763c78e781ae5551c30a84fe54942d7730fd0616b6c90cecfa09c",
    "elem_coke_oven": "29/29afd0f64a3ff0d0883620e1a8fa1e1026704551fbaad19f3a33cfd67fb9d1ed",
    "elem_reduction_furnace": "d4/d46fc48fb3ce3ab742de6b9dbf7325e93e668076d560be6daf25a3c7a7772a00",
    "elem_blast_furnace": "4a/4a89954f0794dc89fb411ddb98aa66fc26031cd3b3af68bdcd1e9f08c7047f49",
    "elem_chemical_reactor": "fc/fc3e274e1872b42e94d7f6e57f80dc75657b017cae0f308f0af4d5588e425fd9",
    "elem_tablet_press": "b3/b3542ba527ef3da1cd93eec66838c2d92ec3c9fa7d8b2e68ac4741b13bb7bba6",
    "elem_arc_furnace": "ac/acc791f96ee08eae10c2265a44671de0eaf0b3023e32fca5f0ae002017fb4f68",
    "elem_crystal_puller": "b0/b032f1caac15410068ba2b004a95e48c2a890a7bd247d54853e5b5340fb84cf8",
    "elem_wafer_saw": "77/778eb32e5add06d066c16fa5340c6d62309556a9b60e563ffe3562ff6d7ee989",
    "elem_chip_fab": "d7/d7a908ef3682875ad05c790b45e504b96af4183e34ec0367bdbf2635ef68f783",
}
ITEMS = set(list(SOURCES)[:22])


def process(name, source):
    im = Image.open(source).convert("RGBA")
    # Discard barely-visible generation fringe before finding the real silhouette.
    alpha = im.getchannel("A").point(lambda a: 255 if a >= 96 else 0)
    im.putalpha(alpha)
    bbox = alpha.getbbox()
    if not bbox:
        raise ValueError(f"{name}: empty alpha")
    im = im.crop(bbox)

    logical = 16 if name in ITEMS else 48
    inner = logical - 2
    scale = min(inner / im.width, inner / im.height)
    fitted = (max(1, round(im.width * scale)), max(1, round(im.height * scale)))
    im = im.resize(fitted, Image.Resampling.NEAREST)
    canvas = Image.new("RGBA", (logical, logical), (0, 0, 0, 0))
    canvas.alpha_composite(im, ((logical - fitted[0]) // 2, (logical - fitted[1]) // 2))

    # Hard alpha and a small per-sprite palette; no dithering or translucent fringe.
    a = canvas.getchannel("A").point(lambda v: 255 if v >= 128 else 0)
    rgb = Image.new("RGB", canvas.size, (0, 0, 0))
    rgb.paste(canvas.convert("RGB"), mask=a)
    quantized = rgb.quantize(colors=24, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert("RGBA")
    quantized.putalpha(a)
    quantized = quantized.resize((logical * 16, logical * 16), Image.Resampling.NEAREST)
    small = quantized.resize((logical, logical), Image.Resampling.NEAREST)

    small.save(OUT / f"{name}.png")
    quantized.save(HIGH / f"{name}.png")
    return small


def contact_sheet(names, path, zoom, columns):
    tile = (16 if names[0] in ITEMS else 48) * zoom
    label_h = 28
    rows = (len(names) + columns - 1) // columns
    sheet = Image.new("RGB", (columns * (tile + 12) + 12, rows * (tile + label_h + 12) + 12), "#20242b")
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    for i, name in enumerate(names):
        x = 12 + (i % columns) * (tile + 12)
        y = 12 + (i // columns) * (tile + label_h + 12)
        for yy in range(y, y + tile, 16):
            for xx in range(x, x + tile, 16):
                draw.rectangle((xx, yy, min(xx + 15, x + tile - 1), min(yy + 15, y + tile - 1)), fill="#303640" if ((xx - x) // 16 + (yy - y) // 16) % 2 else "#292e36")
        im = Image.open(OUT / f"{name}.png").convert("RGBA").resize((tile, tile), Image.Resampling.NEAREST)
        sheet.paste(im, (x, y), im)
        label = name.removeprefix("elem_")
        draw.text((x, y + tile + 5), label, fill="#e4e4d8", font=font)
    sheet.save(path)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    HIGH.mkdir(parents=True, exist_ok=True)
    PREVIEW.mkdir(parents=True, exist_ok=True)
    for name, rel in SOURCES.items():
        source = ATTACH / rel
        if not source.exists():
            raise FileNotFoundError(source)
        process(name, source)
        print(name)
    contact_sheet(list(SOURCES)[:22], PREVIEW / "vanilla-replacement-items.png", 6, 6)
    contact_sheet(list(SOURCES)[22:], PREVIEW / "vanilla-replacement-machines.png", 4, 4)
    print(f"Processed {len(SOURCES)} textures")


if __name__ == "__main__":
    main()
