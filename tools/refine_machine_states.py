"""Small, deterministic pixel edits to the approved machine sprites.

Only the indicator and chamber rectangles vary between states. Never regenerate
the cabinet, pipework, glass outlines or alpha mask for a state change.
"""
from PIL import ImageDraw


STATE_REGIONS = {
    "air_separator": ((24, 27, 26, 27),),
    "rust_extractor": ((11, 34, 13, 34), (22, 26, 40, 32)),
    "chip_fab": ((39, 20, 41, 22), (17, 23, 31, 27)),
}


def variants(name, source):
    base = source.copy()
    draw = ImageDraw.Draw(base)
    if name == "rust_extractor":
        # Remove sediment only; preserve the glass rim and existing reflections.
        for y in range(26, 33):
            for x in range(22, 41):
                r, g, b, a = base.getpixel((x, y))
                if r > g * 1.15 and r > b * 1.15:
                    base.putpixel((x, y), (87, 103, 112, a))
    else:
        # Empty internal tray; the spindle and clamps stay in place.
        draw.rectangle((17, 23, 31, 27), fill="#2b2839")
        draw.line((17, 27, 31, 27), fill="#50545e")

    result = {}
    for loaded in ((False,) if name == "air_separator" else (False, True)):
        contents = base.copy()
        if loaded and name == "rust_extractor":
            contents.paste(source.crop((22, 26, 41, 33)), (22, 26))
        elif loaded:
            board = ImageDraw.Draw(contents)
            board.rectangle((18, 24, 30, 26), fill="#526c46")
            board.line((18, 24, 30, 24), fill="#788b5c")
            board.rectangle((21, 24, 24, 25), fill="#2b2839")
            for x in (19, 26, 29):
                board.point((x, 25), fill="#c9c6a5")
        for powered in (False, True):
            image = contents.copy()
            indicator = ImageDraw.Draw(image)
            indicator.rectangle(STATE_REGIONS[name][0], fill="#292638")
            if powered:
                x0, y0, x1, _ = STATE_REGIONS[name][0]
                indicator.line((x0, y0, x1, y0), fill="#99bca7")
                if name == "chip_fab":
                    indicator.point((39, 22), fill="#587f80")
                    indicator.point((41, 22), fill="#587f80")
            suffix = ("_powered" if powered else "") + ("_loaded" if loaded else "")
            result[suffix] = image
    return result
