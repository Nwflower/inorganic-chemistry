"""Asset invariants for machine state changes; run with python tools/test_machine_states.py."""
from pathlib import Path
import unittest

from PIL import Image

TEX = Path(__file__).resolve().parents[1] / "Textures/Items"


class MachineStateTests(unittest.TestCase):
    def image(self, name, suffix=""):
        return Image.open(TEX / f"elem_{name}{suffix}.png").convert("RGBA")

    def test_states_only_change_indicators_and_contents(self):
        regions = {
            "air_separator": ((24, 27, 26, 27),),
            "rust_extractor": ((11, 34, 13, 34), (22, 26, 40, 32)),
            "chip_fab": ((39, 20, 41, 22), (17, 23, 31, 27)),
        }
        for name, boxes in regions.items():
            base = self.image(name)
            suffixes = ("_powered",) if name == "air_separator" else ("_powered", "_loaded", "_powered_loaded")
            for suffix in suffixes:
                variant = self.image(name, suffix)
                self.assertEqual(base.getchannel("A").tobytes(), variant.getchannel("A").tobytes())
                changed = 0
                for y in range(48):
                    for x in range(48):
                        if base.getpixel((x, y)) != variant.getpixel((x, y)):
                            changed += 1
                            self.assertTrue(any(x0 <= x <= x1 and y0 <= y <= y1 for x0, y0, x1, y1 in boxes),
                                            (name, suffix, x, y))
                self.assertGreater(changed, 0, (name, suffix))
                self.assertLess(changed, 150, (name, suffix))

    def test_power_does_not_change_contents(self):
        for name in ("rust_extractor", "chip_fab"):
            empty, powered, loaded, both = [self.image(name, suffix) for suffix in ("", "_powered", "_loaded", "_powered_loaded")]
            for y in range(48):
                for x in range(48):
                    p = (x, y)
                    if loaded.getpixel(p) != empty.getpixel(p):
                        self.assertEqual(loaded.getpixel(p), both.getpixel(p), (name, p))
                    if powered.getpixel(p) != empty.getpixel(p):
                        self.assertEqual(powered.getpixel(p), both.getpixel(p), (name, p))

    def test_empty_extractor_has_no_sediment(self):
        for suffix in ("", "_powered"):
            empty = self.image("rust_extractor", suffix)
            for y in range(26, 33):
                for x in range(22, 41):
                    r, g, b, _ = empty.getpixel((x, y))
                    self.assertFalse(r > g * 1.15 and r > b * 1.15, (suffix, x, y))


if __name__ == "__main__":
    unittest.main()
