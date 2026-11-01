# 独立气瓶造型

`gas-cylinder-master.png` 用内置 imagegen 生成；原版电池和设备图只作为像素密度、颜色和材质参考。气瓶本身采用独立的金属圆筒、圆肩、顶部阀门和底座，不复用滤芯轮廓。

提示词：

> Create a NEW single gas-cylinder sprite template for the inventory of a retro-futuristic 2D pixel-art game. Use the supplied screenshots only as style references for the game's chunky pixel scale, dark purple outlines, muted weathered metal colors, and subtle three-quarter top-down perspective. The object must unmistakably be an industrial compressed gas cylinder: cylindrical metal tank, rounded tapered shoulders, a short neck with a visible valve / valve wheel, and a sturdy bottom rim. It must have its own cylinder silhouette, not the shape of a filter cartridge, water filter, or battery. Design the entire icon on a 16-pixel-wide by 32-pixel-high pixel grid with large crisp square pixels and no antialiasing. Center one full upright object with transparent margin around it; no text, no letters, no other objects. Make the tank mostly neutral gray metal so color bands and gas labels can later be added. Provide genuinely transparent background. This is the editable master for 1-by-2 inventory-grid gas bottles.

运行 `python Modding/ElementalIndustry/tools/generate_gas_bottles.py`，从原稿缩放、量化并添加四种气体标记，生成运行时 16×32 贴图、对应 16×源图及预览。物品尺寸、ID 和机制不变。
