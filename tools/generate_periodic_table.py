"""Generate the periodic table page image for the field manual.

Output: Textures/Paper/elem_periodic_table.png (paper sprites are loaded with bilinear
filtering, so this is drawn at 3x the book's design scale rather than as pixel art).
The image carries only symbols and numbers so one file serves every language.
Masses are rounded to integers (chlorine 35.5), matching the gas grams in recipes.json.
"""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Textures" / "Paper" / "elem_periodic_table.png"
FONTS = Path("C:/Windows/Fonts")

INK = (33, 28, 26, 255)
FAINT = (33, 28, 26, 110)
HIGHLIGHT = (158, 31, 26, 255)
HIGHLIGHT_FILL = (158, 31, 26, 38)

CELL = 96
GAP = 6
LEFT = 44
TOP = 44
F_BLOCK_GAP = 36

# Elements used by this mod's recipes are highlighted.
USED = {"H", "C", "N", "O", "Na", "Si", "Cl", "Fe"}

# (Z, symbol, rounded standard atomic weight or None when the element has none)
ELEMENTS = [
    (1, "H", 1), (2, "He", 4), (3, "Li", 7), (4, "Be", 9), (5, "B", 11), (6, "C", 12),
    (7, "N", 14), (8, "O", 16), (9, "F", 19), (10, "Ne", 20), (11, "Na", 23), (12, "Mg", 24),
    (13, "Al", 27), (14, "Si", 28), (15, "P", 31), (16, "S", 32), (17, "Cl", 35.5), (18, "Ar", 40),
    (19, "K", 39), (20, "Ca", 40), (21, "Sc", 45), (22, "Ti", 48), (23, "V", 51), (24, "Cr", 52),
    (25, "Mn", 55), (26, "Fe", 56), (27, "Co", 59), (28, "Ni", 59), (29, "Cu", 64), (30, "Zn", 65),
    (31, "Ga", 70), (32, "Ge", 73), (33, "As", 75), (34, "Se", 79), (35, "Br", 80), (36, "Kr", 84),
    (37, "Rb", 85), (38, "Sr", 88), (39, "Y", 89), (40, "Zr", 91), (41, "Nb", 93), (42, "Mo", 96),
    (43, "Tc", None), (44, "Ru", 101), (45, "Rh", 103), (46, "Pd", 106), (47, "Ag", 108),
    (48, "Cd", 112), (49, "In", 115), (50, "Sn", 119), (51, "Sb", 122), (52, "Te", 128),
    (53, "I", 127), (54, "Xe", 131), (55, "Cs", 133), (56, "Ba", 137), (57, "La", 139),
    (58, "Ce", 140), (59, "Pr", 141), (60, "Nd", 144), (61, "Pm", None), (62, "Sm", 150),
    (63, "Eu", 152), (64, "Gd", 157), (65, "Tb", 159), (66, "Dy", 163), (67, "Ho", 165),
    (68, "Er", 167), (69, "Tm", 169), (70, "Yb", 173), (71, "Lu", 175), (72, "Hf", 178),
    (73, "Ta", 181), (74, "W", 184), (75, "Re", 186), (76, "Os", 190), (77, "Ir", 192),
    (78, "Pt", 195), (79, "Au", 197), (80, "Hg", 201), (81, "Tl", 204), (82, "Pb", 207),
    (83, "Bi", 209), (84, "Po", None), (85, "At", None), (86, "Rn", None), (87, "Fr", None),
    (88, "Ra", None), (89, "Ac", None), (90, "Th", 232), (91, "Pa", 231), (92, "U", 238),
    (93, "Np", None), (94, "Pu", None), (95, "Am", None), (96, "Cm", None), (97, "Bk", None),
    (98, "Cf", None), (99, "Es", None), (100, "Fm", None), (101, "Md", None), (102, "No", None),
    (103, "Lr", None), (104, "Rf", None), (105, "Db", None), (106, "Sg", None), (107, "Bh", None),
    (108, "Hs", None), (109, "Mt", None), (110, "Ds", None), (111, "Rg", None), (112, "Cn", None),
    (113, "Nh", None), (114, "Fl", None), (115, "Mc", None), (116, "Lv", None), (117, "Ts", None),
    (118, "Og", None),
]


def position(z):
    """Return (row, column) with rows 0-6 for periods and 7-8 for the f-block."""
    if z == 1:
        return 0, 0
    if z == 2:
        return 0, 17
    for period, start in ((1, 3), (2, 11)):
        if start <= z < start + 8:
            offset = z - start
            return period, offset if offset < 2 else offset + 10
    if 19 <= z <= 36:
        return 3, z - 19
    if 37 <= z <= 54:
        return 4, z - 37
    for period, start, f_row in ((5, 55, 7), (6, 87, 8)):
        if z in (start, start + 1):
            return period, z - start
        if start + 2 <= z <= start + 16:
            return f_row, 2 + (z - start - 2)
        if start + 17 <= z <= start + 31:
            return period, 3 + (z - start - 17)
    raise ValueError(z)


def cell_origin(row, column):
    y = TOP + row * (CELL + GAP) + (F_BLOCK_GAP if row >= 7 else 0)
    return LEFT + column * (CELL + GAP), y


def font(name, size):
    return ImageFont.truetype(str(FONTS / name), size)


def centered(draw, box, text, typeface, fill):
    left, top, right, bottom = box
    bounds = draw.textbbox((0, 0), text, font=typeface)
    width, height = bounds[2] - bounds[0], bounds[3] - bounds[1]
    draw.text((left + (right - left - width) / 2 - bounds[0], top + (bottom - top - height) / 2 - bounds[1]),
              text, font=typeface, fill=fill)


def main():
    width = LEFT + 18 * (CELL + GAP) + 8
    height = TOP + 9 * (CELL + GAP) + F_BLOCK_GAP + 8
    image = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    symbol_font = font("arialbd.ttf", 36)
    small_font = font("arial.ttf", 20)
    mass_font = font("arial.ttf", 21)
    label_font = font("arialbd.ttf", 22)

    for column in range(18):
        x, _ = cell_origin(0, column)
        centered(draw, (x, 4, x + CELL, TOP - 6), str(column + 1), label_font, FAINT)
    for period in range(7):
        _, y = cell_origin(period, 0)
        centered(draw, (0, y, LEFT - 6, y + CELL), str(period + 1), label_font, FAINT)

    for z, symbol, mass in ELEMENTS:
        row, column = position(z)
        x, y = cell_origin(row, column)
        used = symbol in USED
        colour = HIGHLIGHT if used else (INK if mass is not None else FAINT)
        if used:
            draw.rectangle((x, y, x + CELL, y + CELL), fill=HIGHLIGHT_FILL)
        draw.rectangle((x, y, x + CELL, y + CELL), outline=colour, width=4 if used else 2)
        draw.text((x + 8, y + 5), str(z), font=small_font, fill=colour)
        centered(draw, (x, y + 28, x + CELL, y + 68), symbol, symbol_font, colour)
        if mass is not None:
            text = f"{mass:g}"
            centered(draw, (x, y + 70, x + CELL, y + CELL - 4), text, mass_font, colour)

    # Placeholders for the lanthanides and actinides in group 3.
    for row, label in ((5, "57-71"), (6, "89-103")):
        x, y = cell_origin(row, 2)
        draw.rectangle((x, y, x + CELL, y + CELL), outline=FAINT, width=2)
        centered(draw, (x, y, x + CELL, y + CELL), label, small_font, FAINT)

    OUT.parent.mkdir(parents=True, exist_ok=True)
    image.save(OUT, optimize=True)
    print(f"{OUT.relative_to(ROOT)} {image.size[0]}x{image.size[1]}")


if __name__ == "__main__":
    main()
