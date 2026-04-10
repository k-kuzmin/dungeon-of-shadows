"""
Упаковка индивидуальных PNG-спрайтов предметов в единый спрайтшит.
Layout: 8 cols x 8 rows, каждая ячейка 16x16 px. Итого 128x128 px.

Row 0: swords/sword_01[a-e]
Row 1: armors/armor_01[a-e]
Row 2: rings/ring_02[a-e]
Row 3: rings/ring_03[a-e]
Row 4: amulets/necklace_03[a-e]
Row 5: potions/potion_01a, potion_01b, potion_02a, potion_02b, potion_03a, potion_03b
Row 6: scrolls/scroll_01[a-h]
Row 7: spellScrolls/scroll_01[a-h]

Запуск: python tools/pack_items.py
"""

from PIL import Image
from pathlib import Path

TILE = 32
COLS = 8
ROWS = 8
BASE = Path(__file__).resolve().parent.parent / "assets" / "sprites" / "items"
OUT = BASE / "items.png"

# (row, [(subfolder, filename), ...])
LAYOUT = [
    # Row 0: swords
    (0, [("swords", f"sword_01{c}.png") for c in "abcde"]),
    # Row 1: armors
    (1, [("armors", f"armor_01{c}.png") for c in "abcde"]),
    # Row 2: rings type 1
    (2, [("rings", f"ring_02{c}.png") for c in "abcde"]),
    # Row 3: rings type 2
    (3, [("rings", f"ring_03{c}.png") for c in "abcde"]),
    # Row 4: amulets
    (4, [("amulets", f"necklace_03{c}.png") for c in "abcde"]),
    # Row 5: potions
    (5, [
        ("potions", "potion_01a.png"), ("potions", "potion_01b.png"),
        ("potions", "potion_02a.png"), ("potions", "potion_02b.png"),
        ("potions", "potion_03a.png"), ("potions", "potion_03b.png"),
    ]),
    # Row 6: scrolls
    (6, [("scrolls", f"scroll_01{c}.png") for c in "abcdefgh"]),
    # Row 7: spell scrolls
    (7, [("spellScrolls", f"scroll_01{c}.png") for c in "abcdefgh"]),
]


def main():
    sheet = Image.new("RGBA", (COLS * TILE, ROWS * TILE), (0, 0, 0, 0))

    for row, sprites in LAYOUT:
        for col, (subfolder, filename) in enumerate(sprites):
            path = BASE / subfolder / filename
            if not path.exists():
                print(f"  SKIP (not found): {path.relative_to(BASE)}")
                continue
            img = Image.open(path).convert("RGBA")
            # Центрируем если спрайт меньше 16x16, обрезаем если больше
            cx = (TILE - img.width) // 2
            cy = (TILE - img.height) // 2
            sheet.paste(img, (col * TILE + max(cx, 0), row * TILE + max(cy, 0)))

    sheet.save(OUT, "PNG")
    print(f"Spritesheet saved: {OUT} ({COLS * TILE}x{ROWS * TILE})")


if __name__ == "__main__":
    main()
