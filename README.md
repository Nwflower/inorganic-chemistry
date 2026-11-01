# Inorganic Chemistry Basics / 化学拓展：无机化学基础

《深空当铺 / Probably Stolen》模组：一套数据驱动的无机化学工业链。气瓶、电解、熔炉化学模块、硅与电子件、夜间化学家商店，都挂在原版玩法上，不另开一套界面。

需要游戏正式版（运行时版本 0.46D）。当前是开发中的版本，尚未完成实机验收。

[English](#english)

## 内容

**七个模块**
Core（数据库、工厂、通用配方执行器、公共 API）、GasProduction（氢/氧/氮/氯气瓶、水电解、空气分离）、Energy（可插拔燃料能量电池）、Metallurgy（铁锈提取、原版熔炉化学模块、焦炭）、ChemicalProcessing（氯碱到净水片、药品柜）、Electronics（硅砂到模组芯片）、WorldIntegration（掉落、化学家商店、起始方案、手册）。

**规模**
26 件数据物品、6 台机器、1 块燃料能量电池、22 条配方，另有可生成的手册和每台机器自带的说明卡。

## 玩法

**气瓶**
四种气体各有气瓶。`GAS_TYPE` 标记种类，物品数量就是克数；同种气体拖在一起即按克合并，不同气体不能合并。克数写在物品说明里，不盖住贴图；拆分时图标旁显示待取克数。新生成的气瓶默认 100 克。

**电解与空气分离**
电解器每晚基础耗电 12、耗水 1 升，产出 112 克氢和 888 克氧。空气分离机每晚基础耗电 6，产出 222 克氧和 778 克氮。效率改变耗电，性能提高每晚批次与处理水量，质量增加产氢量或产物估值。

**燃料能量电池**
1×2 的初级违禁品，插在机器电池槽里当电源。每 10 克氢供 1 点电，不足 10 克的部分留着下次用。装了氧气和产水容器时，每 1 克氢搭配 8 克氧产出 9 毫升纯水；容器没空间就只供电、氧气留下。它不能给充电机、装备或飞船电源架用。

**原版熔炉化学模块**
装进原版熔炉后，熔炉接受氧化铁、焦炭、硅砂、硅和废料。焦炭在原版炼锭成功时提高一档纯度，可与原版助熔剂叠加；3 份废料耗电 8 变 2 份焦炭；2 份硅砂加 2 份焦炭耗电 16 炼出工业硅，再用焦炭在熔炉里逐档提纯。

**焦炭**
能放进接受滤芯的水容器和水产设备，沿用原版滤芯耐久与颗粒过滤，并有限量吸附化学污染物的作用。它不杀菌。当熔炉助剂时整件消耗。

**电子烧杯**
负责气体反应、化学品合成和净水片压制。每批耗电列在设备说明卡里，气体用量按化学计量暂定；游戏化学品包的体积不构成完整质量守恒。

**芯片台**
把硅切片做成集成电路、模组芯片、机器框架和催化剂。

**实验区与药品格**
开局优先发放实验区：4×4 的金属架，内部 14×18 格主网格，下方有 12 个独立 2×2 药品格；收纳设备不会让机器停机。架子图标按主网格与药品格中的物品条目总数切换 0、1、2、3、4+ 件五种状态。专用药品格每格一种物品，模组定量化学品与医疗品可按同 ID 堆到 99，原版药品保持单件。旧式独立药品柜保留给已有存档使用，不再由商店供应。

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
- 开发中版本不提供旧存档迁移。旧气瓶的数量会被当成克数，机器槽位也调整过；旧存档里的设备可能保留旧耗电与说明，请用新测试存档验收。
- 气体是轻量模型：没有压力、温度、混合、管网和扩散，只存在于气瓶物品里。
- 非气体产物不会塞进已有堆叠的空位，输出区要留独立空位。
- 纯度沿用原版「Metal Purity: …」文案，未另做本地化改写。
- 危害标签目前只进 tooltip，没有独立的辐射或毒气结算。
- 氧气种子筛选还是实验玩法，和原版基因扫描仪定位重叠。
- 配方状态只记最近一次夜间运行的汇总，多个配方同时缺料时只报一个最可操作的原因。
- 多产物处理对空间不足和扣电失败做了回滚，但无法保证第三方回调或后续扣水、销毁、充电抛异常时所有副作用都能回滚。
- 实验区不是停机开关：装在里面的通电机器可能照常夜间工作，整理前应取出电池和投入物。
- 熔炉化学模块和燃料电池接入游戏运行时，游戏更新后需要重新检查补丁目标。
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

**代码分工**：`Core/` 负责加载、校验、模块开关与配方执行；`Content/` 是物品、实验区、药品柜工厂；`Machines/` 是通用流程机器、原版熔炉接入和各设备；`Gas/` 是气体容器规则；`Shop/` 是夜间商店；`Api/` 是对外注册与气体判断接口。

**文本**：`Localization/zh.csv` 与 `en.csv` 各自单语，不混排。

**贴图**：`Textures/Items/` 是运行时贴图，`ArtSource/` 存 16× 源图与生成原稿，不进包。

## 许可

本项目以 [Functional Source License 1.1（MIT Future License）](LICENSE) 发布：可以自由使用、修改、分发，也可以拿去做自己的东西，但**不能把它做成与原版功能相同或近似的商业产品或服务出售**。每个版本发布满两年后，自动转为 MIT 许可。

游戏本体、Unity 与官方 Mod SDK 不在本仓库授权范围内。

## English

A *Probably Stolen* mod that adds a data-driven inorganic chemistry chain: gas cylinders, electrolysis, air separation, a chemistry module for the vanilla furnace, silicon and electronics, a night-time chemist shop, and a laboratory bay.

Requires the full release of the game (runtime version 0.46D). In development; not yet verified in game.

**Content** — 26 data items, 6 machines, 1 plug-in fuel cell and 22 recipes across seven modules (core, gas production, energy, metallurgy, chemical processing, electronics, world integration), plus a generated manual and an instruction card on every machine.

**Gas** — cylinders carry `GAS_TYPE` and grams as their count; same gases merge when stacked, different gases never do. Efficiency changes power draw, performance increases batches and water throughput, quality adds hydrogen or raises product value.

**Fuel cell** — a 1×2 low-grade contraband power source: 10 g hydrogen per 1 energy, remainders kept for later. With oxygen and a container it also turns 1 g hydrogen plus 8 g oxygen into 9 ml pure water.

**Vanilla furnace** — with the chemistry module installed, the furnace accepts iron oxide, coke, silica, silicon and junk. Coke raises ingot purity by one step per successful vanilla smelt and stacks with vanilla flux.

**Silicon and electronics** — smelt silica with coke, refine it step by step in the furnace, then slice wafers into circuits, module chips, machine frames and catalysts.

**Shop and storage** — a separate night-time chemist counter and a 14×18 laboratory bay with 12 dedicated medicine slots below its main grid. Its icon reflects the combined item count; mod chemistry and medical items stack to 99 in these slots. Legacy standalone cabinets remain usable in existing saves but are no longer sold.

**Limits** — gases are a lightweight model with no pressure, temperature, mixing or pipes; non-gas outputs need their own free cells; the mod is still in development and does not migrate old saves.

**Build** — `dotnet build ElementalIndustry.csproj -c Release`; output goes to `bin/` only. Enable the folder in the game's Mods menu and restart, then `F8` → `mod-scan elemental_industry`.

**Item catalog** — `python tools/generate_item_catalog.py` creates `item-catalog.html` with item and machine artwork, effects, category, module, value and footprint. Machine manuals can be expanded, and all laboratory-bay storage states are shown.

**License** — [Functional Source License 1.1 (MIT Future License)](LICENSE): use, modify and redistribute freely, but do not sell it as a commercial product or service that substitutes for it. Each release becomes MIT-licensed two years after it ships.
