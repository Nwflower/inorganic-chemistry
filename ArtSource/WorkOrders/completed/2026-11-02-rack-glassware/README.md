# 实验架玻璃器皿细化

使用内置 imagegen 工具，以用户提供的 `reference.png` 为样式参考，逐件生成透明瓶器母图。完整提示词见 `prompts.json`。

- `amber_flask.png`：上层右侧锥形瓶。
- `red_round_flask.png`：下层左侧圆底烧瓶。
- `blue_beaker.png`：下层右侧烧杯。
- `teal_cylinder.png`：上层左侧量筒。

`tools/generate_native_fit_assets.py --rack-only` 将母图转换到确定的像素尺寸和共享色板，生成独立的 64×64 透明内容层，再叠加既有空架。该命令只生成实验架，不重绘气瓶、电池或其他设备。

运行时仍按物品数量选择 0、1、2、3、4+ 状态。每个状态使用同一架体和同一份已加入的瓶器层，架体与已有瓶器不会随状态变化而重画。所有贴图维持 64×64、硬透明边缘和不超过 32 色，并同步输出 16 倍源图。
