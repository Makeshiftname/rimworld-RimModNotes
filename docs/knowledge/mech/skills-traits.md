# 技能与特质（Skill / Trait / 后天）

> **mech 层**：本文只讲**技能与特质系统由什么构成、字段含义、内容清单**。
> 「技能怎么通过工作实际生效」见 [work.md](work.md)（WorkGiver 用技能掷骰）；
> 「技能等级怎么通过学习行为提升」参考 `Job`/`ThinkTree` 机制（见 [work.md](work.md)）；
> 「Ideaology 的戒律如何影响特质」见 [ideology.md](ideology.md)。
>
> **数据源与核对日期（2026-10-16）**：
> 本地 `F:\Games\GBL\rimworld\Data\`（RimWorld 1.6，全 6 DLC），中文取官方简中 DefInjected。
> 复现：`python tools/game_defs/extract_defs.py --def SkillDef TraitDef`。
>
> 规模：**SkillDef 12（全部 Core）· TraitDef 51（Core 43 · Anomaly 6 · Biotech 2）**。

---

## 1. 框架

### 1.1 两层结构

```
Pawn（生物）
  ├─ Skills ──── SkillDef（12 种技能）
  │     └─ 每个技能有：等级 0–20 + 熟练度（passion: 无/喜好/狂热）+ 激情下限
  ├─ Traits ─── TraitDef（51 种特质）
  │     └─ 每个特质有 degreeDatas（可多档度，如乐观/乐观主义程度）
  └─ 后天 ────── InspirationDef（灵光，8 种）+ 后天技能曲线
```

🔑 **技能是「能做什么、做得怎样」，特质是「天生倾向」**：
技能可通过练习提升，特质出生即固定（后天天赋除外）。

### 1.2 技能 SkillDef（12 种，全 Core）

| defName | 中文 | 说明 |
|---|---|---|
| `Shooting` | 射击 | 远程战斗（枪械、弓） |
| `Melee` | 格斗 | 近战战斗 |
| `Medicine` | 医疗 | 治疗、手术 |
| `Construction` | 建造 | 建墙、家具 |
| `Crafting` | 手工 | 制作装备、艺术品 |
| `Mining` | 采矿 | 开采矿石 |
| `Plants` | 种植 | 种植作物 |
| `Cooking` | 烹饪 | 做饭、屠宰 |
| `Animals` | 驯兽 | 训练动物 |
| `Artistic` | 艺术 | 绘画、雕塑 |
| `Social` | 社交 | 说服、成交 |
| `Intellectual` | 智识 | 研究 |

🔑 **只有 12 种**——游戏本体不多；mod 的技能（如 hacking 破解、骑术）要么
复用这 12 种的变体，要么整个新加 `SkillDef`（在 `biotech` 里 hacking 由 Social 派生）。

**SkillDef 的字段**（Shooting 为例，代码内置曲线不在 XML 里）：

| 字段 | 说明 |
|---|---|
| `defName` / `skillLabel` | 内部名 / 显示标签 |
| `description` | 描述（`generalRules` 里的规则字符串用于生成描述里的名词） |
| `listOrder` | 排序（决定 UI 顺序） |
| `disablingWorkTags` | **禁用哪些工作标签**（如 Shooting 禁 `Violent`——不能调去种田） |
| `neverDisabledBasedOnWorkTypes` | 是否永不禁用 |
| `pawnCreatorSummaryVisible` | 角色创建时是否显示 |
| `lessonInteraction` | 学习互动（可 `MayRequire` DLC） |

⚠️ **技能经验曲线是代码内置的**（`Skills.xml` 里只有上述字段，没有每级经验数值）。
想改技能成长曲线必须打 Harmony 补丁。

### 1.3 技能等级 / 激情

每个 pawn 的每个技能有：
- **等级 0–20**（被动积累，通过使用相关工作/战斗提升）
- **激情 / passion**（`None` 无 · `Minor` 喜好 · `Major` 狂热）——
  影响学习速度。**狂热的学习速度是无激情的数倍**，这是 RimWorld 培养专长的核心。
- **激情下限**（passion floor，Ideology 等会修改它）

技能通过**实际工作**（WorkGiver 掷骰）+ **战斗**提升，不是靠菜单加点。

### 1.4 特质 TraitDef（51 种）

🔑 **特质的关键结构是 `degreeDatas`**——一个特质可以有**多个档度**
（如「乐观」有轻度乐观 / 高度乐观 / 抑郁三档）：

```xml
<TraitDef>
  <defName>Neurotic</defName>          <!-- 特质家族名 -->
  <commonality>0.7</commonality>        <!-- 出现权重 -->
  <conflictingTraits>                  <!-- 互斥特质 -->
    <li>Nerves</li>
  </conflictingTraits>
  <exclusionTags>                      <!-- 互斥标签（更细粒度） -->
    <li>Toughness</li>
  </exclusionTags>
  <degreeDatas>
    <li>
      <label>depressive</label>        <!-- 这一档的标签 -->
      <degree>-2</degree>              <!-- 档度值，正/负 -->
      <description>...</description>
      <statOffsets.MentalBreakThreshold>-0.18</statOffsets.MentalBreakThreshold>
    </li>
    <li>
      <label>anxious</label>
      <degree>-1</degree>
      <statOffsets.MentalBreakThreshold>-0.09</statOffsets.MentalBreakThreshold>
    </li>
  </degreeDatas>
</TraitDef>
```

- **`degreeDatas` 有多少档**，这个特质就有多少「形态」；pawn 具体获得哪档由生成时掷骰。
- **`degree`** 表示档度的方向和强度（正 = 正面，负 = 负面）。
- 一个 TraitDef 可以有互斥的多个档度（如 `Abrasive` 的粗暴 ↔ 和善）。

> ⚠️ **和想法一样，特质的中文文案在 `degreeDatas[].label`**，顶层 label 罕见。
> 51 个 TraitDef 的顶层中文标签是 0——全部文案在 `degreeDatas` 里（51/51 有）。

### 1.5 特质的影响手段

特质通过三种方式改变数值：

| 方式 | 字段 | 例子 |
|---|---|---|
| **属性乘算** | `statFactors` | `CertaintyLossFactor` ×2（Ideology 认可度损失，见 [ideology.md](ideology.md)）· `IncomingDamageFactor` ×0.5 |
| **属性加算** | `statOffsets` | `GlobalLearningFactor` ±0.75 · `MentalBreakThreshold` ±0.18（精神崩溃阈值） |
| **禁用需求** | `disablesNeeds` | 关掉 `Outdoors` / `Food`（某些怪胎不需要） |

其他特质机制：
- `requiredWorkTags`：有这特质才能做某类工作（如需要 `Violent`）
- `theOnlyAllowedMentalBreaks` / `randomMentalState`：限定能得的精神崩溃类型
- `disallowedInspirations` / `disallowedMeditationFocusTypes`：禁用某些灵光/冥想
- `allowedMeditationFocusTypes`：允许的冥想方向
- `marketValueFactorOffset`：影响该 pawn 的市场价（基因交易等）

### 1.6 特质互斥

两条互斥机制并存：

| 机制 | 字段 | 粒度 |
|---|---|---|
| **直接互斥** | `conflictingTraits` | 指定具体互斥的特质（13/51 有） |
| **标签互斥** | `exclusionTags` | 按标签互斥（7/51 有，如 `Toughness` 类互斥） |

🔑 **改特质要同时看这两种互斥**——只改 `conflictingTraits` 可能仍被 `exclusionTags` 挡住。

### 1.7 特质性别差异

`commonality`（通用权重）与 `commonalityFemale`（女性权重）分开——
某些特质明显偏向某性别。本地有 2 个特质带 `commonalityFemale`（如 Nudist 裸体癖）。

### 1.8 后天（Inspiration）与灵光曲线

`InspirationDef`（8 种）是**后天带来的独特天赋**（如 `Frenzy_Shoot` 战斗狂热、
`Frenzy_Go` 狂奔），一次性获得、永久改变一个技能或状态。

后天**不是提升技能等级**，而是给技能一个「灵光下限」——即 passion floor——
让人立刻对某技能达到狂热并快速成长。

---

## 2. 层级

### 2.1 特质的档度（degree）层级

```
TraitDef（如 Neurotic 神经质）
  └─ degreeDatas（多档）
       ├─ degree -2  depressive（抑郁）→ MentalBreakThreshold -0.18
       ├─ degree -1  anxious（焦虑）   → MentalBreakThreshold -0.09
       └─ (可选) degree 0 / +1 正常档
```

🔑 一个 pawn 只会拿到**其中一档**。档度决定强度（如 -2 的抑郁比 -1 的焦虑更极端）。

### 2.2 技能 → 工作 → 提升的闭环

```
WorkTypeDef（工作类型，如种植）
  └─ WorkGiverDef（执行者）→ 需要 SkillDef
       └─ 掷骰：技能等级 + 激情 + 心情 + 特质修正 → 成功率
            └─ 成功 → 产出 + 技能经验
                 └─ 经验满 → 技能升级
```

所以**技能是「用得多就强」**——想让某人变强，让他多干对应的活，
或给他狂热 passion（见 §1.3）。这是 mod 平衡的基石。

### 2.3 特质 ↔ 心情 ↔ 精神状态

```
特质（负面档，如抑郁）
  └─ statOffsets.MentalBreakThreshold ↓（更易崩溃）
       └─ 心情跌破阈值 → 精神崩溃（MentalState）
            └─ 需 Psychopath 等抵消（见 needs-mood.md）
```

`Neurotic`/`Depressive` 的 `MentalBreakThreshold` 是负的——这特质**天生更易崩溃**，
是它的核心代价。

---

## 3. 内容

### 3.1 技能全表（12 种，全 Core）

见 §1.2 的完整表。官方中文标签 12/12 全有。

> 复现：`python tools/game_defs/extract_defs.py --def SkillDef`

### 3.2 特质全表（51 种）

规模：**Core 43 · Anomaly 6 · Biotech 2**。按性质分组（代表性，完整表见脚本）：

**正面特质**：`Kind` 和善 · `Nerves` 沉着 · `Optimist` 乐观 · `FastWalker` 快走 ·
`Tough` 坚韧 · `NaturalMood` 天然心境 · `Masculinist`/`Femininist` 性别倾向 ·
`Industriousness` 勤劳 · `Greedy` 贪婪 · `Jealous` 嫉妒（有代价）

**负面特质**：`Abrasive` 粗暴 · `Neurotic` 神经质 · `Sanguine` 冲动 ·
`Wimp` 胆小 · `Pessimist` 悲观 · `Depressive` 抑郁 · `Masochist` 受虐 ·
`Pyromaniac` 纵火犯 · `Violent` 暴力 · `Cannibal` 食人族 · `Nudist` 裸体癖

**特殊/玩法特质**：`Psychopath` 心理变态（免疫大量负面想法）·
`ChemicalInterest` 化学兴趣 · `ChemicalAversion` 化学厌恶 ·
`SpeedOffset` 速度偏移 · `Insomniac` 失眠 · `Tortured` 折磨 · `Transhumanist` 技术至上

**Ideology/Anomaly/Biotech 新增**：Ideology 的 `Transhumanist` ·
Anomaly 的 6 个（与虚空生物相关）· Biotech 的 2 个。

> 完整 51 行清单 + 每条的 degreeDatas 详情：
> `python tools/game_defs/extract_defs.py --def TraitDef`
> 字段级细节（各特质调了什么属性、互斥什么）：
> `python tools/game_defs/extract_defs.py --def TraitDef --fields degreeDatas.li.degree degreeDatas.li.statFactors.CertaintyLossFactor`

### 3.3 特质的关键属性修正统计

从本地 51 个特质实测：

| 修正类型 | 涉及特质数 | 例子 |
|---|---:|---|
| `MentalBreakThreshold` 加/减 | ~10 | 抑郁 −0.18、易崩溃 |
| `CertaintyLossFactor` 乘算 | 8 | 影响 Ideology 认可度损失 |
| `GlobalLearningFactor` 加/减 | 3 | 加快/减慢学习 |
| `IncomingDamageFactor` 乘算 | 2 | 受伤倍率 |
| `disablesNeeds` | 2 | 关掉 `Food` / `Outdoors` 需求 |
| `theOnlyAllowedMentalBreaks` | 3 | 限定崩溃类型 |

---

## 4. 解读

### 4.1 技能 + 激情是「专精经济」的核心

RimWorld 的技能系统本质是**「用进废退 + 激情加速」**：
- 技能靠**实际使用**积累经验（不是分配点数）
- **激情（passion）是关键**：狂热（Major）的学习速度远高于无激情
- 所以管理技巧是**让正确的人做正确的事**：把有激情的指到对应工作

对 mod 的意义：新增一个「技能」时，要同时提供
① 让它能被某个 `WorkGiver` 使用、② 影响某个 `StatDef` 或工作速度。

### 4.2 特质是「天生倾向」，伴代价

设计模式：每个正面特质几乎都有一个**隐藏代价**
（对应字段 `statOffsets` 或 `exclusionTags`）：
- `Kind` 和善 → 心情更好，但可能社交冲突少
- `Greedy` 贪婪 → 交易更划算，但道德
- `Nerves` 沉着 → 不易崩溃，但可能少了什么

`Psychopath`（心理变态）是极端例子——它免疫大量负面想法（见 [needs-mood.md](needs-mood.md) §4.3），
代价由其他特质（`Kind` 等）制衡。

### 4.3 特质与 Ideology 深度联动

**某些 Ideology 戒律会对特定特质失效或反转**：

- `Transhumanist`（技术至上）会被**纯血派** Ideo 排斥
- `Psychopath` 免疫的负面想法里，有些恰是 Ideology 戒律产生的——
  所以**心理变态者对某些文化更宽容**
- Ideology 的 `MemeDef.agreeableTraits` / `disagreeableTraits` 指定哪些特质讨喜/招嫌

见 [ideology.md](ideology.md) §3.1（MemeDef 的 `agreeableTraits` 字段）。

### 4.4 特质的「档度」让同一特质有强弱

以 `Abrasive`（粗暴）为例——它有正面和负面两档（粗鲁 vs 和善），
pawn 生成时掷骰决定。这种「正负双档」设计让同一特质家族覆盖更广的人格光谱，
避免特性单一。

### 4.5 基因影响技能与特质（Biotech）

Biotech 的 `XenotypeDef`（异种人）可以**规定一组基因**，
基因进而影响技能成长和特质倾向（如 `Genes` 改 `GlobalLearningFactor`）。
详见 [genes.md](genes.md)。

---

## 5. modder 常见坑

1. **技能只有 12 种且全在 Core**。想加新技能（如 hacking）要么复用变体
   （`Social` 派生），要么新建 `SkillDef` 并配 `WorkGiver`/`StatDef`——
   光加 `SkillDef` 不会让它有任何效果。
2. **技能经验曲线是代码内置的**，`Skills.xml` 里改不了，改曲线要打 Harmony 补丁。
3. **特质文案在 `degreeDatas[].label`，不在顶层**（51/51 特质顶层 label 为空）。
   加新特质时每档都要写 label/description。
4. **`degreeDatas` 的 `degree` 值决定档度方向**，不是大小——`-2` 是「非常负面」
   不是「轻微负面」。
5. **特质互斥有两套**（`conflictingTraits` + `exclusionTags`），
   改互斥要同时看两种。
6. **`commonalityFemale` 单独控女性权重**，想调性别倾向用这个，别改 `commonality`。
7. **技能通过工作提升**，新增技能要保证有 `WorkGiver` 会用它，否则永远 0 级。
8. **`MentalBreakThreshold` 是特质与心情/精神状态的接口**
   （见 [needs-mood.md](needs-mood.md)）——加崩溃类特质必看这个字段。
