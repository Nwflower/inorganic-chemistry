# 原版贴图替换工单

> 历史工单：后续的 `2026-10-31-compact-stations` 工单已调整机器尺寸、四种化学品和机器框架。不要直接重跑本工单的 `process_textures.py`，否则会覆盖这些修订；请使用后续工单的生成与验收流程。

- 状态：已完成静态验收
- 日期：2026-10-31
- 工具：ChatGPT 生图工具（35 张独立原图）
- 范围：`Data/items.json` 中 22 个 `copySpriteFrom` 物品，`Data/machines.json` 中 13 个 `copySpriteFrom` 机器。
- 不在范围：氮/氯气瓶与氯碱槽复用的是模组内已有贴图，并非原版 Sprite。

## 风格与生产

统一参考原版物品的硬边像素画、深紫黑描边、奶油白/蓝灰工业色与少量橙/青功能色。化学品按瓶型、液体色和危险图标区分；冶金与电子原料按轮廓和材质区分；机器按实际功能设计管线、炉膛、反应釜、压机、晶体生长和晶圆加工结构。

生成原图经透明边裁切、NEAREST 缩放、硬 alpha 与每图 24 色以内量化，保存为：

- 物品：16×16 运行时 PNG，256×256 的 16×源图。
- 机器：48×48 运行时 PNG，768×768 的 16×源图。

`process_textures.py` 保存本轮生成源路径与后期处理过程；`update_data.py` 保存引用切换过程。运行时资源位于 `Textures/Items/`，正式源图位于 `ArtSource/Items/16x/`。

## 验收

- 回读 `ArtSource/Previews/vanilla-replacement-items.png` 与 `vanilla-replacement-machines.png`。
- 运行 `python Modding/ElementalIndustry/tools/check_textures.py`，检查全部 41 张运行时贴图。
- 数据、构建与包级校验按工程 README 执行。
- 不修改 manifest、版本号或 C#，不部署、不上传创意工坊。
