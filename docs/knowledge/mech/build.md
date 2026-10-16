# 建造与房间（Designation / Room / RoomStat）

> **mech 层**：本文只讲**建造标记与房间系统由什么构成、字段含义、内容清单**。
> 「工作台制作流程」见 [production.md](production.md)（配方）；
> 「建筑/家具的 statBases」见 [production.md](production.md)；
> 「房间美观与心情」见 [needs-mood.md](needs-mood.md)。
>
> **数据源与核对日期（2026-10-16）**：
> 本地 `F:\Games\GBL\rimworld\Data\`（RimWorld 1.6，全 6 DLC），中文取官方简中 DefInjected。
> 复现：`python tools/game_defs/extract_defs.py --def DesignationDef DesignationCategoryDef RoomRoleDef RoomStatDef RoomPartDef`。
>
> 规模：**DesignationDef 27 · DesignationCategoryDef 16 · RoomRoleDef 23 · RoomStatDef 13 · RoomPartDef 12**。

---

## 1. 框架

### 1.1 五个 Def 的关系

```
DesignationCategoryDef（16 个分类）── 建筑菜单分组（Architect）
  └─ DesignationDef（27 个标记）── 玩家在地图上标的「标记」
       └─ WorkGiver 看到标记 → 生成 Job 去处理

RoomRoleDef（23 种房间角色）── 房间被识别成什么（卧室/厨房/医务室…）
  └─ RoomStatDef（13 种房间属性）── 房间好不好（美观/清洁/财富/空间…）
       └─ 影响心情/工作/研究/手术等

RoomPartDef（12 种）── 组成房间的「部件」定义
  └─ RoomPart_ThingDef / _CrateDef / _BarricadeDef 等子类
```

🔑 **两条独立但联动的线**：
- **Designation（标记）**——玩家的「意图」：我要挖这里、要造这里
- **Room（房间）**——物理空间被自动识别后得到「属性」

### 1.2 建造标记 DesignationDef（27 个）

**全部在 Core**。字段极简：

| 字段 | 出现率 | 说明 |
|---|---:|---|
| `texturePath` | 100% | 标记图标（`Designations/Mine` 挖掘…） |
| `targetType` | 100% | 标在**格子**（`Cell`）还是**物件**（`Thing`）上 |
| `removeIfBuildingDespawned` | 11% | 建筑消失时移除标记 |
| `designateCancelable` | 4% | 能否取消标记（如蓝图不可取消） |
| `shouldBatchDraw` | 4% | 批量绘制优化 |

🔑 **`targetType` 只有两种**——标格子的（挖矿、造墙）或标物件的（拆除、打标签）。
这是设计标记的根本区分。

⚠️ **27 个标记的顶层中文标签为 0**（全 Core，靠 `texturePath` 的英文名和
`Keyed` 翻译），且**几乎没有额外字段**——Designation 是很轻的 Def，
真正的工作在 `WorkGiver`（见 [work.md](work.md)）里。

> 复现：`python tools/game_defs/extract_defs.py --def DesignationDef`

### 1.3 建造菜单分类 DesignationCategoryDef（16 个）

**Architect 建筑界面**的分组：结构（Structure）、生产（Production）、
家具（Furniture）、电力（Power）、运输（Shipping）、娱乐（Joy）、
安全（Security）、温度（Temperature）、Misc 等（16 个）。

🔑 **Ideology 会往里加分类**——如 `IdeoBuilding`（文化建筑），
见 [ideology.md](ideology.md)（建筑是 Ideo 的组成部分）。

### 1.4 房间角色 RoomRoleDef（23 种）

🔑 **房间角色是「这个房间是干什么的」**——自动识别（放张床=卧室），
对应不同的机制（卧室给休息、厨房给做饭效率、医务室给手术成功率……）。

**Core（12 种）**：`Room` 房间 · `None` 未分类 · `Bedroom` 卧室 ·
`Barracks` 多人宿舍 · `PrisonCell` 牢房 · `PrisonBarracks` 多人牢房 ·
`Hospital` 医务室 · `Kitchen` 厨房 · `DiningRoom` 餐厅 · `RecRoom` 娱乐室 ·
`Storeroom` 仓库 · `Workshop` 工作间 · `Laboratory` 研究室 · `Barn` 畜棚 ·
`Tomb` 陵墓

**Ideology（1 种）**：`WorshipRoom` 圣殿
**Royalty（1 种）**：`ThroneRoom` 谒见厅
**Biotech（3 种）**：`Nursery` 育儿室 · `Playroom` 游乐室 · `Classroom` 教室 ·
`DeathrestChamber` 死眠室
**Anomaly（2 种）**：`ContainmentCell` 收容室 · `CeremonialChamber` 仪式厅

🔑 **一个房间可以有多个角色**——如「卧室 + 学习室」；角色决定它同时获得
这些功能的加成。

### 1.5 房间属性 RoomStatDef（13 种）

🔑 **房间属性是「这个房间有多好」**——房间品质的核心：

| 属性 | 中文 | 影响 |
|---|---|---|
| `Beauty` | 美观 | 心情、Ideology 风格优势 |
| `Cleanliness` | 清洁 | 感染几率、手术成功率 |
| `Impressiveness` | 观感 | 心情（`+3~5`）|
| `Space` | 空间 | 舒适度（房间越大越好） |
| `Wealth` | 财富 | 心情（放贵重家具） |
| `ResearchSpeedFactor` | 研究速度系数 | 研究台在好房间里加速 |
| `AssemblySpeedFactor` | 工作速度系数 | 工作台在好房间里加速（Biotech） |
| `ReadingBonus` | 阅读加成 | 读书 |
| `InfectionChanceFactor` | 感染几率系数 | 干净房间减少感染 |
| `SurgerySuccessChanceCleanlinessFactor` | 手术成功几率清洁系数 | 干净医务室提高手术成功率 |
| `FoodPoisonChance` | 食物有毒几率 | 厨房卫生影响食物中毒 |
| `GraveVisitingJoyGainFactor` | 扫墓娱乐系数 | 陵墓扫墓的娱乐值 |
| `BiosculpterPodSpeedFactor` | 塑形舱速度系数 | 塑形舱速度（Ideology） |

🔑 **`Cleanliness` 是被低估的关键属性**——它同时影响感染和手术成功率，
「干净的医务室 + 干净的房间」是手术系统的核心（见 [hediff.md](hediff.md)）。

### 1.6 房间部件 RoomPartDef（12 种）

房间由「部件」组成——定义房间的边界/形状。子类：
`RoomPart_ThingDef`（基于物件）· `RoomPart_CrateDef`（箱子围成）·
`RoomPart_BarricadeDef`（路障）· `RoomPart_GestationTankDef`（培育舱）。

> 复现：`python tools/game_defs/extract_defs.py --def RoomPartDef`

---

## 2. 层级

### 2.1 建造的完整链路

```
玩家点 Architect 菜单
  └─ DesignationCategory（分类）── 选类型
       └─ DesignationDef（标记）── 在地图上标
            └─ WorkGiver 扫描标记 ──▶ 生成 Job
                 └─ JobDriver 执行建造/挖掘/搬运
                      └─ 产出：建筑/矿/清理
```

🔑 **标记是「意图」，WorkGiver 是「执行」**——玩家只管标，
执行全靠 WorkGiver（见 [work.md](work.md)）。写「新的建造行为」要写 WorkGiver，
不是写标记。

### 2.2 房间的识别与属性

```
玩家围出封闭空间（门/墙）
  └─ 自动识别为 Room
       ├─ 房间角色 RoomRole（放床→卧室）── 决定获得什么功能
       └─ 房间属性 RoomStat ── 决定功能有多强
            └─ 属性受房间内物件影响（美观=家具好看程度总和）
```

🔑 **房间角色给「功能」，房间属性给「强度」**：
一间卧室（角色）里有豪华床 → 高美观 + 大空间（属性）→ 休息更舒适。

### 2.3 房间与系统的多处联动

```
RoomStat.Impressiveness / Beauty ──▶ 心情（needs-mood.md）
RoomStat.Cleanliness ──▶ 感染 / 手术成功率（hediff.md）
RoomStat.ResearchSpeedFactor ──▶ 研究速度（production.md / work.md）
RoomStat.Beauty ──▶ Ideology 风格优势（ideology.md）
RoomRole.Hospital ──▶ 手术允许（hediff.md）
```

🔑 **房间属性是多系统的交汇点**——一个房间同时影响心情、健康、生产。
做「殖民地管理」类 mod，房间是最有效的杠杆。

### 2.4 房间与 Ideology 的联动

- `WorshipRoom`（圣殿）是 Ideology 角色——`PreceptDef.buildingRoomRequirements`
  可要求房间有 `RoomRequirementAllFloored` 等条件（见 [ideology.md](ideology.md)）。
- 房间的 `Beauty` 影响 Ideology 的「风格优势」（见 [ideology.md](ideology.md) §4.6）。

---

## 3. 内容

### 3.1 建造标记全表（27 个，全 Core）

按功能分组（代表性）：

| 功能 | 代表标记 |
|---|---|
| **挖掘** | `Mine` 挖掘 · `MineVein` 挖矿脉 · `MineDesignation` |
| **建造** | `Plan` 计划（蓝图）· `Blueprint` |
| **拆除** | `Cancel` 取消 · `Deconstruct` 拆除 · `Disassemble` 拆解 |
| **清理** | `CutPlant` 割草 · `Harvest` 收获 · `ChopWood` 伐木 |
| **区域** | `Home` 家 · ` stockpile` 库存区 |
| **地板** | `Smooth` 抹平 · `SmoothFloor` 整平 |
| **通电/管路** | `PowerConduit` 电线 · `SteamConduit` |

> 完整 27 行：`python tools/game_defs/extract_defs.py --def DesignationDef`

### 3.2 建筑菜单分类全表（16 个）

Core 基础分类 + Ideology 增设的文化建筑分类等。

### 3.3 房间角色全表（23 种）

见 §1.4。官方中文标签 23/23 全有。

### 3.4 房间属性全表（13 种）

见 §1.5。官方中文标签 13/13 全有。

> 完整清单：`python tools/game_defs/extract_defs.py --def RoomStatDef`

---

## 4. 解读

### 4.1 房间是「隐形的心情杠杆」

一个布置好的房间通过 `Impressiveness`（观感）+ `Beauty`（美观）+ `Space`（空间）
持续给住在里面的 pawn 心情加成。这是「殖民地为什么心情好」的核心之一。

对 mod 的意义：**做「居住品质」类系统，看 RoomStat 比改 Thought 更根本**。

### 4.2 Cleanliness 驱动健康系统

`Cleanliness` → `InfectionChanceFactor`（感染几率）+ `SurgerySuccessChanceCleanlinessFactor`
（手术成功率）。所以「干净的房间」是手术成功率的前置条件之一——
这让「打扫」成了一个真实的战略任务。

### 4.3 房间角色可以叠加

一间房放床 + 书桌 → 同时是「卧室」和「学习室」，获得休息 + 学习加成。
这是玩家自发优化的空间（不需要 mod）。

### 4.4 标记是轻量的、WorkGiver 是重的

想加一个「新的玩家操作」（如「设计灌溉」），路径是：
```
新 DesignationCategory（如需要）
  └─ 新 Designation（标记）
       └─ 新 WorkGiver（扫描这个标记并执行）── 真正的工作量
            └─ 复用现有 Job 或写新 JobDriver
```

Designation 本身几乎不用写代码；**核心工作在 WorkGiver**（见 [work.md](work.md)）。

### 4.5 房间属性的跨系统影响

一个房间同时是：
- 心情的来源（Impressiveness/Beauty）
- 健康的保障（Cleanliness → 手术/感染）
- 生产的加速（ResearchSpeedFactor/AssemblySpeedFactor）
- Ideology 的场所（风格优势）

这让「房间布局」成为 mod 平衡的核心杠杆之一。

---

## 5. modder 常见坑

1. **Designation 顶层标签为空**——靠 `texturePath` + `Keyed` 翻译，
   且几乎无字段。真正的逻辑在 `WorkGiver`。
2. **`targetType` 只有 `Cell`/`Thing` 两种**——标格子的和标物件的机制不同。
3. **房间属性影响多系统**——改 `Cleanliness` 会连带改感染、手术、心情，
   不是孤立的数值。
4. **`RoomPartDef` 是房间的形状定义**——做「非矩形房间」类 mod 要动它。
5. **房间角色叠加会累积加成**——想让某个房间只算一种功能，
   要通过标签/设施排除其他角色。
6. **Ideology 会往建筑菜单加分类**——建筑类 mod 要注意
   `DesignationCategory` 可能是 DLC 加的（见 [ideology.md](ideology.md)）。
7. **`Impressiveness` 是 Royalty 谒见厅等的门槛**——Royalty 的头衔系统依赖它
   （见后续 DLC 篇）。
