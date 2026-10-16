# 需求与心情（Needs / Mood / Thoughts）

> **mech 层**：本文只讲**需求-心情-想法系统由什么构成、字段含义、数据模型、内容清单**。
> 「需要做饭/睡觉的工作是怎么排的」见 [../game-and-world-components.md](../game-and-world-components.md)（Job 系统）；
> 「怎么写一个影响心情的 mod」参考 Ideology 的戒律机制（[ideology.md](ideology.md) §4）。
>
> **数据源与核对日期（2026-10-16）**：
> 本地 `F:\Games\GBL\rimworld\Data\`（RimWorld 1.6，全 6 DLC），中文取官方简中 DefInjected。
> 复现：`python tools/game_defs/extract_defs.py --def NeedDef ThoughtDef MentalStateDef InspirationDef JoyKindDef`。
>
> 规模：**NeedDef 24 · ThoughtDef 923 · MentalStateDef 46 · InspirationDef 8 · JoyKindDef 10**。

---

## 1. 框架

### 1.1 心情的计算模型

```
Pawn（生物）
  ├─ Need（需求）           ── 身体/心理的基本驱动力，随时间衰减
  │     └─ NeedDef           定义：多少满足、怎么补充、掉了什么心情
  │
  ├─ Thought（想法）         ── 短期情绪叠加，来自事件/环境/记忆
  │     └─ ThoughtDef        每个想法带 baseMoodEffect（+5 / -30）
  │
  ├─ MentalState（精神状态） ── 严重的精神问题，如崩溃、精神狂乱
  │     └─ MentalStateDef    触发阈值（基于心情）、强制行为、恢复方式
  │
  └─ Mood（心情）           ── ══ 所有需求满足度 + 想法累加后的一个总数值 ══
        ├─ 需求低 → 心情扣分
        ├─ 想法 ± → 心情加减
        ├─ 精神状态 → 严重扣分
        └─ 低于阈值 → 触发 MentalState（崩溃等）
```

🔑 **心情不是一个独立的可编辑数值**——它是**需求和想法共同推导出的综合结果**。
统计页面的 `Mood` 百分比是当前所有因素加权后的净值。

### 1.2 需求（Need）

需求是**持续衰减、需要行为补充**的驱动力。每个需求对应一条对应的 Job
（饿了→找吃，困了→去睡）。

| 需求类别 | 实例 | 说明 |
|---|---|---|
| **生理** | `Food` 饮食 · `Rest` 休息 · `Beauty` 美观 · `Comfort` 舒适 | 最基础，掉到 0 会严重扣心情 |
| **环境** | `Outdoors` 室外 · `Indoors` 室内 · `RoomSize` 房间尺寸 | 待在合适环境会满足 |
| **娱乐** | `Joy` 娱乐 · `Play` 玩乐（Biotech）· `Learning` 学习（Biotech） | 休闲活动 |
| **化学** | `Chemical_Alcohol` 酒精 · `Chemical_Smokeleaf` 大麻 · `Chemical_GoJuice` 活力水 · `Chemical_Luciferium` 魔鬼素 · `Chemical_Psychite` 精神叶 · `Chemical_WakeUp` 清醒丸 · `Chemical_Ambrosia` 仙馐果 | 成瘾性需求，都继承 `DrugAddictionNeedBase` |
| **药品** | `DrugDesire` 药品 | 想吃药（成瘾者的通用需求） |
| **心情** | `Mood` 心情 | 元需求：心情本身也作为一个需求被追踪 |
| **Ideology** | `Suppression` 压制 | 想压制信仰/规矩的欲望 |
| **Royalty** | `Authority` 权威 | 想追求头衔权威（1.6 尚未有中文标签） |
| **Biotech** | `Deathrest` 死眠 · `KillThirst` 杀欲 · `MechEnergy` 能量 | 死眠者需求、机械族能量 |
| **压制/需求** | `Suppression` · `DrugDesire` | 特殊欲望 |

一个 `NeedDef` 描述：需求名称、满足曲线（多少点算满足）、
**相关 Hediff**（如酒精需求高时会有「醉」Hediff）、耗尽后触发什么。

### 1.3 想法（Thought）——心情的真正来源

923 个 `ThoughtDef` 是心情的主要加减项。**关键结构：`stages`**。

```xml
<ThoughtDef>
  <defName>Inebriated</defName>
  <thoughtClass>Thought_MemoryObservation</thoughtClass>
  <durationDays>1</durationDays>          <!-- 持续天数 -->
  <stackLimit>100</stackLimit>            <!-- 可叠加层数上限 -->
  <stages>
    <li>
      <label>alcohol warmth</label>
      <description>喝了点酒，感觉挺放松。</description>
      <baseMoodEffect>10</baseMoodEffect>  <!-- 这层心情 +10 -->
    </li>
    <li>
      <label>drunk</label>
      <baseMoodEffect>20</baseMoodEffect>  <!-- 更醉 +20 -->
    </li>
    <li>
      <label>hammered</label>
      <baseMoodEffect>26</baseMoodEffect>
    </li>
  </stages>
</ThoughtDef>
```

🔑 **`stages` 让同一个想法随强度/时间递进**，每个阶段心情效果更大。
这跟 Hediff 的 `stages` 是同一个设计模式（见 [hediff.md](hediff.md) §2.1）。

**想法的触发来源（`thoughtClass` / `workerClass`）**：

| 来源 | class | 说明 |
|---|---|---|
| **Hediff 触发** | `ThoughtWorker_Hediff` | 有某个 Hediff 时持续产生（如「我醉了」） |
| **期望触发** | `ThoughtWorker_Expectations` | 需求/期待被满足的程度（贵重期待） |
| **故事触发** | `ThoughtWorker_Tale` | 过去的记忆/故事 |
| **事件观察** | `Thought_MemoryObservation` | 看到/经历某事 |
| **社交记忆** | `Thought_MemorySocial` | 关于某人的印象 |
| **进食** | `Thought_FoodEaten` | 吃了什么 |

`workerClass` 决定这个想法**什么时候被触发和刷新**（328/923 有 workerClass）。

### 1.4 想法的抵消机制

923 个想法里大量带「免于触发」的字段：

| 字段 | 出现 | 说明 |
|---|---:|---|
| `nullifyingTraits` | 17% | 某些特质让你**不再产生**这个想法（如 `Psychopath` 免疫血腥想法） |
| `nullifyingHediffs` | 12% | 某些 Hediff 免疫（如 `DetoxifierLung` 免疫某些负面） |
| `requiredTraits` | 4% | 只有具备某特质才产生（如食人族才有食人想法） |
| `developmentalStageFilter` | 14% | 限 babies/children/adults |
| `doNotApplyToQuestLodgers` | 11% | 不对任务客人/寄居者生效 |

> 💡 **`Psychopath`（心理变态）** 免疫大量负面想法——这是它强力 Traits 的底层机制。
> 158 条想法带 `nullifyingTraits`，这个特质的价值远超表面。

### 1.5 想法的叠加

| 字段 | 说明 |
|---|---|
| `stackLimit` | 同一想法最多叠几层（39% 有） |
| `stackedEffectMultiplier` | 每叠一层，效果衰减（如 0.75 → 第 2 层只加 75%） |
| `stackLimitForSameOtherPawn` | 针对**同一个人**的同类想法的叠加上限 |
| `showBubble` | 是否在头顶显示气泡 |

### 1.6 精神状态（MentalState）

46 个 `MentalStateDef`——当心情跌到某个阈值触发的**极端精神状态**，
会强制 pawn 做出失控行为（杀人、逃跑、僵住）。

触发基于**心情阈值**（如心情 < 20% 触发 `Berserk`）。
状态本身带**强制行为**（如 berserk 会攻击最近的人）、
**恢复方式**（时间、药物如 `Luciferium`，或环境改善）。

这与 Hediff 不同——精神状态不是「病」，是「行为失控模式」，
更接近一个 AI 状态机。

### 1.7 娱乐与灵光（Joy / Inspiration）

- **`JoyKindDef`（10 个）**：玩乐的方式（社交、吃饭、睡觉、冥想等）。
  满足 `Joy` 需求产生想法和心情。
- **`InspirationDef`（8 个）**：灵光一闪——触发创作/设计行为（写诗、雕刻），
  满足后会大幅加心情。

---

## 2. 层级

### 2.1 心情的三层叠加

```
第一层：需求满足度（背景色）
  └─ 需求高于阈值 → 不扣；低于阈值 → 按缺口扣（越来越低扣越多）
     └─ 需求严重不足（如饿死前）会触发 MentalState

第二层：想法叠加（主动波动）
  └─ 好事（吃好饭、赢战斗）→ +5 ~ +30
  └─ 坏事（亲人死亡、被打）→ -5 ~ -50
     └─ 坏想法可被 nullifyingTraits/Hediffs 抵消

第三层：精神状态（一票否决）
  └─ 心情跌破阈值 → Berserk / MentalBreak
     └─ 精神状态期间心情被强制拉到很低，且行为失控
```

> 💡 **设计的精妙处**：需求是慢变量（慢性扣分），想法是快变量（瞬时波动），
> 精神状态是防崩溃的保险丝。三层配合让心情既有稳定又有变化。

### 2.2 化学需求的成瘾闭环

```
Chemical_X（化学需求）低
  └─ 想摄入 → 用对应的物品/药物
       └─ 摄入后：
            ├─ 短期：产生正面想法（Inebriated 等）
            ├─ 长期：Chemical_X 需求满足，但产生耐药 Hediff
            └─ 停用：需求反弹 + 戒断想法（负面）
  └─ 成瘾（Hediff Addiction）持续要求摄入，否则持续负面想法
```

这与 Hediff 的酒精环是同一套（见 [hediff.md](hediff.md) §2.3）——
`Chemical_X` 需求 + Hediff 耐药/成瘾 + `Thought` 三者构成一个完整闭环。

### 2.3 Ideology 压制需求

`Suppression`（压制）是 Ideology DLC 独有的需求——
反映「想按自己那套规矩行事」或「想挣脱规矩」的欲望。
它被 `MemeDef` / `PreceptDef` 影响：某些模因下压制欲降低或升高
（见 [ideology.md](ideology.md)，如 `PainIsVirtue` 苦行者）。

### 2.4 想法 → 能力的连锁

```
想法（正面/负面） → 心情变化 → 影响技能效率 / 决策 AI
  └─ 例：「我富有了」→ 心情+ → 社交技能判定+
  └─ 例：「我很害怕」→ 心情- → 逃跑倾向
```

心情不是终点——它影响**技能掷骰**和**AI 决策**。低心情的 pawn 更易崩溃、
拒绝工作；高心情的 pawn 工作效率高。

---

## 3. 内容

### 3.1 需求全表（24 个）

官方中文标签 23/24（`Authority` 尚未翻译）：

**Core（17）**：`Food` 饮食 · `Rest` 休息 · `Beauty` 美观 · `Comfort` 舒适 ·
`Outdoors` 室外 · `Indoors` 室内 · `RoomSize` 房间尺寸 · `Joy` 娱乐 ·
`Mood` 心情 · `DrugDesire` 药品 · `Chemical_Alcohol` 酒精 ·
`Chemical_Smokeleaf` 大麻 · `Chemical_GoJuice` 活力水 ·
`Chemical_Luciferium` 魔鬼素 · `Chemical_Psychite` 精神叶 ·
`Chemical_WakeUp` 清醒丸 · `Chemical_Ambrosia` 仙馐果

**Biotech（5）**：`Deathrest` 死眠 · `KillThirst` 杀欲 · `Learning` 学习 ·
`MechEnergy` 能量 · `Play` 玩乐

**Ideology（1）**：`Suppression` 压制

**Royalty（1）**：`Authority` 权威

### 3.2 想法全表（923 个）

规模：**Ideology 433（最多）· Core 281 · Biotech 86 · Anomaly 65 · Royalty 39 · Odyssey 19**。

> 🔎 **Ideaology 占了 433 个**——因为每条戒律（Precept）都触发对应想法（见
> [ideology.md](ideology.md)）。文化系统是想法的最大来源。

按来源分组（代表性）：

| 来源 | 数量级 | 代表 |
|---|---:|---|
| **Ideology 戒律** | 433 | `ButcheredHumanlikeCorpse`（碎尸）· `CharityFulfilled_*` / `CharityRefused_*`（仁善）· `Blindness_*`（失明） |
| **酒精/药物** | ~50 | `Inebriated` 醉 · `Hungover` 宿醉 · `AlcoholWithdrawal` 戒断 |
| **食物** | ~40 | `AteNutrientPasteMeal` 吃营养膏 · 各种 `AteXXX` |
| **记忆/社交** | ~100 | `DeathMemory*` 死亡记忆 · 家人死亡 · 被背叛 |
| **期望** | ~80 | 高低期待（奢华房间/破屋） |
| **创伤** | ~60 | 截肢 · 器官被摘 · 长期监禁 |

> ⚠️ **923 个想法里只有 4 个有顶层中文 `label`**——因为原版多数想法的文案在
> `stages.li.label` / `stages.li.description` 里，**顶层 label 很少用**。
> 这不影响游戏（UI 读 stages 的文案），但写 mod 时要知道**文案写在哪**：
> 顶层 `label` 是少数情况，**绝大多数想法的显示文本在 `stages[].label`**。

完整清单：`python tools/game_defs/extract_defs.py --def ThoughtDef`

### 3.3 精神状态全表（46 个）

包括：崩溃 `Break` · 狂暴 `Berserk` · 逃跑 `Flee` · 僵住 `Freeze` ·
精神狂乱 `InsaneIndividualVerbs` 等。

完整清单：`python tools/game_defs/extract_defs.py --def MentalStateDef`

### 3.4 娱乐与灵光

- `JoyKindDef`（10）：社交 · 吃饭 · 睡觉 · 冥想 · 运动等玩乐类型
- `InspirationDef`（8）：写诗 · 雕刻等灵光触发

---

## 4. 解读

> 本节为机制串联，来自本地 Def 交叉印证 + 灰机 wiki。

### 4.1 心情 = 「需求健康 + 想法账本」的净结果

统计页显示的 `Mood`（心情）百分比，其计算本质是：
```
心情 ≈ 需求满足的加权贡献 + 所有活跃想法的 baseMoodEffect 累加
```
- 需求部分：每项需求按满足程度贡献，低于阈值开始扣
- 想法部分：所有想法的 `baseMoodEffect` 按层叠加，受 `stackLimit` /
  `stackedEffectMultiplier` / `nullifyingTraits` 调制

**所以「心情低」有两种原因**：长期需求没满足（慢病），或负面想法堆叠（急性）。
诊断心情问题要先分清是哪种。

### 4.2 「需求」不只是生理需求

24 个需求里，只有 7 个（Food/Rest/Beauty/Comfort/Outdoors/Indoors/RoomSize）是传统生理需求。
其余 17 个是**欲望**（Joy 玩乐、Learning 学习、KillThirst 杀欲、
Deathrest 死眠、各种化学成瘾……）。这意味着 RimWorld 的需求系统本质上
是一个**通用的动机框架**，不局限于生存。mod 可以基于它加新动机。

### 4.3 「Psychopath」的底层机制

`Psychopath`（心理变态）通过 158 条想法的 `nullifyingTraits` 免疫大量负面情绪
（血腥、死亡、别人的痛苦等）。**它不是简单加一个数值，而是让你几乎不会因为
负面事件而心情受损**。这也解释了为什么它在游戏里那么强，也为什么要用 Ideology
文化（某些戒律会对 Psychopath 失效）来平衡。

### 4.4 化学成瘾的完整闭环

以酒精为例，需求-想法-Hediff 三者是这样咬合的：

```
Chemical_Alcohol 需求降低
  └─ 想喝酒 → 喝 Beer/Whiskey
       ├─ 喝完：Inebriated 想法（+10/+20/+26 分层）── 心情上升
       ├─ 需求被满足，但产生 AlcoholTolerance（耐药 Hediff）── 需要喝更多
       ├─ 继续喝：AlcoholAddiction（成瘾 Hediff）
       └─ 停止：AlcoholWithdrawal 想法（负面）── 心情大跌，可能戒断症状
```

这是「需求（想要）→ 想法（即时情绪）→ Hediff（长期生理）」三层配合的典范。
加一味新药（Custom Drug）要同时动这三层，参考
[../ref/content-blueprints.md](../ref/content-blueprints.md) 的药物蓝本。

### 4.5 精神状态是「保险丝」

心情跌破阈值 → 触发精神状态（MentalState）。
精神状态不是病，是**行为失控状态机**：
- `Berserk`：攻击最近的目标
- `Flee`：逃离
- `Freeze`：僵住不动
- `Break`：崩溃，无法行动

它设计成**低心情的极端后果**，防止玩家无视心情管理。
`Luciferium`（魔鬼素）能**临时阻止**精神状态触发——这是它作为必备药品的原因。

> 💡 做「精神压力」相关的 mod，直接改 `MentalStateDef` 的触发阈值或用
> `Luciferium` 机制（临时免疫精神状态）是最直接的切入点。

### 4.6 心情影响技能和 AI

低心情 pawn：
- 更易崩溃、拒绝工作
- 技能掷骰受心情惩罚（心情影响工作/战斗效率）
- AI 更可能逃跑/罢工

高心情 pawn：
- 工作效率高
- 更愿意接受任务
- 社交技能判定受益

所以「经营心情」本质是**提升全队生产力和稳定性**的手段。

### 4.7 Ideology 想法是文化系统的输出端

Ideaology 的 433 个想法是「戒律违反/遵守」的**反馈端**。
一条戒律（Precept）绑定若干想法（`comps.thought`），当事件触发历史事件
（`eventDef`）时，该想法产生并增减心情——这就是「文化如何影响心情」的完整链路：

```
事件（吃了人肉/拒绝施舍/失明）──▶ HistoryEventDef
  └─▶ 对应戒律的 comps 匹配 → 产生 Thought
       └─▶ baseMoodEffect → 心情变化
            └─▶ 长期低于阈值 → 精神状态
```

（戒律的 `comps` 结构见 [ideology.md](ideology.md) §1.3）

---

## 5. modder 常见坑

1. **想法的文案在 `stages[].label`，不是顶层 `label`**。923 个想法里绝大多数
   顶层 label 是空的，UI 读的是每个 stage 的 label。新加想法时**每个 stage 都要写 label**。
2. **想法用 `stages` 分层**，效果随强度递进。别只写一个 stage 就以为完事了。
3. **`nullifyingTraits` 是强力字段**。给负面想法加这个字段（如给一个 mods 的坏心情加
   `Psychopath` 免疫）会大幅改变平衡，改动前想清楚。
4. **需求不只是生理的**。通用动机框架，可以加「学习」「社交」等新需求，
   但每个需求都要配对应的 Job，否则满足了也扣「找不到行为」的心情。
5. **精神状态触发基于心情阈值**。改 `MentalStateDef` 阈值要小心，可能让整个游戏变难/变易。
6. **`Chemical_X` 需求 + Hediff + Thought 要成套加**。只加需求不给想法，
   或只加 Hediff 不配需求，逻辑会断裂。
7. **心情高≠能力强**。心情影响技能掷骰和 AI，不是直接加属性。
8. **存档兼容**：自定义需求/想法若给 `ThoughtWorker` 换了个类，
   老存档反序列化会失败，用 Scribe 默认回退（见
   [../game-and-world-components.md](../game-and-world-components.md) §3）。
