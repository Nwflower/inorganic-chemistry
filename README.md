# Inorganic Chemistry Basics / 无机化学基础

- Mod ID：`elemental_industry`
- 路线：正式版官方 `IMod`
- Mod 版本：`1.0.0`
- 目标游戏版本：`0.46`
- 游戏目录：`E:\SteamLibrary\steamapps\common\Probably Stolen`

## 功能范围

一个单包、数据驱动的工业链：

| 模块 | 内容 |
|---|---|
| Core | 内容数据库、物品/机器工厂、通用配方执行器、危害标签、公共 API |
| GasProduction | H₂/O₂/N₂/Cl₂ 气瓶、水电解、空气分离 |
| Energy | 可插入机器的燃料能量电池、氢氧焊接/修复机 |
| Metallurgy | 铁锈提取、原版熔炉化学模块与焦炭滤材 |
| ChemicalProcessing | 盐水、氯碱、盐酸、合成氨、硝酸、化肥、双氧水、漂白粉、净水片、药品柜 |
| Electronics | 硅砂、硅、硅片、集成电路、模组芯片、机器框架、催化剂 |
| WorldIntegration | 掉落注入、化学家商店、起始方案、手册、中英文本地化 |

共 26 个数据物品、6 台机器、1 块燃料能量电池和 21 条配方，另有手册及机器说明卡。焦炭与工业硅配方并入原版熔炉化学模块，不再注册独立矿热炉、铁矿石、还原炉和高炉。本开发版不提供移除设备的旧存档兼容。

## 核心机制

- 物品注册：`ModHook.OnModItemDirectoryInit` + `ItemDirectory.CreateEmptyItem`。
- 机器：`MachineHelper.CreateMachineryWindow`、`MachineryHelper.InitMachinery`、`onCycleEndSlotItemFunc` 每夜结算。
- 配方：通用 `Processor` 按 JSON 输入/输出/耗电/水量匹配；多件产物先预留空间，再扣原料和电。气体可以合入已有同类堆叠，扣电失败时恢复重量和品质；其他产物仍需独立空位。
- 配方选择与状态：通用流程机器窗口底部列出可用配方，可逐条停用；选择按机器保存在物品标签中并随存档保留。状态面板显示启用数量、当前每晚能力，以及上次运行的批数和停止原因。单配方机器只显示状态，不重复放置无效开关。全部配方停用后不扣材料、水或电；多配方机器仍按 JSON 顺序尝试，首个满足条件的配方执行本批。
- 气瓶：`GAS_TYPE` 表示气体种类，`unitCount` 表示克数。同种气体可合并，不同气体不能合并；数量受游戏整数上限约束。新生成气瓶默认含 100 克；每 100 克的基础价值暂定氢 60、氧 5、氮 3、氯 90。
- 电解器：每晚基础消耗 10 点电和 1 升实际水分，产出 112 克氢和 888 克氧。燃料电池按 10 克氢供电 1，单批可发 11 点并保留 2 克氢；5 批合计可发 56 点。空气分离机基础每晚消耗 6 点电，产出 222 克氧和 778 克氮，共 1 千克。
- 配料：配方输入可以写 `purity`（如 `INGOT_PURITY_LOW`），同一物品的不同纯度据此区分。执行器把一堆原料按 `unitCount` 拆成 N 份，配方要几份就扣几份，剩下的留在堆里。
- 纯度：铁与硅都只保留一件物品，用原版 `IngotPurityHelper`（6 档）表达「海绵铁/高纯度铁锭」「工业硅/电子级硅」，价值随纯度浮动，不给每个纯度单独做贴图。
- 燃料能量电池：1×2、初级违禁品，放入机器电池槽；内部有氢气、可选氧气和产水容器三个槽。每 10 克氢供电 1，不足 10 克的部分保留；如果放入氧气与容器，每 1 克参与反应的氢搭配 8 克氧可产生 9 毫升纯水。容器不存在或可用空间不足时仍消耗氢供电，未用于产水的氧气保留。纯水与容器已有液体混合。它不适用于充电机、装备或飞船电源架。
- 原版熔炉：安装化学模块后可将氧化铁按普通矿石熔炼，焦炭在成功炼锭时提高一档纯度。3 份废料耗电 8 制 2 份焦炭；2 份硅砂加 2 份焦炭耗电 16 制工业硅。
- 焦炭：可放入接受滤芯的水容器或水产设备，沿用原版滤芯耐久和颗粒过滤，并有限量吸附化学污染物；不能杀菌。用作熔炉助剂时整件消耗。
- 操作说明：新创建的模组机器附原版风格的可取出说明卡；读档重建时不重复创建。燃料电池的用法列在自身说明与手册中。
- 氢氧焊机：基础每次耗电 8，并使用 10 克氢和 80 克氧修复一台机器；质量模块可减少用气，比例保持 1:8。目标版本没有公开的原地修复 API，因此以替换实现，不保留内部物品。
- 通用模块：效率影响耗电；其他流程机器的性能影响每晚完整批次，质量影响非金属产物估值、硅纯度或焊机用气。电解器使用原版的 1:2 效率修正，性能按比例提高每晚的耗水和基础氢氧产量，质量在不额外耗水时增加氢气；气体数量按克四舍五入。气体合并时按重量平均品质。
- 涉及气体或化工的模块有依赖关系；关闭上游模块会级联禁用下游模块，避免半残配方。
- 两级开关：`Data/modules.json` 控制流程，`Data/buildings.json` 控制单台机器；均在 Mod 配置中显示。关闭综合设备也会关闭它承担的全部工序。
- 化工反应釜承担气体反应及净水片；芯片台承担硅提纯、切片与电子件。气体配比按化学计量暂定，但游戏化学品包的体积不构成完整的质量守恒模型。

## 获取方式

- 原始材料：注入原版 `materialTable` / `junkTable`，按模块开关过滤。
- 机器与化学品：通过独立的夜间化学家商店获得；原版试剂、模组化学品、原版医疗品、原版机器、模组机器和机器储藏区按货架容量供货。起始方案提供基础设备。
- 化学家商店：`Shop/ChemistShopLocation.cs` 注册独立 `AfterhourLocation`，复用原版夜间柜台与离店流程；`Shop/ChemistStockPlan.cs` 是纯函数货表，较大的商品优先尝试摆放，小件按游戏日期轮换顺序。商店不向发明家注入商品。
- 储藏：开局优先发放实验区 `elem_laboratory_bay`，复用原版扩展机器储藏区的外形及收纳规则，外形 4×4、内部 14×18 格。开局库存仍须有空间放下它。
- 药品柜 `elem_medicine_cabinet` 有 12 个独立 2×2 物品槽，按工具箱的 `GameSlotInventory` 模式限制每槽一种物品。原版药品一格一件、不堆叠；模组定量化学/医疗物品可在柜内同 ID 堆叠至 99，离开药品柜后不会在其他库存自动堆叠。
- 完整生产链：矿石/硅砂/盐 → 中间体 → 机器与电子件。
- F8 调试分类仍提供四种气瓶套件、全机器套件和原料套件，方便实机验收。

## 数据与代码结构

| 路径 | 作用 |
|---|---|
| `Api/ElementalIndustryApi.cs` | 对外注册与气体判断/消耗 API |
| `Core/ContentModels.cs` | JSON 数据模型 |
| `Core/ContentDatabase.cs` | 加载、校验、模块过滤 |
| `Core/ModuleSwitches.cs` | 模块/建筑开关与依赖级联 |
| `Content/ItemFactory.cs` | 数据物品工厂与模组药品柜堆叠策略 |
| `Machines/ProcessMachineFactory.cs` | 通用流程机器 |
| `Machines/ProcessControlPanel.cs` | 配方开关、产能和上次运行状态面板 |
| `Machines/RecipeSelection.cs` | 按机器保存的配方启用状态 |
| `Machines/Processor.cs` | 每夜配方执行器（含堆叠扣减、纯度匹配与停止原因） |
| `Machines/FuelCellMachine.cs` | 按需消耗燃气的可插拔电源 |
| `Machines/FurnaceChemistry.cs` | 原版熔炉模块和焦炭滤材接入 |
| `Machines/MachineManual.cs` | 可取出的设备说明卡 |
| `Content/LaboratoryBay.cs` | 实验区及原版收纳规则 |
| `Machines/WelderMachine.cs` | 破损机器替换机器 |
| `Shop/ChemistShopLocation.cs` | 独立夜间化学家商店 |
| `Shop/ChemistStockPlan.cs` | 商店货表与每日轮换（纯函数） |
| `Content/MedicineCabinet.cs` | 12 个固定医疗格与收纳规则 |
| `Content/MedicineCabinetRules.cs` | 槽位接受/同类堆叠纯规则 |
| `Gas/GasContainer.cs` | 气体类型标签、按克合并与消耗 |
| `Machines/ProductQuality.cs` | 产品品质、气体估值及合并品质计算 |
| `Data/*.json` | 全部模块、建筑、物品、机器、配方、掉落数据 |
| `Localization/zh.csv`、`en.csv` | 文本；两种语言各自单语，不再混排 |
| `Books/elem_field_manual.json` | 可生成的手册物品 |
| `Textures/Items/` | 物品与机器运行时贴图 |
| `tools/generate_vanilla_inspired_art.py` | 基于本机原版参照生成机器、气瓶、矿石和电池贴图 |
| `tools/generate_gas_bottles.py` | 从独立气瓶原稿生成四种气瓶贴图 |
| `ArtSource/` | 正式 16×源图、参考图与工单归档，不进包 |

## 构建与校验

```powershell
python tools/psmod.py validate Modding/ElementalIndustry
dotnet build Modding/ElementalIndustry/ElementalIndustry.csproj -c Release
python tools/psmod.py validate Modding/ElementalIndustry/bin/Release/netstandard2.1 --package --route official
python -m unittest tests.test_elemental_industry_data -v
dotnet run --project tests/ElementalMachineTransactions/ElementalMachineTransactions.csproj -c Release
```

构建只写入本工程 `bin/`，不会自动安装。未获用户明确指示不部署、不上传工坊。

## 素材

- 运行时贴图：`Textures/Items/`；气瓶与燃料电池为 16×32（1×2），流程机器为 48×48（3×3），硅砂和氧化铁使用原版矿石的 48×32 不规则形状。
- 机器、电池与矿石参考原版对应物品；气瓶使用独立的带阀门金属瓶身，并参考原版像素风格。`python Modding/ElementalIndustry/tools/generate_gas_bottles.py` 单独生成气瓶；`generate_vanilla_inspired_art.py` 生成整套贴图。
- 正式 16×源图：`ArtSource/Items/16x/`；原版 Sprite 参考：`ArtSource/References/`。
- 16× 工作流：改完源图后运行 `python Modding/ElementalIndustry/tools/compress_textures.py`；硬验收：

```powershell
python Modding/ElementalIndustry/tools/check_textures.py
```

要求：尺寸精确；alpha 只有 0/255；颜色 ≤ 32；16×源图须能还原运行时贴图；规则形状每个 16×16 网格的中心采样像素必须不透明，矿石保留原版不规则占格。

## 已知限制

- 尚未实机加载与功能验收；编译、静态校验和离线数据测试不等于游戏内行为。
- 气瓶就地合并依赖原版拖放叠放；重量上限为有符号 32 位整数。离线用例不覆盖实际拖放手感。
- 化学家商店沿用原版离店转移流程。离开前请先将购买物收好并为库存预留空间；若所有接收位置都满，原版流程可能丢失柜台上的物品。
- 化学家商店复用原版夜间舞台与化学家肖像，尚未制作专属场景；柜台只有在前台、后台均无人占用时才会铺货，否则本次不供货。货表含 4×4 的实验区和药品柜；空间不足的货物会跳过并留待后续访问。
- 本版本处于开发期，不提供旧版机器或旧存档迁移。旧气瓶的数量会被解释为克数，机器槽位也已调整；已保存的电解器、焊机、提取机、反应釜和芯片台可能保留旧基础耗电与说明，应使用新测试存档或重新创建设备验证本次数值。
- 轻量气体：无压力、温度、混合、管网和真实扩散；气体只存在于气瓶物品。
- 每批 112 克氢理论可供电 11.2，但供电按整数结算，先发 11 点并留下 2 克氢；5 批用电 50、发电 56，净多 6 点。若装入氧气和足够大的容器，5 批可回收 4.995 升水。
- 原版基础效率模块 +16% 经 1:2 修正使电解耗电降至 9，5 批用电 45、发电 56，理论净多 11 点。机器需要启动电源，玩家还需转移气体和回收的水；模块组合的实际盈亏有待实机验证。
- 按原版模块属性理论计算，4 个神经核心模组（受限，`system_capped_neural_core`）合计可使电解器每晚耗电 30、耗水 5 升，产 1680 克氢和 4440 克氧；氢发电 168，净多 138，配氧还可回收 4.995 升水。该组合仍需实机验证。
- 纯度沿用原版金属纯度文案（英文「Metal Purity: …」），未做本地化改写；用的是游戏自带文本，不属于模组双语混排。
- 危害标签目前只进入 tooltip，没有独立的辐射/毒气扩散结算。
- 氢氧焊机通过生成同型号新机器来修复，不保留破损机器内部残留模块。
- 氧气种子筛选仍是实验性玩法，和原版基因扫描仪定位重叠。
- 非气体产物不会利用已有堆叠中的空余容量，输出区需留出独立空位。
- 配方状态只显示最近一次夜间运行的汇总，不保留逐配方历史；一次停止只能报告最具操作性的单一原因，多个配方同时缺料时不会逐一列出。
- 多产物处理对普通空间不足和扣电失败采取回滚；无法保证第三方回调或后续扣水、销毁、充电时抛异常的所有副作用都能回滚。
- 实验区不是停机开关；装入其中的通电机器可能仍在夜间工作，整理前应取出电池和投入物。接受条件和实际摆放仍需实机验证。
- 熔炉化学模块和燃料电池接入目标版本的原版运行时；游戏更新后应重新检查 Harmony 补丁目标。滤材、机器结算、说明卡和保存/读档仍需实机验证。

设计依据：[气体工艺与工程清单](../../docs/research/元素周期表/元素核心-气体工艺与工程清单.md)、[逐元素可行性](../../docs/research/元素周期表/元素周期表拓展-可行性研究.md)、[全元素矩阵](../../docs/research/元素周期表/元素周期表拓展-全元素可行性矩阵.md)。
