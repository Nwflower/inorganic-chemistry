# 2×3 气罐贴图原稿

使用内置 imagegen 编辑上一版独立气瓶原稿，并仅以原版水容器作为灰紫色像素风格参考。主要提示词：

> Redesign the gas-cylinder sprite for a 2-by-3 game inventory footprint. Give it broad, straight, parallel sidewalls with the same width through the top, middle, and bottom of the body; a short shoulder and small top valve, and a broad bottom ring almost as wide as the body. Do not make the body barrel-shaped, skinny, tapered, or bulbous. One upright compressed-gas cylinder only. Recreate it on a notional 32-by-48-pixel grid with large square pixels, hard edges, sparse metallic highlights, and dark purple outlines like the reference game. No letters, numerals, labels, icons, watermark, filter shape, other objects, or background. Preserve true transparency outside the object.

`gas-canister-master.png` 为内置工具的透明原稿。`tools/generate_gas_bottles.py` 对称裁掉多余留白、按最近邻缩放至 32×48、限制调色板，并添加不同气体的标牌；原稿不包含字体。氢、氧、氮标签带下标 2，氯气瓶标签按要求只显示 `Cl`。
