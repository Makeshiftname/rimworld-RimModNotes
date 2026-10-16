# 帝国与灵能（Royalty DLC）

> **mech 层**：本文讲 **Royalty DLC 独有的系统**——头衔/荣誉、灵能（ Psycast）、
> 帝国蓝图与外交。**复用 Core 的部分**（ThingDef / HediffDef / AbilityDef /
> FactionDef 等）指回其他文档，不重复。
>
> **数据源与核对日期（2026-10-16）**：
> 本地 `F:\Games\GBL\rimworld\Data\Royalty\`，RimWorld 1.6。中文取官方简中 DefInjected。
> 复现：`python tools/game_defs/extract_defs.py --def RoyalTitleDef RoyalTitlePermitDef MeditationFocusDef WorldObjectDef --dlc Royalty`
>
> 规模：RoyalTitleDef 11 · RoyalTitlePermitDef 15 · MeditationFocusDef 7 ·
> WorldObjectDef 33 · **Royalty 能力 30**。
> ⚠️ 大量内容依赖其他 DLC（蓝图来自 Biotech/Odyssey）。

---

## 1. 框架

### 1.1 四大子系统

```
帝国系统（Royalty 核心玩法）
  ├─ RoyalTitleDef（11 个头衔）── 玩家派系的头衔体系
  │    └─ 头衔带来：期望（Expectations）、心情特权、帝国委托
  ├─ 帝国委托（QuestScriptDef，61 个）── 头衔附带任务
  │    └─ 完成得 Favor（帝国好感）── 可换蓝图/头衔
  └─ 蓝图 Techprint ── 蓝图解锁 Ultimate/Spacer 技术（Blueprint 解锁链）

灵能系统（Pg. Anima）
  ├─ Psylink（灵能链接等级）── Stat（≤ P0 可控）
  │    └─ 决定能放多强的灵能
  ├─ AbilityDef（30 个灵能）── Psycast
  │    ├─ 消耗 Psyfocus（专注）
  │    └─ 产生 Entropy（熵）── 熵满 → 昏迷
  ├─ MeditationFocusDef（7 个冥想方向）── 恢复专注的方式
  └─ 灵能受伤/增益 Hediff（见 hediff.md）
```

### 1.2 头衔 RoyalTitleDef（11 个）

**玩家派系的头衔**——每个头衔给玩家派系一个 buff（心情期望），
同时决定能接哪些帝国委托、要求多高认可度：

| defName | 中文 | 级别 |
|---|---|---|
| `Freeholder` | 自由民 | 低 |
| `Acolyte` | 扈从 | 低 |
| `Knight` | 骑士 | 中 |
| `Yeoman` | 修士 | 中 |
| `Baron` | 领主 | 高 |
| `Count` | 总督 | 高 |
| `Praetor` | 法务官 | 最高 |
| `Stellarch` | 星系主宰 | 最高 |
| `Consul` | 执政官 | NPC 派系 |
| `Duke` | 专制公 | NPC 派系 |
| `Emperor` | 星海帝皇 | NPC 派系 |

🔑 **头衔的继承链**（本底稿实测）：
- `BaseEmpireTitle` —— 玩家派系头衔基类（Freeholder/Baron/Knight/Count…）
- `BaseEmpireTitleNPC` —— NPC 帝国头衔基类（Stellarch/Duke/Emperor…）

🔑 **头衔附带的期望（Expectations）**——升头衔会提升期望门槛，
需要对成员更好（如「帝国头衔要求殖民地观感达到 X」），
否则成员不满（Expectation 相关见 [needs-mood.md](needs-mood.md) 的解读部分）。

### 1.3 头衔许可 RoyalTitlePermitDef（15 个）

🔑 **头衔是「许可制」的**——不是升到某级别自动获得，
而是要通过**蓝图的许可（Permit）系统**解锁。

机制（来自本地 `buildRoyaltyPermitDefs`）：
- 玩**帝国蓝图**能赚取**帝国认可度（Favor）**——完成委托、找到神器、清理怪兽
- 用 Favor 从帝国换取**新的头衔许可**（解锁某个头衔）
- 每个头衔对殖民者能力有要求（如 Stellarch 要求 Insp. 5+ 人格魅力）

> 复现：`python tools/game_defs/extract_defs.py --def RoyalTitlePermitDef`
> 官方详细机制见 [灰机 wiki 帝国](https://rimworld.huijiwiki.com/wiki/%E5%B8%9D%E5%9B%BD)。

### 1.4 灵能能力 AbilityDef（30 个灵能）

灵能是一类特殊 `AbilityDef`，核心字段：

| 字段 | 出现率 | 说明 |
|---|---:|---|
| `ParentName` | 100% | 继承：`PsycastBase` / `PsycastBaseSkip` / `SpeechBase` |
| `level` | 97% | 灵能等级（1–5），决定 Psylink 门槛 |
| `statBases.Ability_PsyfocusCost` | 93% | **专注消耗** |
| `statBases.Ability_EntropyGain` | 70% | **熵产生**（持续/瞬时） |
| `statBases.Ability_Duration` | 63% | 持续时间 |
| `casterMustBeCapableOfViolence` | 80% | 是否要求施法者能施暴（封闭技巧） |
| `comps.compClass` | 57% | 效果类（`CompAbilityEffect_GiveHediff` / `_Stun` / `_ForceJob`） |

🔑 **熵（Entropy）是灵能的核心限制**——施放灵能产生「熵」，
**熵满 → 昏迷**。所以灵能不能无脑放，要控制节奏（实用技巧见 [../ref/content-blueprints.md](../ref/content-blueprints.md)）。
`Ability_EntropyGain` 是平衡灵能的核心旋钮。

灵能继承基类：
- `PsycastBase` —— 精神灵能（Painblock 痛觉阻断、Stun 眩晕、Burden 减速）
- `PsycastBaseSkip` —— 需要**跳过**的灵能（前置灵能未学不能学后面的）

🔑 **`PsycastBaseSkip` 的「跳过」机制**——灵能树有前置依赖，
要学高级灵能需先学低级。这是灵能树的顺序约束。

### 1.5 冥想 MeditationFocusDef（7 个）

**恢复专注（Psylocus）的方式**——每个冥想方向对应不同的能力/特质要求：

| 字段 | 说明 |
|---|---|
| `canGiveAbilities` | 能否学灵能（多数要求） |
| `requiredMentalStateDef` | 精神状态要求 |
| `thought` | 冥想后的想法 |
| `focusType` / `derivesFrom` | 灵能类型要求 |

> 复现：`python tools/game_defs/extract_defs.py --def MeditationFocusDef`

### 1.6 帝国世界对象 WorldObjectDef（33 个）

帝国在**世界地图**上的据点/徽章等对象。33 个里部分是帝国专属
（`ImperialSettlement` 破碎帝国、`StarFort` 星堡、`Monument` 纪念碑），
部分是通用的。

> 复现：`python tools/game_defs/extract_defs.py --def WorldObjectDef --dlc Royalty`

---

## 2. 层级

### 2.1 灵能链：Psylink → 能力 → 熵

```
Psylink 等级（Stat，≤ 需 Psycho敏感度）
  └─ 决定可用灵能的 level 上限
       └─ 施放灵能（Ability）
            ├─ 消耗 Psyfocus（专注，用完要冥想恢复）
            └─ 产生 Entropy（熵）
                 └─ 熵满 → 昏迷 Hediff（见 hediff.md）
```

🔑 **两条资源线**：专注（Psyfocus，技能冷却性质）vs 熵（Entropy，累积惩罚）。
专注回得快（冥想），熵回得慢且会昏迷。这是灵能平衡的双重约束。

### 2.2 头衔链：委托 → Favor → 蓝图/头衔

```
完成帝国委托（QuestScriptDef，61 个）
  └─ 得 Favor（帝国好感）
       ├─ 换蓝图（Techprint）→ 解锁 Spacer/Ultimate 技术
       │    └─ 蓝图也来自 Biotech/Odyssey DLC
       └─ 换头衔许可（RoyalTitlePermitDef）→ 新头衔
            └─ 新头衔 → 更高期望 + 更多委托 + 更强 buff
```

🔑 **蓝图和头衔共用 Favor 这条资源线**——所以玩家要权衡
「换蓝图（技术）」vs「换头衔（心情+委托）」。

### 2.3 头衔 → 期望 → 心情

```
头衔（RoyalTitleDef）
  └─ 提升 Expectations（期望）
       └─ 期望高 → 成员要求殖民地更好
            └─ 殖民地不达标 → 成员不满（心情扣，见 needs-mood.md）
                 └─ 影响头衔稳定度（可能掉头衔）
```

🔑 **头衔越高，成员越挑剔**——这是 Royalty 的核心张力。
「 Stellarch 需要大量高魅力成员」等要求，见 [needs-mood.md](needs-mood.md)。

---

## 3. 内容

### 3.1 头衔全表（11 个）

见 §1.2。官方中文标签 11/11 全有。

### 3.2 灵能全表（30 个）

按功能分组（代表性）：

| 类别 | 代表灵能 |
|---|---|
| **精神控制** | `Stun` 眩晕 · `Burden` 减速 · `Painblock` 痛觉阻断 |
| **心灵灼烧** | `MindBurn` 等（造成心理伤害） |
| **激励/祝福** | 给 Hediff 的增益 |
| **预言/感知** | 窥视未来、感知实体 |
| **唤能** | 授予他人临时灵能 |
| **仪式咒语** | `SpeechBase` 系（领袖演讲等 Ideology/Royalty 联动） |

> 完整 30 行：`python tools/game_defs/extract_defs.py --def AbilityDef --dlc Royalty`
> 每条的专注消耗/熵产生：
> 加 `--fields statBases.Ability_PsyfocusCost statBases.Ability_EntropyGain`

### 3.3 冥想方向全表（7 个）

多数要求 `Psycast` 灵能前置。

---

## 4. 解读

> 本节机制串联自本地 Def + 灰机 wiki。

### 4.1 灵能的双重资源约束

灵能不是「无限放」：
- **Psylocus（专注）**= 技能冷却，放完要冥想恢复
- **Entropy（熵）**= 累积惩罚，满了昏迷

所以灵能的战战术是**间歇性爆发**，不是持续压制。战斗时要在
「眩晕/减速打一套 → 昏迷」之间找平衡。

### 4.2 头衔的期望机制 = 「高地位高要求」

头衔给 buff，但同步提升 Expectations。成员对殖民地有更高要求，
不达标就掉头衔（心情大扣）。

对 mod 的意义：Royalty 玩法的张力是「头衔带来好处也带来压力」——
平衡改动要同时看 buff 和 expectation 门槛。

### 4.3 Favor 是 Royalty 的核心经济

帝国委托、找神器、清理怪兽都换 Favor；Favor 换蓝图或头衔。
所以 Royalty 玩法是**「外交/冒险 → 帝国技术/地位」**的循环。

### 4.4 蓝图跨 DLC

蓝图（Techprint）用来解锁 Spacer/Ultimate 级技术，但**蓝图本身来自
其他 DLC**（Biotech 基因、Odyssey 星舰部件）。所以 Royalty 的
「Favor 换蓝图」实际是跨 DLC 内容的枢纽——它把各 DLC 串起来。

---

## 5. modder 常见坑

1. **灵能能力继承 `PsycastBase` 或 `PsycastBaseSkip`**——
   新灵能选错父类会导致前置依赖失效（`PsycastBaseSkip` 有「跳过」语义）。
2. **熵（`Ability_EntropyGain`）是平衡核心**——新增灵能不配熵产生会过强。
3. **头衔是许可制的**——不是升级自动给，要过 `RoyalTitlePermitDef` 解锁。
4. **头衔的期望会随头衔提升**——改 buff 时注意同步 Expectations，否则玩家
   拿 buff 却掉头衔。
5. **Favor 同时用于蓝图和头衔**——改蓝图成本会影响头衔获取路径。
6. **灵能要求 Psycho敏感度（Stat）**——`casterMustBeCapableOfViolence` 和
   敏感度门槛是能不能施放的前提（见 [production.md](production.md)）。
7. **Blueprint 是跨 DLC 的**——引用蓝图要处理来源 DLC 未加载的情况。
