# 空分机、提取机、模组装配台重绘

使用内置 imagegen 工具分别生成三张透明母图，风格参考为 `ArtSource/References/native/` 中的 `evaporator.png`、`furnace.png` 和 `chem_finisher.png`。完整提示词见 `prompts.json`。

- `air_separator-master.png`：双分离柱、蓝绿色出口色带与底部进气风机。
- `rust_extractor-master.png`：左侧过滤柱、右侧含锈色沉积物的收集罐与底部收集抽屉。
- `chip_fab-master.png`：封闭式自动装配机柜，观察窗内为加工头和电路板，正面设进料槽、成品出口与控制面板。该版本替换了先前的开放工作台设计，最终提示词见 `chip-fab-machine-prompt.json`（`prompts.json` 中的同名条目为初稿记录）。
- `*-before.png`：本次重绘前的运行时贴图，仅供对比，不参与生成。

在工程目录执行 `python tools/generate_native_fit_assets.py --machines-only`，由母图自动裁边、最近邻缩放、限制为最多 20 色，再应用 `tools/refine_machine_states.py` 中的局部像素修整。最终贴图不超过 32 色，输出 48×48 硬透明运行时贴图、768×768 最近邻源图，以及 `ArtSource/Previews/machine-redraw-comparison.png`。预览依次展示修改前 4 倍、修改后 4 倍与修改后原尺寸。

完整素材生成入口也调用该步骤，避免旧版原版素材改色逻辑覆盖新设计。三件设备保持原有 3×3 占格与物品 ID；配方和生产逻辑未改动。

## 状态精修

以现有像素图为基础做确定性局部修改，没有重新生成母图。补回空分出口色带和电路板颜色；不同状态仅改变指示灯、罐内沉积物或电路板，机壳、管道、玻璃轮廓、透明掩码和占格完全一致。

- 无后缀：缺电、空舱。
- `_powered`：供电、空舱。指示灯表示电池满足至少一种配方的当前耗电量，包含模块修正；不表示完整配方已就绪。
- `_loaded`：缺电、有料。
- `_powered_loaded`：供电、有料。

空分机直接取空气，没有材料槽，只使用前两种状态。提取机根据液体中的矿物或重金属含量是否大于零显示沉积物，清水与空容器不显示；不足一批时仍显示已有内容。装配机根据输入格是否有可接受材料显示内部电路板，材料不足或缺关键材料不等于可以开工。

`Machines/ProcessMachineAppearance.cs` 在槽位增删、数量变化、夜间处理结束、读档、打开窗口和每秒刷新时同步状态；每秒刷新补充检测原地充电、耗电及液体成分变化。状态不变时不重复设置贴图。预览见 `ArtSource/Previews/machine-state-variants.png`，列顺序为缺电空舱、供电空舱、缺电有料、供电有料。

原版依据来自本机 `Assembly-CSharp.dll`：`MachineEvaporator` 依据容器插拔切换 `evaporator` / `evaporator_b`；`MachineHydroponic.UpdateSprite` 依据加工状态、供电和用水切换 `hydroponic` / `hydroponic_off`。这是各机器自己的显示逻辑，`SetupBatterySlot` 并不会自动提供通用通电贴图。

状态资产回归：`python tools/test_machine_states.py` 检查透明掩码、允许变化范围、供电与内容独立性、空提取罐不残留沉积物。运行时状态变化仍需实机验收。

验证：`tools/check_textures.py` 通过，包含九个占格采样中心；工作区 `tests.test_elemental_industry_data` 共 30 项通过。未部署或进行实机验收。
