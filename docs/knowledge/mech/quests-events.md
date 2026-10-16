# 任务与事件（Quest / Incident / History / Tale）

> **mech 层**：本文只讲**任务-事件系统由什么构成、字段含义、内容清单**。
> 「历史事件怎么触发想法/戒律」联动 [needs-mood.md](needs-mood.md) 和 [ideology.md](ideology.md)；
> 「任务节点怎么执行」属实操，在 [../ref/csharp-feature-recipes.md](../ref/csharp-feature-recipes.md) 与 patch 类 mod。
>
> **数据源与核对日期（2026-10-16）**：
> 本地 `F:\Games\GBL\rimworld\Data\`（RimWorld 1.6，全 6 DLC），中文取官方简中 DefInjected。
> 复现：`python tools/game_defs/extract_defs.py --def QuestScriptDef IncidentDef HistoryEventDef TaleDef LetterDef IncidentCategoryDef`。
>
> 规模：**QuestScriptDef 151 · IncidentDef 131 · HistoryEventDef 172 · TaleDef 101 ·
> LetterDef 26 · IncidentCategoryDef 13**。

---

## 1. 框架

### 1.1 五个系统的分工

```
IncidentDef（131）── 随机事件：「今天发生了什么」
  └─ 可以 spawn QuestScriptDef（18% 带 questScriptDef）
       └─ 或给玩家发信（LetterDef）→ 或触发 HistoryEventDef

QuestScriptDef（151）── 任务：节点树驱动的多步挑战
  └─ root ─▶ QuestNode 树 ─▶ 每步可完成/失败
       └─ 完成后给奖励

HistoryEventDef（172）── 历史事件记录：「谁做了什么」
  └─ 触发 ThoughtDef（心情）/ PreceptDef（戒律）/ 记录 Tale
       └─ 是想法与戒律的触发源（needs-mood.md / ideology.md）

TaleDef（101）── 故事：可被回忆的往事
  └─ 由事件生成，日后影响心情（如「某人的死」）

LetterDef（26）── 信件模板（Letter）
  └─ 事件的对外公告载体
```

🔑 **四个系统的关系**：事件（Incident）是**随机触发**，
任务（Quest）是**多步挑战**，历史（History）是**记录触发源**，故事（Tale）是**记忆载体**。
事件可以派生出任务，历史可以派生出想法/故事。

### 1.2 事件 IncidentDef（131 个）

**事件的触发与控制字段**（131 实测）：

| 字段 | 出现率 | 说明 |
|---|---:|---|
| `workerClass` | 82% | C# 类：`IncidentWorker_Ambush_EnemyFaction` 等，真正执行事件逻辑 |
| `baseChance` | 75% | 基础触发权重（0.4 / 1.0 / 1.5） |
| `category` | 73% | 分类（`ThreatBig` 大威胁 · `ThreatSmall` 小威胁 · `Misc` 等，10 类） |
| `targetTags` | 73% | 触发目标（`Caravan` 商队 · `Map_PlayerHome` 玩家家 · `World` 世界…） |
| `minRefireDays` | 37% | 再触发间隔（0.17 天 / 13 天…） |
| `earliestDay` | 19% | 最早第几天可能触发 |
| `minThreatPoints` | 12% | 需要威胁点数（400 / 300 / 700） |
| `pointsScaleable` | 19% | 点数是否随强度缩放 |
| `durationDays` | 13% | 持续时间（`1.5~3.0` 等区间） |
| `questScriptDef` | 18% | **触发此事件会派生一个任务** |
| `tale` | 18% | 触发时生成的故事 |
| `letterDef` / `letterText` | 35%/28% | 发信（事件公告） |
| `diseaseIncident` | 10% | 疾病事件（`Flu` 流感等） |
| `mtbDaysByBiome.<Biome>` | 3% | **按生物群系调整触发频率** |
| `minAnomalyThreatLevel` | 20% | Anomaly 威胁等级门槛 |

🔑 **`workerClass` 是事件的核心**——`IncidentDef` 只是数据，
真正「发生什么」由 C# 的 `IncidentWorker_*` 决定。改事件逻辑要打补丁或改 worker。

🔑 **`mtbDaysByBiome` 让事件随地点变化**——同一种事件在热带/寒带频率不同。

⚠️ **51/131 有 `ParentName`**（`DiseaseIncident` `GiveQuestBase` 等）——
疾病类事件大量继承公共基类，改基类影响全谱系。

### 1.3 事件分类 IncidentCategoryDef（13 个）

事件的归类：`ThreatBig` 大威胁 · `ThreatSmall` 小威胁 · `Misc` 杂项 ·
`Disease` 疾病 · `Quest` 任务 · `GiveQuest` 给予任务 · `ConditionGame` 游戏状态 等。

### 1.4 任务 QuestScriptDef（151 个）

🔑 **任务是「节点树」驱动的多步挑战**——结构与其他系统完全不同：

| 字段 | 出现率 | 说明 |
|---|---:|---|
| `questNameRules` | 67% | 任务名生成规则（`questName->The [bandit] [camp]`） |
| `questDescriptionRules` | 78% | 任务描述生成规则（大量 `rulesStrings`） |
| `root` | 28% | 根节点（`QuestNode_Root_*` C# 类），整棵树的入口 |
| `root.nodes` | 28% | 根节点的子节点 |
| `rootSelectionWeight` | 41% | 任务被选中的权重 |
| `rootMinPoints` | 25% | 触发所需任务点 |
| `autoAccept` | 35% | 自动接受（不询问玩家） |
| `isRootSpecial` | 29% | 特殊根节点 |

🔑 **任务名/描述是「规则字符串」拼出来的**——`questNameRules` 里
`questName->[bandit] [camp]` 意思是「把 bandit 换成实际敌人、camp 换成据点类型」。
所以**任务名不是硬编码的**，是根据实际内容动态生成（大量同义词如
`bandit->Raider`/`Outlaw`/`Outlander`）。

🔑 **`root` 是 `QuestNode` 树**——每个节点是一个「任务步骤」
（`QuestNode_Root_DelayedRewardDropPods` 等 C# 类），节点间跳转构成任务流程。
151 个任务里 **Royalty 61 个**——帝国任务是 DLC 内容主体。

⚠️ **QuestScriptDef 顶层中文标签为 0**（151 个全空）——
任务名全靠 `questNameRules` 动态生成。

### 1.5 历史事件 HistoryEventDef（172 个）

🔑 **极简——只有 `defName` + `label`**——它是纯「事件记录标识」，
不定义逻辑，只作为**触发源**存在。

**这是全知识库最重要的联动点**：

```
某事发生了（HistoryEventDef，如 GaveGift / Traded / AteHumanMeatDirect）
  └─ 触发 ThoughtDef（心情，见 needs-mood.md）
       └─ 或匹配 PreceptDef 的 comps（戒律奖惩，见 ideology.md）
            └─ 记录 TaleDef（故事，日后被回忆）
```

🔑 **Ideology 的 59 个 HistoryEvent 是文化反馈的核心**——
「吃了人肉」等事件触发戒律判定 → 产生心情增减（见 [ideology.md](ideology.md) §4）。

规模：**Core 95 · Ideology 59 · Biotech 8 · Anomaly 6 · Odyssey 4**。

### 1.6 故事 TaleDef（101 个）

「**可被回忆的往事**」——比 HistoryEvent 更进一步，是一段有内容的记忆。

- 由事件生成（IncidentDef.tale 或事后记录）
- **日后影响心情**——被回忆的往事触发 `ThoughtDef`
- 是「故事叙述」系统（Storyteller）的产物之一（见下方 DLC 篇）

### 1.7 信件 LetterDef（26 个）

信件模板——事件的对外公告。26 种定义：威胁、疾病、任务、正面事件、
赎金要求等。`IncidentDef.letterDef` 引用它。

---

## 2. 层级

### 2.1 事件的触发与派生

```
Storyteller（叙事者）按难度和权重决定
  └─ 选一个 IncidentDef（按 baseChance/MTB/时间/群系）
       ├─ workerClass 执行事件逻辑
       │    ├─ 可能 spawn 敌人/物品/疾病
       │    ├─ 可能派生 Quest（questScriptDef）
       │    ├─ 可能发信（LetterDef）
       │    └─ 可能记录 HistoryEvent + Tale
       └─ 满足 minRefireDays 后才能再次触发
```

🔑 **事件是随机的，任务是事件或独立的**——
任务可以由事件派生（18%），也可以独立触发（如 WandererJoins 加入者）。

### 2.2 任务的节点树

```
QuestScriptDef
  └─ root（QuestNode_Root_*）
       ├─ 子节点1 ─▶ 条件满足才激活
       ├─ 子节点2 ─▶ 与节点1 并行/串行
       └─ 叶子节点 ─▶ 奖励/结算
```

任务是一棵**条件树**——节点按条件激活，全部完成则任务成功。

### 2.3 历史 → 想法 → 心情的链

```
事件（HistoryEventDef，如 AteHumanMeatDirect）
  └─ 触发 ThoughtDef（如 AteHumanlikeMeatDirect 吃了人肉）
       ├─ → 心情增减（needs-mood.md）
       ├─ → 匹配 Ideology Precept 戒律奖惩（ideology.md）
       └─ → 记录 Tale（日后回忆再产生影响）
```

🔑 **这是「游戏怎么把行为变成情绪」的完整机制**——
做一个「你的行为影响心情」的 mod，就是在这条链上加 HistoryEvent / Thought。

### 2.4 事件的难度与强度

```
IncidentDef
  ├─ baseChance（基础权重）
  ├─ minThreatPoints（威胁点数门槛）
  ├─ pointsScaleable（随殖民地规模缩放）
  └─ mtbDaysByBiome（按群系调频）
       └─ Storyteller 按难度加权选择
```

🔑 **`pointsScaleable` 让事件强度随殖民地成长**——
做「后期更有挑战」的 mod 就调这个。

---

## 3. 内容

### 3.1 事件全表（131 个）

规模：**Core 57 · Anomaly 40 · Odyssey 12 · Royalty 10 · Ideology 7 · Biotech 5**。官方中文标签 131/131 全有。

按类别分组（代表性）：

| 类别 | 代表事件 |
|---|---|
| **袭击/伏击** | `Ambush` 伏击 · `ManhunterAmbush` 兽群伏击 · `Infestation` 虫巢 |
| **疾病** | `DiseaseFlu` 流感 · `DiseasePlague` 瘟疫（13 个 diseaseIncident） |
| ** caravan/商队** | `CaravanMeeting` 相遇 · `CaravanAmbush` 商队遇袭 |
| **任务触发** | `WandererJoins` 加入者 · `RefugeePodCrash` 难民舱 · `GiveQuest*` |
| **游戏状态** | `ToxicFallout` 毒落 · `VolcanicWinter` 火山寒冬（`gameCondition`） |

> 完整 131 行：`python tools/game_defs/extract_defs.py --def IncidentDef`

### 3.2 任务全表（151 个）

规模：**Royalty 61 · Core 23 · Ideology 14 · Anomaly 12 · Biotech 10 · Odyssey 31**。
⚠️ 顶层中文标签为 0（靠 `questNameRules` 动态生成）。

按类型分组（代表性）：

| 类型 | 代表任务 |
|---|---|
| **营救** | `OpportunitySite_DownedRefugee` 救援难民 |
| **清剿** | `OpportunitySite_BanditCamp` 清剿匪巢 |
| **物资** | `DelayedRewardDropPods` 延迟奖励舱 |
| **帝国（Royalty）** | 大量帝国委托（61 个） |
| **文化（Ideology）** | 14 个文化相关任务 |

> 完整 151 行：`python tools/game_defs/extract_defs.py --def QuestScriptDef`

### 3.3 历史事件全表（172 个）

Core 95 · Ideology 59 · Biotech 8 · Anomaly 6 · Odyssey 4。
只有 `defName` + `label`。它作为想法/戒律的触发源，被广泛引用
（见 [needs-mood.md](needs-mood.md) §1.3、`ideology.md` §1.3）。

> 完整 172 行：`python tools/game_defs/extract_defs.py --def HistoryEventDef`

### 3.4 故事全表（101 个）

故事的种类（旧事、死亡、袭击、感情等）。可被回忆，影响心情。

> 复现：`python tools/game_defs/extract_defs.py --def TaleDef`

---

## 4. 解读

### 4.1 事件是「随机的玩家体验」的引擎

RimWorld 的独特之处：**不知道什么时候会发生什么**。
`IncidentDef` 定义了「有哪些可能发生的事」，
`baseChance`/`minRefireDays`/`mtbDaysByBiome` 定义「多可能发生、何时发生」。
事件由 Storyteller 按难度加权选择。

改事件频率/强度是 mod 最常见的玩法改动——调 `baseChance` 即可。

### 4.2 任务名是动态生成的

一个任务模板（如「营救」类）能生成无数变体的任务名——
因为 `questNameRules` 里 `bandit->Raider`/`Outlaw`/`Outlander` 是同义词，
实际生成时替换成当前敌人类型。

这意味着**一个 `QuestScriptDef` 可以撑起一整个任务家族**——
所以 151 个任务 Def 实际对应远多于 151 种任务体验。

### 4.3 历史事件是多系统的公共触发源

🔑 **HistoryEventDef 是全游戏最底层的「事件总线」**——
它把「游戏里发生的事」分发给各个响应系统：

```
HistoryEventDef（发生了什么）
  ├─▶ ThoughtDef（→ 心情，needs-mood.md）
  ├─▶ PreceptDef（→ 文化奖惩，ideology.md）
  └─▶ TaleDef（→ 故事记忆，日后影响）
```

做「事件驱动」的 mod——**加一个 HistoryEvent + 对应 Thought，
就能让某件事影响心情/文化**。这是最干净的扩展点之一。

### 4.4 故事的「延迟影响」

`TaleDef` 记录往事，日后被回忆时再次影响心情。
所以伤害有「二次效应」——当下扣心情（事件）+
日后回忆再扣（故事）。做「创伤」类 mod 可利用这点。

### 4.5 Royalty 任务是 DLC 内容主体

151 个任务里 Royalty 占 61 个——帝国任务是 Royalty DLC 的核心玩法。
它们依赖头衔、灵能、外交系统（见后续 DLC 篇）。

---

## 5. modder 常见坑

1. **事件逻辑在 `workerClass`（C#）不在 `IncidentDef`**——
   改 `IncidentDef` 只能改频率/条件，改「发生什么」要打补丁。
2. **`IncidentDef` 51/131 有 `ParentName`**——疾病类大量继承基类，
   改 `DiseaseIncident` 影响所有疾病事件。
3. **任务名靠 `questNameRules` 动态生成**——找不到任务中文标签是正常的，
   名字在规则里，别去 DefInjected 找。
4. **`QuestNode` 是节点树**——任务流程是条件驱动的，改任务要懂节点跳转逻辑
   （属实操，在 patch 类 mod 或 QuestNode 类里）。
5. **`HistoryEventDef` 只有 label**——它是触发源，加新历史事件只需
   `defName`+`label`，再让 Thought/Precept 引用它。
6. **`minRefireDays`/`earliestDay`/`mtbDaysByBiome` 控制频率**——
   调事件太频繁/太稀有改这些，别只改 `baseChance`。
7. **Royalty 任务依赖 DLC**——引用帝国任务要处理 Royalty 未加载的情况。
8. **想做「事件影响心情」——走 HistoryEvent + Thought 这条路**
   （见 [needs-mood.md](needs-mood.md)），不要直接改事件 worker。
