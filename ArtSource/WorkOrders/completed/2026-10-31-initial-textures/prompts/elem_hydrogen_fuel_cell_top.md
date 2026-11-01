# 贴图任务：燃料电池顶盖 overlay / Hydrogen Fuel Cell Top

- 目标运行时文件：`Textures/Items/elem_hydrogen_fuel_cell_top.png`
- 运行时尺寸：80x32（5x2 格）
- **生图输出尺寸：1280x512（16× 放大；最终由脚本按 1/16 NEAREST 缩回）**
- 透明背景：是
- 用途：这是燃料电池的**最后一层 overlay**，只画顶盖、侧边、接口和底部压条；中间必须透明，让下面的电池和氢气罐显示。

## 参考图（原版物品 Sprite，不是游戏截图）

- `ref1-original-recharger-top-6x.png`：原版 `recharger_top`，6× NEAREST 放大；结构参考。
- `ref2-original-recharger-base-6x.png`：原版 `recharger_base`，6× NEAREST 放大；颜色/描边参考。
- `ref3-original-battery100-8x.png`：原版 `battery100`，8× NEAREST 放大；电池高度参考。

## 构图

80×32 的透明 overlay：顶部横梁（含 H2 蓝色接口、O2 青色接口与两颗螺丝）、左右窄侧轨、底部细压条；中间 4 个电池位 + 第 5 个氢气罐位全部留空透明。不要画电池、气罐、闪电或文字。

## 输出要求

- 只输出一张大尺寸 PNG，透明背景；保持 1280x512 的生成尺寸。
- 12～20 个颜色，硬边像素画，无渐变/抗锯齿/软阴影。
- 生成后由 `tmp/_work/compress_and_overwrite.py` 缩回；不要直接把大图放进 `Textures/Items/`。
