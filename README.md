# Inorganic Chemistry Basics / 化学拓展：无机化学基础

《深空当铺 / Probably Stolen》模组：一套数据驱动的无机化学工业链。气瓶、电解、熔炉化学模块、硅与电子件、夜间化学家商店，都挂在原版玩法上，不另开一套界面。

需要游戏正式版（运行时版本 0.46D）。当前是开发中的版本，尚未完成实机验收。

[English](#english)

## 内容

**七个模块**
Core（数据库、工厂、通用配方执行器、公共 API）、GasProduction（氢/氧/氮/氯气瓶、水电解、空气分离）、Energy（可插拔燃料能量电池）、Metallurgy（铁锈提取、熔炉化学模块、焦炭）、ChemicalProcessing（氯碱流程与无残净水片）、Electronics（硅砂、硅提纯与模组装配）、WorldIntegration（掉落、化学家商店、起始方案、手册）。

**规模**
20 件数据物品、6 台设备（含燃料能量电池）、17 条机器配方，另有盐、盐水、硝酸铵肥料和无残净水片四条原版制剂机成型路径，以及手册和设备说明卡。

## 玩法

**气瓶**
四种气体各有气瓶。`GAS_TYPE` 标记种类，物品数量就是克数；同种气体拖在一起即按克合并，不同气体不能合并。克数写在物品说明里，不盖住贴图；拆分时图标旁显示待取克数。新生成的气瓶默认 100 克。

**电解与空气分离**
电解器每晚基础耗电 12、耗水 1 升，产出 112 克氢和 888 克氧。空气分离机每晚基础耗电 6，产出 222 克氧和 778 克氮。效率改变耗电，性能提高每晚批次与处理水量，质量增加产氢量或产物估值。

**燃料能量电池**
外形参考原版等离子电池的 4×2 初级违禁品，插在机器电池槽里当电源。外壳固定，内部贴图随空仓、装氢、氢氧齐备切换。每 10 克氢供 1 点电，不足 10 克的部分留着下次用。装了氧气和产水容器时，每 1 克氢搭配 8 克氧产出 9 毫升纯水；容器没空间就只供电、氧气留下。它不能给充电机、装备或飞船电源架用。

**原版熔炉化学模块**
装进原版熔炉后，熔炉接受氧化铁、焦炭、硅砂、硅和废料，效率降低 50%（基础耗电 8 变 12），化学配方都按熔炉当前耗电结算。每份氧化铁直接炼成 1 个低纯金属锭，同时放入焦炭则产出高纯锭。焦炭在原版矿石炼锭成功时提高一档纯度，可与原版助熔剂叠加。6 份废料变 1 份焦炭；2 份硅砂加焦炭炼出工业硅，再用焦炭在熔炉里逐档提纯。

**焦炭**
耐久 100 的工具，不是滤芯。熔炉提纯每次扣 10，炼工业硅扣 20，硅提纯每档扣 5。拿它点一下盛水容器可吸附化学污染物和重金属，每点耐久吸附 2 毫升，吸走的部分补成水；它不杀菌。耐久用尽即消耗。

**气体合成与原版化工**
气体合成器保留盐酸、氨、硝酸、双氧水和次氯酸钠剂五条需要气体输入的反应；氯碱电解在电解器完成。盐从含矿物质的液体中经原版制剂机提取，盐溶于水可分装盐水。氨与硝酸在原版容器中反应得到硝酸铵，经蒸发浓缩后制成肥料；含氯试剂可制成无残净水片。制剂机检查实际提取部分的组分，纯水不能冒充原料。

盐酸、硝酸、烧碱、氨、硝酸铵、双氧水和次氯酸钠在游戏液体表中保持独立身份。硝酸铵给原版水培提供矿物营养，过量仍会伤害作物；双氧水逐步消毒并分解，次氯酸钠释出原版有效氯，遇酸则产生污染。无残净水片沿用原版图标，使用时以原版净化效果的双倍剂量去除微生物和部分化学污染。产量、浓度和反应速度采用游戏化数值，不宣称真实质量守恒。

**模组装配台**
每批基础耗电 10，至少放 1 个硅；电子元件、线缆、金属锭和打印塑料一并投入。按价值区间 [总价值 + 10，硅价值 ×4 + 其他材料价值 ×2 + 30] 随机出一个原版模组。价值偏高时先补「优质」词条、再提高基础属性，补不完就把最贵的材料留在输入格；价值偏低时向上取更贵的模组，差价越大越可能变成报废或损坏模组、带上破旧状态或多一条负面词条。

**实验区与药品格**
开局优先发放实验区：4×4 的金属架，内部 14×18 格主网格，下方有 12 个独立 2×2 药品格；收纳设备不会让机器停机。架子图标按主网格与药品格中的物品总数切换五种状态。每个药品格可放一种物品，定量化学品与药品可按同类堆叠至 99 件，原版药品保持单件。

**化学家商店**
独立的夜间柜台，卖原版试剂、模组化学品、原版与模组机器、机器储藏区。较大的商品先尝试摆放，小件按游戏日期轮换；柜台被占用时本次不供货。商店不向发明家注入商品。

**配方与纯度**
配方选择与状态在机器窗口底部：可以逐条停用，选择随存档保留，状态面板显示当前每晚能力和上次停止的原因。配方输入可以指定 `purity`，同一物品的不同纯度据此区分。铁与硅用原版的 6 档 `IngotPurityHelper` 表达工业硅到电子级硅，价值随纯度浮动，不为每档单独做贴图。

**模块与开关**
`Data/modules.json` 控制流程，`Data/buildings.json` 控制单台机器，两者都显示在模组设置里。上游模块关掉会级联禁用依赖它的模块，避免出现半残配方。

## 安装

把构建产物的内容复制到正式版游戏的 `Mods/elemental_industry/`，在「模组」菜单启用后重启。

遇到问题时按 `F8` 打开控制台，输入 `mod-scan elemental_industry` 查看加载状态。

## 已知限制

- 尚未实机加载与功能验收；编译、静态校验和离线数据测试不等于游戏内行为。
- 气瓶的就地合并依赖原版拖放叠放，重量上限是有符号 32 位整数；离线用例不覆盖实际手感。
- 化学家商店沿用原版离店流程：离开前请收好购买物并留出库存空间，接收位置全满时原版流程可能丢失柜台物品。
- 商店复用原版夜间舞台与化学家肖像，没有专属场景；前台和后台都被占用时本次不铺货。
- 本次不提供旧存档迁移。已删除的模组物品不会保留兼容定义；请用新存档验收。
- 气体是轻量模型：没有压力、温度、混合、管网和扩散，只存在于气瓶物品里。
- 非气体产物不会塞进已有堆叠的空位，输出区要留独立空位。
- 纯度沿用原版「Metal Purity: …」文案，未另做本地化改写。
- 危害标签目前只进 tooltip，没有独立的辐射或毒气结算。
- 氧气种子筛选还是实验玩法，和原版基因扫描仪定位重叠。
- 配方状态只记最近一次夜间运行的汇总，多个配方同时缺料时只报一个最可操作的原因。
- 多产物处理对空间不足和扣电失败做了回滚，但无法保证第三方回调或后续扣水、销毁、充电抛异常时所有副作用都能回滚。
- 实验区不是停机开关：装在里面的通电机器可能照常夜间工作，整理前应取出电池和投入物。
- 熔炉化学模块和燃料电池接入游戏运行时，游戏更新后需要重新检查补丁目标。
- 燃料电池改成 4×2 后放进机器电池槽的显示与拖放手感尚未实机确认；液体反应每夜按原版容器温度 tick 推进，实际速率与提示刷新仍需在游戏里核对。
- 每批 112 克氢理论可供电 11.2，按整数结算可发 11 点、留 2 克氢；5 批基础电解耗电 60、燃料电池可供电 56。原版基础效率模块会把电解耗电降至 11，5 批净多 1 点。配入产物氧气时 5 批可回收 4.995 升水；这些数字与模块组合的实际盈亏仍需实机核对。

## 构建与校验

```powershell
dotnet build ElementalIndustry.csproj -c Release
python tools/generate_item_catalog.py
```

构建只写入本工程的 `bin/`，不会自动安装。目录命令会在本目录生成 `item-catalog.html`，列出物品与设备的贴图、效果、类别、模组、价值和占格；机器条目可展开查看说明书，实验区会展示所有收纳状态贴图。

[查看现有物品与设备目录](item-catalog.html)

## 数据与代码结构

**数据全在 JSON**：`Data/*.json` 是模块、建筑、物品、机器、配方和掉落的唯一来源，加一个元素通常只需要加数据、贴图和两条本地化。

**代码分工**：`Core/` 负责加载、校验、模块开关与配方执行；`Content/` 是物品、实验区和药品格；`Machines/` 是通用流程机器、熔炉接入和各设备；`Gas/` 是气体容器规则；`Shop/` 是夜间商店；`Api/` 是对外注册与气体判断接口。

**文本**：`Localization/zh.csv` 与 `en.csv` 各自单语，不混排。

**贴图**：`Textures/Items/` 是运行时贴图，`ArtSource/` 存 16× 源图与生成原稿，不进包。

## 许可

本项目以 [Functional Source License 1.1（MIT Future License）](LICENSE) 发布：可以自由使用、修改、分发，也可以拿去做自己的东西，但**不能把它做成与原版功能相同或近似的商业产品或服务出售**。每个版本发布满两年后，自动转为 MIT 许可。

游戏本体、Unity 与官方 Mod SDK 不在本仓库授权范围内。

## English

A *Probably Stolen* mod that adds a data-driven inorganic chemistry chain: gas cylinders, electrolysis, air separation, a chemistry module for the vanilla furnace, silicon and electronics, a night-time chemist shop, and a laboratory bay.

Requires the full release of the game (runtime version 0.46D). In development; not yet verified in game.

**Content** — 20 data items, 6 devices including a fuel cell, and 17 machine recipes across seven modules. Four additional products are formed through the native chemical finisher: salt, brine, ammonium nitrate fertilizer and residue-free water tablets.

**Gas** — cylinders carry `GAS_TYPE` and grams as their count; same gases merge when stacked, different gases never do. Efficiency changes power draw, performance increases batches and water throughput, quality adds hydrogen or raises product value.

**Fuel cell** — a 4×2 low-grade contraband power source shaped like the vanilla plasma cell. Its shell stays fixed while the core indicates empty, hydrogen, or hydrogen plus oxygen. Ten grams of hydrogen supply 1 energy; remainders remain for later. With oxygen and a container, each reacted gram of hydrogen plus 8 g oxygen also returns 9 ml pure water.

**Vanilla furnace** — with the chemistry module installed, the furnace accepts iron oxide, coke, silica, silicon and junk. The module costs 50% efficiency (8 base power becomes 12) and chemistry recipes pay the furnace's current draw. Each iron oxide becomes one low-purity ingot, or a high-purity one with coke. Coke raises vanilla ore ingots by one step and stacks with vanilla flux.

**Coke** — a 100-durability tool, not a filter. Furnace boosts cost 10, industrial silicon 20 and each silicon refining step 5. Use it on a water container to adsorb chemical contaminants and heavy metals at 2 mL per point; it does not kill microbes.

**Silicon and electronics** — smelt silica with coke and refine it step by step in the furnace. The module assembler spends at least one silicon plus any electronics, wire, ingots and printer plastic on one vanilla module whose value falls between total value + 10 and silicon × 4 + other materials × 2 + 30. Surplus buys a premium effect and higher base stats, otherwise the priciest material stays behind; a shortfall rolls the next module up and risks ruin, corruption, a shabby condition or a negative effect.

**Shop and storage** — a separate night-time chemist counter and a 14×18 laboratory bay with 12 dedicated medicine slots below its main grid. Its icon reflects the combined item count; mod chemistry and medical items stack to 99 in these slots. Legacy standalone cabinets remain usable in existing saves but are no longer sold.

**Limits** — gases are a lightweight model with no pressure, temperature, mixing or pipes; non-gas outputs need their own free cells; the mod is still in development and does not migrate old saves.

**Native chemistry** — the electrolyzer also splits packaged brine into chlorine, hydrogen and caustic soda. The sealed gas synthesizer handles five gas-input reactions. The vanilla container, evaporator and chemical finisher process salt, brine, ammonium nitrate fertilizer and residue-free tablets. Seven distinct liquid components preserve their identities; peroxide disinfects, hypochlorite releases native chlorine, and ammonium nitrate feeds hydroponics. Gameplay volumes and reaction rates are deliberately simplified.

**Build** — `dotnet build ElementalIndustry.csproj -c Release`; output goes to `bin/` only. Enable the folder in the game's Mods menu and restart, then `F8` → `mod-scan elemental_industry`.

**Item catalog** — `python tools/generate_item_catalog.py` creates `item-catalog.html` with item and machine artwork, effects, category, module, value and footprint. Machine manuals can be expanded, and all laboratory-bay storage states are shown.

**License** — [Functional Source License 1.1 (MIT Future License)](LICENSE): use, modify and redistribute freely, but do not sell it as a commercial product or service that substitutes for it. Each release becomes MIT-licensed two years after it ships.
