# 工作与 Job（Work / Job / ThinkTree / 排班）

> **mech 层**：本文只讲**工作-Job 系统由什么构成、字段含义、内容清单**。
> 「怎么写一个 JobDriver + Toil」见 [../ref/csharp-feature-recipes.md](../ref/csharp-feature-recipes.md)；
> 「排班/分配的运行时逻辑」见 [../game-and-world-components.md](../game-and-world-components.md)；
> 「技能如何影响工作成功率」见 [skills-traits.md](skills-traits.md)。
>
> **数据源与核对日期（2026-10-16）**：
> 本地 `F:\Games\GBL\rimworld\Data\`（RimWorld 1.6，全 6 DLC），中文取官方简中 DefInjected。
> 复现：`python tools/game_defs/extract_defs.py --def JobDef WorkGiverDef WorkTypeDef ThinkTreeDef TimeAssignmentDef`。
>
> 规模：**JobDef 320 · WorkGiverDef 153 · WorkTypeDef 23 · ThinkTreeDef 61 · TimeAssignmentDef 5**。

---

## 1. 框架

### 1.1 五个 Def 的关系

```
ThinkTreeDef（61）── AI 决策树：pawn 在空闲时想什么
  └─ ThinkNode ──▶ 调用 WorkGiverDef

WorkGiverDef（153）── 工作执行器：扫描目标 → 生成 Job
  └─ workType ──▶ WorkTypeDef（23 种工作类型）
  └─ requiredCapacities（需要什么身体能力）

JobDef（320）── Job 定义（被 JobDriver 实例化执行）
  └─ driverClass ── JobDriver_C（真正干活的 C# 类）

TimeAssignmentDef（5）── 排班：一天分「工作/休息/娱乐/冥想/任意」
  └─ 决定 pawn 什么时候允许工作

Pawn.WorkPriorities ── 每个工作类型的优先级（玩家设）
```

🔑 **决策链**：ThinkTree 决定「该做什么」→ WorkGiver 扫目标 → 生成 Job →
JobDriver 用 Toil 一步步执行。排班决定「什么时候能做」。

### 1.2 工作类型 WorkTypeDef（23 种）

玩家在「工作」界面看到的**大类**：

| defName | 中文 | defName | 中文 |
|---|---|---|---|
| `BasicWorker` | 基本 | `Handling` | 驯兽 |
| `Hauling` | 搬运 | `Hunting` | 狩猎 |
| `Cleaning` | 清洁 | `Construction` | 建造 |
| `Mining` | 采矿 | `PlantCutting` | 割除 |
| `Growing` | 种植 | `Cooking` | 烹饪 |
| `Smithing` | 锻造 | `Tailoring` | 缝制 |
| `Crafting` | 制作 | `Art` | 艺术 |
| `Research` | 研究 | `Doctor` | 医生 |
| `Patient` | 就医 | `PatientBedRest` | 休养 |
| `Warden` | 监管 | `Firefighter` | 灭火 |
| `Childcare` | 育儿（Biotech） | `DarkStudy` | 暗研（Anomaly） |
| `Fishing` | 钓鱼（Odyssey） | | |

🔑 **`Patient`（就医）是个特殊工作类型**——它是「去被治疗」而非「治疗别人」，
由医生（Doctor）执行。类似地 `PatientBedRest`（休养）。这种「被动工作」设计很常见。

> 新增一个 mod 工作类型的完整流程：新建 `WorkTypeDef` → 建对应 `WorkGiverDef`
> → 在 `ThinkTree` 里挂 `WorkGiver` → 写 `JobDriver`。

### 1.3 工作执行器 WorkGiverDef（153 个）

WorkGiver 是**扫描目标并生成 Job** 的中间层，字段极清晰：

| 字段 | 出现率 | 说明 |
|---|---:|---|
| `giverClass` | 99% | C# 类：`WorkGiver_FightFires` 等，负责扫描逻辑 |
| `workType` | 94% | 属于哪个工作类型（对应 WorkTypeDef） |
| `priorityInType` | 94% | 同一工作类型内的优先级（数字小的先做） |
| `requiredCapacities` | 88% | 需要哪些身体能力（`Manipulation` / `Talking` / `Moving`） |
| `verb` / `gerund` | 95% | 动作动词（`extinguish` 灭火 / `tend to` 治疗） |
| `canBeDoneByMechs` | 21% | 机械体能否做 |
| `scanThings` / `scanCells` | 8% | 扫描目标（物件/格子）还是两者 |
| `fixedBillGiverDefs` | 13% | 限定在哪些工作台做（如 `ElectricStove`） |
| `emergency` | 3% | 紧急工作（灭火/急救），可打断其他 |
| `equivalenceGroup` | 5% | 互斥组（同组不重复排队） |
| `canBeDoneWhileDrafted` | 3% | 被征召时能否做（如灭火） |
| `tagToGive` | 5% | 给 job 打标签（如 `RestingForMedicalReasons`） |

🔑 **`priorityInType` 决定同一工作类型内的执行顺序**——如医生先 `emergency`
急救再 `tend to` 普通治疗。改工作优先级常改这里。

### 1.4 Job 定义 JobDef（320 个）

Job 是**实际执行的原子动作**：

| 字段 | 出现率 | 说明 |
|---|---:|---|
| `driverClass` | 99% | `JobDriver_C`——真正的执行类 |
| `reportString` | 94% | 执行中的显示文本（`nuzzling TargetA.`） |
| `allowOpportunisticPrefix` | 52% | 允许「顺便」（Opportunistic）前缀 |
| `casualInterruptible` | 23% | 能否被随便打断（如娱乐活动） |
| `joyKind` / `joyDuration` | 7% | 这是娱乐活动（什么娱乐 / 持续多久） |
| `joySkill` / `joyXpPerTick` | 2% | 娱乐还能练技能（如射击游戏） |
| `carryThingAfterJob` / `dropThingBeforeJob` | 8%/7% | 完成后拿着/开始前放下东西 |
| `suspendable` | 8% | 可否被挂起（如建造到一半被征召） |
| `neverShowWeapon` / `alwaysShowWeapon` | 8%/4% | 执行时是否显示武器 |
| `taleOnCompletion` | 5% | 完成后生成故事（`BuiltSnowman` 堆雪人等） |
| `makeTargetPrisoner` | 5% | 把目标变囚犯（如绑架） |
| `isIdle` | 2% | 这是「空闲」Job |

🔑 **Job 是 WorkGiver 的产物**——WorkGiver 扫描后 `TryCreateJobFor`，
返回一个 Job 实例，`JobDriver` 接手执行。**WorkGiver 决定「做什么」，JobDef 决定「怎么做」**。

⚠️ **JobDef 的顶层中文标签多为 0**（320 个里几乎不显示 label）——
因为 Job 显示靠 `reportString`（运行时动态文本），不需要静态 label。

### 1.5 决策树 ThinkTreeDef（61 个）

ThinkTree 是**pawn 空闲时的决策树**——决定它想干什么、想干哪类活。
每个 ThinkTree 含多个 `ThinkNode`，节点间按优先级跳转，最终调用 `WorkGiver`。

- 原版有专门的 `ThinkTree`（如 `Humanlike`、`Mech`、`Animal`）
- `ThinkNode` 决定：唤醒条件、跑哪个 WorkGiver、失败跳哪
- 这是 mod 做「AI 行为定制」的主要入口（如改派系 AI 优先级）

> 「ThinkTree 怎么读/怎么改」属实操，在 [../game-and-world-components.md](../game-and-world-components.md) 和
> 仓库 patch 类 mod 里；本层只说明它在决策链中的位置。

### 1.6 排班 TimeAssignmentDef（5 种）

一天分时段，每个时段决定「允许做什么」：

| defName | 中文 | 说明 |
|---|---|---|
| `Work` | 工作 | 正常工作时段 |
| `Sleep` | 休息 | 睡觉（可设 `Anything` 之外强制睡） |
| `Joy` | 娱乐 | 娱乐活动时段 |
| `Meditate` | 冥想 | 冥想时段（Royalty，可配合灵能） |
| `Anything` | 任意 | 任何时段都做（如吃饭、急救） |

🔑 **`Anything` 是最宽松的**——急救、吃饭这类无论排班都要做的工作挂在 `Anything`。

---

## 2. 层级

### 2.1 完整决策链

```
[排班 TimeAssignment] 当前时段允许做什么？
  └─ [ThinkTree] 空闲时想什么（决策树）
       └─ [WorkGiver] 扫描目标（地图/物件/台子）→ 掷骰优先级
            └─ [JobDef + JobDriver] 生成并执行 Job
                 └─ Toil 循环：走向目标 → 操作 → 完成
                      └─ 成功 → 技能经验 + 产出 + 满足需求
```

🔑 **四层各管一段**：排班管「何时」，ThinkTree 管「想干什么」，
WorkGiver 管「去哪做」，JobDriver 管「怎么做」。改任何一环都要考虑上游。

### 2.2 工作优先级的三层

```
玩家设置 WorkPriorities（每个 WorkTypeDef 一个优先级）
  └─ WorkGiver 的 priorityInType（同类型内排序）
       └─ Job 的实际执行（JobDriver）
```

- **工作优先级**由玩家设（0 = 禁用，越小越优先）
- **`priorityInType`** 由游戏决定（同一工作内先做哪个）
- 紧急工作（`emergency`）可**打断**非紧急工作

### 2.3 能力门槛的三层

```
requiredCapacities（WorkGiver 要求，如 Manipulation）
  └─ 身体能力（来自 Hediff 损伤，见 hediff.md）
       └─ 技能等级（skill 掷骰，skills-traits.md）
```

🔑 一个 pawn 做某活的成功：先看**身体能力够不够**（有没有受伤），
再看**技能等级**，再叠加心情/特质修正。断手就做不了精细活（即使技能满级）。

### 2.4 排班与需求的联动

```
Rest 需求低（累）
  └─ 排班进入 Sleep 时段
       └─ ThinkTree 生成睡覺 Job
            └─ 满足 Rest 需求（needs-mood.md）
```

排班和需求互相咬合——排班决定何时满足需求，需求驱动是否去满足。

---

## 3. 内容

### 3.1 工作类型全表（23 种）

见 §1.2 完整表。官方中文标签大部分有（`DarkStudy`/`Childcare`/`Fishing` 暂缺）。

> 复现：`python tools/game_defs/extract_defs.py --def WorkTypeDef`

### 3.2 WorkGiver 全表（153 个）

规模：**Core 105 · Biotech 26 · Anomaly 12 · Ideology 9 · Odyssey 1**。官方中文标签 153/153 全有。

> 复现：`python tools/game_defs/extract_defs.py --def WorkGiverDef`

按工作类型分组（代表性）：

| 工作 | WorkGiver 例子 |
|---|---|
| 灭火 | `FightFires` |
| 医疗 | `PatientGoToBedEmergencyTreatment` / `PatientGoToBedTreatment` / `TendPatient` |
| 搬运 | `HaulToInventory` / `HaulToCell` |
| 建造 | `Build` / `FinishConstruction` / `DeliverToConstruction` |
| 种植 | `SowPlanted` / `Harvest` / `CutPlants` |
| 做饭 | `CookMeal` |
| 研究 | `Research` |
| 清理 | `Cleaning`（拆除标记） |
| 监管 | `PrisonerWillingToWork` 等 |

### 3.3 Job 全表（320 个）

规模：**Core 195 · Biotech 42 · Ideology 36 · Anomaly 29 · Odyssey 15 · Royalty 3**。

> 复现：`python tools/game_defs/extract_defs.py --def JobDef`

按性质（代表性）：

| 类型 | 例子 |
|---|---|
| **娱乐活动** | `Nuzzle`（亲昵）· `Mate`（交配）· `SocialRelaxation` |
| **生物行为** | `LayEgg`（产卵）· `LayMineableEgg` |
| **建造/生产** | 大量 Toil 对应的 Job |
| **Ideology 仪式** | 36 个 Ideology Job（祷告、仪式等） |
| **特殊** | 绑架（`makeTargetPrisoner`）· 堆雪人（`taleOnCompletion`） |

### 3.4 决策树全表（61 个）

涵盖人形生物、动物、机械体、思维体（Anomaly）等各类 pawn 的决策树。

> 复现：`python tools/game_defs/extract_defs.py --def ThinkTreeDef`

### 3.5 排班全表（5 种）

见 §1.6。

---

## 4. 解读

### 4.1 Job 是 mod 最常用的扩展点

想在游戏里加一个「自定义活动」，标准做法：

```
WorkTypeDef（可选，工作类型）
  └─ WorkGiverDef（扫描 + 生成 Job）
       └─ JobDef（JobDriver_C 定义）
            └─ Toil（实际逻辑，见 csharp-feature-recipes.md）
```

Job 是**最小执行单元**——比 Toil 小，比 ThinkNode 大。
一个活动 = 一个 Job。`JobDef.driverClass` 指向你的 `JobDriver`。

### 4.2 「顺便」（Opportunistic）机制

167/320 的 Job 有 `allowOpportunisticPrefix=true`——
意思是「路过时顺便做一下」。如搬运路上顺手灭火。
这是 RimWorld 让 pawn 行为显得自然的关键机制。

### 4.3 紧急工作与打断

`emergency=true` 的 WorkGiver（如灭火、急救）**可打断非紧急工作**。
这是 RimWorld 里「着火时所有人丢下工作去灭火」的实现。

同理，Job 的 `casualInterruptible`（可随便打断）控制娱乐/工作间的打断关系。

### 4.4 娱乐 → 技能（Joy 系统）

部分 Job 带 `joySkill` + `joyXpPerTick`——**娱乐活动也能练技能**。
如玩射击游戏练射击（`joyXpPerTick 0.003`）。
`joyKind` 决定这是什么娱乐（`Social` 社交/`Meditative` 冥想等），
`joyMaxParticipants` 限制人数（三人麻将 vs 两人）。

这与 `Joy` 需求（见 [needs-mood.md](needs-mood.md)）联动——娱乐满足 Joy 需求。

### 4.5 派系工作差异

不同派系的 `ThinkTree` 不同——玩家派系会工作/建造/种植，
敌人派系会战斗/袭击，囚犯按 `Warden` 工作。
做派系 AI mod 要改对应的 `ThinkTreeDef`，而不是改 Job。

### 4.6 Ideaology 的「监视者」工作

`Warden`（监管）工作受 Ideology 影响——某些戒律（如
`Slavery_Acceptable`）会改变监管者对囚犯/奴隶的态度。
见 [ideology.md](ideology.md)（`WorkerClass` 相关）。

---

## 5. modder 常见坑

1. **Job 顶层 label 多为空**（320 个几乎没静态 label）——
   Job 显示靠 `reportString`（运行时动态文本）。写新 Job 必写 `reportString`。
2. **`priorityInType` vs 玩家优先级是两回事**——前者游戏定（同类内排序），
   后者玩家定。别用 `priorityInType` 去改玩家能感知的工作优先级。
3. **`Anything` 时段的工作最不受排班限制**——急救/吃饭类要挂 `Anything`，
   否则被排到 Sleep 时段就做不了。
4. **`Patient`/`PatientBedRest` 是「被动工作」**（被治疗/休养），
   语义上由别人（医生）驱动，别当普通主动工作理解。
5. **改 AI 行为优先改 `ThinkTreeDef`**，不是改 Job——Job 只是执行单元。
6. **`requiredCapacities` 是硬门槛**——`Manipulation` 不够直接不做，
   不看技能等级。加新 WorkGiver 要想清楚能力要求。
7. **机械体能力靠 `canBeDoneByMechs`**（21% 有）——多数工作机械体做不了，
   想让机械体做新活得显式开这个。
8. **`fixedBillGiverDefs` 限定工作台**——新增 WorkGiver 若不想指定台就别加这个字段，
   加了就只能在那些台做。
