# 生产与属性（Recipe / Stat / Stuff / Bill）

> **mech 层**：本文只讲**生产-属性系统由什么构成、字段含义、内容清单**。
> 「给我一个能抄的 RecipeDef 蓝本」见 [../ref/content-blueprints.md](../ref/content-blueprints.md)；
> 「装备的 statBases 怎么改」见 [gear.md](gear.md)；
> 「工作怎么用技能掷骰」见 [work.md](work.md)。
>
> **数据源与核对日期（2026-10-16）**：
> 本地 `F:\Games\GBL\rimworld\Data\`（RimWorld 1.6，全 6 DLC），中文取官方简中 DefInjected。
> 复现：`python tools/game_defs/extract_defs.py --def RecipeDef StatDef StuffCategoryDef ThingSetMakerDef`。
>
> 规模：**RecipeDef 190 · StatDef 244 · StuffCategoryDef 6 · ThingSetMakerDef 50**。

---

## 1. 框架

### 1.1 属性 StatDef —— 一切数值的底层

🔑 **RimWorld 里几乎所有数值都是一个 `StatDef`**——移动速度、护甲、心情、
工作效率、灵能上限……244 个 Stat 构成整个游戏的数值体系。

**StatDef 的关键字段**（244 个实测）：

| 字段 | 出现率 | 说明 |
|---|---:|---|
| `defaultBaseValue` | 81% | 默认基准值（如 0 / 1 / 5） |
| `category` | 80% | 归类（`Ability` 能力 · `Apparel` 装备 · `BasicsNonPawnImportant`…，25 个类别） |
| `minValue` | 71% | 下限（很多是 0 或 1） |
| `maxValue` | 8% | 上限 |
| `toStringStyle` | 78% | 显示格式（`PercentOne` 百分比 · `FloatOne` 一位小数 · `Money` 金钱） |
| `displayPriorityInCategory` | 83% | 类别内排序 |
| `showIfUndefined` | 27% | 未定义时是否显示（false=隐藏） |
| `hideAtValue` | 26% | 到达某值时隐藏（如护甲为 0 时不显示） |
| `capacityFactors` | 16% | **与身体能力的联动**：这个 Stat 受哪些 PawnCapacity 影响（`Moving`/`Manipulation`/`Sight`…） |
| `skillNeedFactors` | 17% | **需要哪些技能才能做好**（`skill` + `baseValue` + `bonusPerLevel`） |
| `statFactors` | 7% | 这个 Stat 乘算哪些其他 Stat（如 `WorkSpeedGlobal`） |
| `workerClass` | 10% | **特殊计算类**（如 `StatWorker_PsyfocusCost` 灵能消耗） |
| `cacheable` | 7% | 可缓存（性能相关，见 [../performance-and-gui.md](../performance-and-gui.md)） |
| `ParentName` | 20% | 50/244 有父类（如 `ArmorRatingBase`） |

🔑 **`capacityFactors` 是属性与身体的接口**——护甲值受 `Sight`/`Moving` 等
能力影响；没手（`Manipulation` 低）会让某些工作 Stat 归零。
这解释了为什么断手会大幅影响生产（见 [hediff.md](hediff.md)）。

🔑 **`skillNeedFactors` 是属性与技能的接口**——某些 Stat 要求相关技能
（如手术成功率要求 `Medicine` 技能），`bonusPerLevel` 定义每级技能加多少。

### 1.2 配方 RecipeDef（190 个）

配方定义「**什么东西能做出什么东西**」：

| 字段 | 说明 |
|---|---|
| `researchPrerequisite` | 需要的项目 |
| `workAmount` | 工作量 |
| `ingredients` | 材料（`li` 列表，可有 `filter`） |
| `products` | 产物 |
| `statBases` | 产物的属性 |
| `skillRequirements` | 技能门槛 |
| `fixedIngredientFilters` | 固定材料过滤 |
| `recipeMaker` | 在哪些工作台做（挂 ThingDef 或 BuildingDef） |

🔑 **配方挂在两处**：
- 建造类配方 → 挂在 `ThingDef`（建筑）上
- 制作类配方 → 挂在 `BuildingDef`（工作台）上

```xml
<!-- 挂在工作台 BuildingDef 上 -->
<building>ElectricStove</building>
<workAmount>1800</workAmount>
<ingredients>
  <li>
    <filter><category>FoodRaw</category></filter>
    <count>5</count>
  </li>
</ingredients>
<products>
  <MealSimple</products>
</products>
<skillRequirements><Cooking>4</Cooking></skillRequirements>
```

### 1.3 材料 StuffCategoryDef（6 个）

材料分类——决定一个材料能做什么（做护甲？做工具？当食物？）：

| defName | 中文 | 用途 |
|---|---|---|
| `Metallic` | 金属 | 金属器、装备 |
| `Woody` | 木质 | 木器、家具 |
| `Fabric` | 布料 | 衣物 |
| `Leathery` | 皮革 | 皮革甲 |
| `Stone` | 石头 | 石器 |
| `…` | （共 6 个） | |

🔑 **材料通过 `StuffCategory` 与 `StuffEffectMultiplier*` 影响制作**——
同一套装备，用金属还是布料做，性能不同（见 [gear.md](gear.md) §2.2）。

### 1.4 随机物品生成 ThingSetMakerDef（50 个）

定义「**从一堆物品中随机挑**」的规则——战利品、奖励、袭击物资用。
`ThingSetMakerDef` 定义物品池，`ThingSetMaker` 引用它随机生成。

> 复现：`python tools/game_defs/extract_defs.py --def ThingSetMakerDef`

### 1.5 建造工单 Bill

🔑 **Bill（工单）不在 Def 里——它是存档中的运行期对象**。
玩家在工作台上添加「要做什么」的清单（`Bill`），游戏按 Bill 驱动 `WorkGiver`。

Bill 相关枚举（3 个小 Def）：
- `BillRepeatModeDef`（3）：永远做 / 做够数量 / 做够一次
- `BillStoreModeDef`（3）：存哪里（默认货架/指定区域）

> 这与品质（`CompQuality`）一样是**代码/存档侧结构**——
> 想改 Bill 行为要打补丁或加 `WorkGiver`。

---

## 2. 层级

### 2.1 数值的四层来源

```
Stat 的最终值 = statBases（基准）
  ├─ + statOffsets（加算修正）── Hediff / 特质 / 基因 / 装备
  ├─ × statFactors（乘算修正）── 装备 Stuff / 特质 / 环境
  ├─ × capacityFactors（受身体能力影响）── 受伤降低
  └─ 显示时 toStringStyle 格式化
```

🔑 **加算 vs 乘算是有序的**——先加算后乘算，所以「额外 +2 速度」在
「速度 ×0.5」下净效果是 +1。这解释了为什么高阶装备的加成看似不生效。

### 2.2 Stat 的能力联动（capacityFactors）

```
某个 Stat（如手术成功率）
  └─ capacityFactors
       ├─ Manipulation（手要能动）
       │    └─ 受 Hediff 损伤影响（hediff.md）
       └─ Sight（如需要看）
            └─ 失明 = 0
```

🔑 **受伤影响工作的机制在此**——不是直接改 Stat，而是通过能力乘算。
做「断肢惩罚」类 mod 要改这个，不要直接改 Stat 值。

### 2.3 技能联动（skillNeedFactors）

```
某个 Stat（如建造速度）
  └─ skillNeedFactors
       ├─ skill = Construction
       ├─ baseValue = 0.4（基础保底）
       └─ bonusPerLevel = 0.075（每级技能 +7.5%）
```

🔑 **`baseValue` 保证没技能也有保底产出**，`bonusPerLevel` 是成长空间——
设计 mod 平衡时这两个数是核心旋钮。

### 2.4 配方的层级

```
ThingSetMakerDef（物品池）
  └─ 被 ThingSetMaker 引用
       └─ 用于战利品/奖励/袭击物资

RecipeDef（配方）
  ├─ 挂 BuildingDef（工作台）→ 制作类
  └─ 挂 ThingDef（建筑）────→ 建造类
       └─ Bill（存档对象）── 按玩家工单驱动
            └─ WorkGiver 执行 → Job
```

🔑 **配方 → 工作台 → Bill → WorkGiver → Job**——
改「玩家能做哪些东西」要同时看 RecipeDef 和 Bill 逻辑。

---

## 3. 内容

### 3.1 属性全表（244 个）

规模：**Core 175 · Biotech 27 · Ideology 25 · Anomaly 8 · Odyssey 6 · Royalty 3**。官方中文标签 244/244 全有。

按类别（25 个 category，代表性）：

| 类别 | 代表 Stat | 说明 |
|---|---|---|
| **Basics**（基础） | `MoveSpeed` 移动速度 · `WorkSpeedGlobal` 全局工作速度 · `MentalBreakThreshold` 精神崩溃阈值 | 核心属性 |
| **Apparel**（装备） | `ArmorRating_Sharp` 锐器护甲 · `Insulation_Cold` 保温 · `Mass` 质量 | 装备属性 |
| **Ability**（能力） | `Ability_PsyfocusCost` 灵能消耗 · `Ability_CastingTime` 施法时间 | 能力消耗 |
| **Comfort**（舒适） | `Comfort` 舒适度 | 家具床铺 |
| **MarketValue**（价值） | `MarketValue` 市场价 · `Mass` 质量 | 交易 |
| **Capacity**（能力相关） | 见 §1.1 capacityFactors | 身体能力 |
| **Pawn**（生物） | `SocialImpact` 社交影响 · `LyingDown` 等 | |

> 完整 244 行：`python tools/game_defs/extract_defs.py --def StatDef`
> 字段级（哪些属性受哪些能力/技能影响）：
> `python tools/game_defs/extract_defs.py --def StatDef --fields capacityFactors.li.capacity skillNeedFactors.li.skill`

### 3.2 配方全表（190 个）

按用途分组（代表性）：

| 类别 | 代表 |
|---|---|
| **建造配方** | 墙、门、床、桌椅、各类建筑 |
| **制作配方** | 武器/护甲（挂在锻造台/裁缝台）· 药物（见 [content-blueprints]） |
| **烹饪** | 各道菜、药膳（挂灶台） |
| **药品** | 药品、医疗物资 |
| **研究/锻造** | 高级装备 |
| **加工** | 拆解、锻造零件 |

> 完整 190 行：`python tools/game_defs/extract_defs.py --def RecipeDef`

### 3.3 材料类别全表（6 个）

见 §1.3。

> 复现：`python tools/game_defs/extract_defs.py --def StuffCategoryDef`

---

## 4. 解读

### 4.1 Stat 是 mod 改数值的「通用语言」

想改游戏任何数值，先找它对应的 `StatDef`：
- 移速 → `MoveSpeed`
- 护甲 → `ArmorRating_Sharp`（见 [gear.md](gear.md)）
- 精神崩溃阈值 → `MentalBreakThreshold`
- 工作速度 → `WorkSpeedGlobal`

然后通过三种机制改它：
- `statBases`（ThingDef 基准）
- `statOffsets` / `statFactors`（Hediff/装备/特质的修正）
- 改 `StatDef` 本身（`defaultBaseValue` / `minValue` / `maxValue`）

### 4.2 加算与乘算的顺序陷阱

**先加算后乘算**。举例：
- 一件装备给你 `+0.1` 移动速度（offset）
- 另一件给你 `×0.5` 移动速度（factor）

净效果是 `(基础 + 0.1) × 0.5`。**乘算会「吃掉」加算的收益**——
这是装备平衡和 Hediff 设计的核心陷阱。做 mod 改数值时务必明确用 offset 还是 factor。

### 4.3 身体能力（capacity）如何影响一切

`capacityFactors` 让 Stat 受身体能力影响。链条：
`Hediff 损伤` → `PawnCapacity 降低`（如 `Manipulation`）→ `Stat × 某系数` → 工作/战斗变慢

所以「受伤」不是简单扣 HP，而是**通过能力乘算全面削弱**。
断手 → `Manipulation` 归零 → 所有精细工作 Stat 归零（见 [hediff.md](hediff.md)）。

### 4.4 技能成长空间（skillNeedFactors）

`baseValue` + `bonusPerLevel` 决定「技能的价值曲线」：
- `baseValue` 低 + `bonusPerLevel` 高 → 技能差距被放大（专家值钱）
- `baseValue` 高 + `bonusPerLevel` 低 → 技能用处小（新手也能干）

做「技能重要 vs 无所谓」的 mod，调这两个数。

### 4.5 配方的挂载位置决定「在哪做」

- 建造配方挂 `ThingDef`（目标建筑）→ 在蓝图附近干活
- 制作配方挂 `BuildingDef`（工作台）→ 在工作台干活

⚠️ 搞错挂载位置会导致「配方做了但找不到工作台/蓝图」。

### 4.6 Bill 是存档对象，不是 Def

工单（Bill）存在存档里，玩家在 UI 上添加。它是 `WorkGiver` 的驱动力——
`WorkGiver` 扫描「哪些工作台有空 Bill」→ 生成 Job。

想改 Bill 行为（如「自动补货」）要么打补丁，要么写新 `WorkGiver`。

---

## 5. modder 常见坑

1. **Stat 加算在乘算之前**——`offset` 会被 `factor` 吃掉。改数值前先想清楚用哪个。
2. **`capacityFactors` 才是「受伤影响工作」的机制**——直接改 Stat 值不会受断手影响，
   要走能力乘算。
3. **配方挂载位置不能错**——建造挂 `ThingDef`，制作挂 `BuildingDef`。
4. **Bill 不是 Def**，改不了 XML；Bill 逻辑靠 `WorkGiver` 或补丁。
5. **`skillNeedFactors` 决定技能重要性**——`bonusPerLevel` 是「专精价值」的旋钮。
6. **`minValue`/`maxValue` 会硬截断**——给装备设大加成时，Stat 的 `maxValue`
   可能让它封顶，达不到预期效果。
7. **`hideAtValue`/`showIfUndefined` 影响 UI 显示**——新加 Stat 若不设这些会到处显。
8. **`cacheable` 与性能相关**——缓存 Stat 的改动要配失效（见
   [../performance-and-gui.md](../performance-and-gui.md) §5）。
