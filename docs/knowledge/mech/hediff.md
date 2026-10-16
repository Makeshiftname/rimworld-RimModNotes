# 健康 Hediff（疾病 / 伤口 / 能力损伤 / 身体部位）

> **mech 层**：本文只讲**健康系统由什么构成、字段含义、数据模型、内容清单**。
> 「怎么写一个能用的 HediffDef」见 [../ref/content-blueprints.md](../ref/content-blueprints.md)
> （含 Custom Drug 药物效果 Hediff 蓝本）；
> 「存档里 Hediff 怎么存」见 [../game-and-world-components.md](../game-and-world-components.md)（IExposable + Scribe）。
>
> **数据源与核对日期（2026-10-16）**：
> 本地 `F:\Games\GBL\rimworld\Data\`（RimWorld 1.6，全 6 DLC），中文取官方简中 DefInjected。
> 复现：`python tools/game_defs/extract_defs.py --def HediffDef BodyPartDef DamageDef InfectionPathwayDef HediffGiverSetDef`。
>
> 规模：**HediffDef 324 · BodyPartDef 141 · DamageDef 52 · InfectionPathwayDef 3 · HediffGiverSetDef 4**。

---

## 1. 框架

### 1.1 五个 Def 的关系

```
Pawn（生物）
  └─ HediffSet（Hediff 的集合）
       ├── Hediff（运行期的一个病/伤实例）
       │     ├── HediffDef  ──────── 静态定义：这是什么病、分几期、每期什么效果
       │     ├── BodyPart   ──────── 挂在哪个部位（可为空 = 全身性）
       │     └── severity            严重度（0→1+），决定处于哪个 stage
       ├── BodyPart 树 ─────────── BodyPartDef 定义：躯干/手臂/心脏…
       └── 感染路径 InfectionPathwayDef（3 个：伤口/呼吸道/胃肠道）
DamageDef ────────────────────── 伤害类型：锐/钝/烧/毒/EMP/心灵…（52 种）
HediffGiverSetDef ──────────────  Hediff 的来源集合（谁给什么病）
```

### 1.2 HediffDef 的核心字段

| 字段 | 出现率 | 说明 |
|---|---:|---|
| `hediffClass` | 45% | **C# 类**：`Hediff_Alcohol` / `Hediff_Addiction` / `Hediff_Injury` 等，决定行为 |
| `ParentName` | 59% | 191/324 有父类（`DrugToleranceBase`、`AddictionBase`…） |
| `stages` | 多数 | **分期数组**，每期一个 stage，见 §2.1 |
| `initialSeverity` / `maxSeverity` | 15% / 14% | 初始与最大严重度 |
| `lethalSeverity` | 7% | 到达即致死（如重伤） |
| `isBad` | 23% | 是否「负面」状态，影响心情与 AI |
| `labelNoun` | 36% | 名词形式标签（`a hangover`），用于文案 |
| `everCurableByItem` | 10% | 是否能被药品治愈 |
| `tendable` | 6% | 是否需要「治疗」操作（绷带/手术） |
| `defaultLabelColor` | 13% | UI 显示颜色 |

**Hediff 的三大来源分类**（324 条按性质）：

| 类别 | 代表 | hediffClass |
|---|---|---|
| **疾病/状态** | 感冒、醉酒、亢奋、耐药、瘾症 | `Hediff_Alcohol` `Hediff_Addiction` 等 |
| **伤口（Injury）** | 割伤、枪伤、烧伤、骨折 | `Hediff_Injury` |
| **能力损伤** | 失明、截肢、耳聋、器官衰竭 | `Hediff_Injury` + `injuryProps` |

### 1.3 HediffComp —— 行为挂在 Comp 上

Hediff 的动态行为通过 `<comps>` 挂 `HediffComp`，这是 1.6 的关键结构：

| Comp | 作用 | 出现 |
|---|---|---|
| `HediffCompProperties_SeverityPerDay` | 严重度随天数变化（`severityPerDay`，如戒断 −1/天） | 36 |
| `HediffCompProperties_DisappearsAfterDeath` | 死亡后消失 | 23 |
| `comps.showRemainingTime` | UI 显示剩余时间 | 48 |
| `comps.disappearsAfterTicks` | 到期消失（数值可为区间 `45000~120000`） | 37 |
| `HediffCompProperties_SeverityFromEntropy` | 严重度随「熵」变化（灵能/Xenogerm） | 23 |

> 💡 **「自然消退」靠 `disappearsAfterTicks`，「慢性恶化」靠 `severityPerDay`**，
> 「治疗」靠 `tendable` + 治疗台——三条不同的路径，别混用。

### 1.4 伤口的 `injuryProps`

伤口有专门的一套属性：

| 字段 | 说明 | 例 |
|---|---|---|
| `injuryProps.destroyedLabel` | 部位被摧毁的标签 | `Destroyed` / `Burned off` / `Burned off (chemical)` |
| `injuryProps.averagePainPerSeverityPermanent` | 每点严重度的永久疼痛 | 0.00625 / 0.075 |
| `injuryProps.boneRepairWork` / `tendable` | 修复工作量 | |

伤口愈合后会留 **永久伤疤**（`BodyPartDef.canScarify`），疤痕影响美感
（`BodyPartDef.beautyRelated`）——这与 Ideology「疤痕」戒律、
`Scarification`（刻印仪式）联动（见 [ideology.md](ideology.md) §3.2）。

### 1.5 能力损伤：PawnCapacity

Hediff 通过 `capMods` 修改 **PawnCapacity**（身体能力）。本地出现的容量：

| 容量 | 含义 |
|---|---|
| `Consciousness` | 意识（昏迷/脑损） |
| `Moving` | 移动（骨折/截肢） |
| `Manipulation` | 操作（手部失能） |
| `Sight` | 视力（失明/眼伤） |
| `Breathing` | 呼吸（肺伤） |
| `BloodFiltration` | 血液过滤（肾/肝） |
| `BloodPumping` | 血液循环（心） |
| `Hearing` | 听力（耳聋） |
| `Talking` | 说话（口/喉） |

🔑 **三种修改方式**（`capMods`）：

```xml
<capMods>
  <li>
    <capacity>Moving</capacity>
    <offset>-0.20</offset>      <!-- 加算：这个阶段移动力额外 -20% -->
  </li>
  <li>
    <capacity>Consciousness</capacity>
    <postFactor>0.8</postFactor> <!-- 乘算：意识 ×0.8 -->
  </li>
</capMods>
```

`offset`（加算）与 `postFactor`（乘算）的叠加顺序是有讲究的——
乘算会放大/缩小已有加成，通常加算表示「额外负担」，乘算表示「整体削弱」。

### 1.6 身体部位 BodyPartDef

141 个部位，**Core 88**（人体 + 动物 + 机械体）。

| 字段 | 出现率 | 说明 |
|---|---:|---|
| `hitPoints` | 96% | 部位血量，被打空即摧毁 |
| `bleedRate` | 47% | 该部位的流血速率 |
| `skinCovered` | 70% | 是否有皮肤（决定护甲是否生效） |
| `solid` | 41% | 是否实心（决定能否穿甲） |
| `tags` | 60%+ | **功能标签**，见下 |
| `frostbiteVulnerability` | 29% | 冻伤易感度（手脚最怕冷） |
| `permanentInjuryChance` | 50% | 永久伤残概率（`9999999` = 永不） |
| `executionPartPriority` | 8% | 处决时优先打的部位 |
| `beautyRelated` | 4% | 是否影响美感（疤痕） |
| `delicate` | 2% | 脆弱（重创直接摧毁） |

**功能标签 `tags`** 是身体部位的关键——器官靠标签发挥功能：

| 标签 | 例子 | 作用 |
|---|---|---|
| `SightSource` | 眼 | 视觉来源 |
| `HearingSource` | 耳 | 听觉来源 |
| `BreathingSource` | 肺 | 呼吸 |
| `TalkingSource` / `TalkingPathway` | 口/喉 | 说话 |
| `BloodFiltrationSource` | 肾/肝 | 过滤毒素 |
| `MovingLimbCore` | 腿/臂核心 | 移动基础 |
| `ManipulationLimbCore` | 手 | 操作基础 |
| `MetabolismSource` | 胃 | 代谢/消化 |
| `EatingSource` | 口 | 进食 |
| `spawnThingOnRemoved` | — | 摧毁时掉落（`Heart`/`Lung`/`Kidney`） |

> 💡 **「打中心脏/肺」之所以致命，是因为这些部位带功能标签且 `delicate`**，
> 被摧毁即 `lethalSeverity`。处决（`ExecutionCut`）优先打 `executionPartPriority` 高的部位。

### 1.7 伤害类型 DamageDef

52 种伤害，`ParentName` 构成继承树：

```
Flame（烧伤）──┬─ Burn（灼伤）
              ├─ AcidBurn（酸蚀）
              └─ ElectricalBurn（电击灼伤）
Bullet（子弹伤）
Arrow（箭伤）── Nerve（神经毒箭）
BluntBase ──┬─ Blunt（钝伤）── Demolish（拆墙）
           └─ Poke（戳）
CutBase ──── Cut（割伤）── ExecutionCut（处决）
StunBase ──── EMP（电磁脉冲）── MechBandShockwave
Vaporize ──── NociosphereVaporize
```

核心分类：
- **武器类**：`Cut` `Stab` `Blunt` `Poke` `Bullet` `Arrow` `Bite`（近战/远程武器，见 [gear.md](gear.md)）
- **环境类**：`Flame` `Frostbite`（环境伤害）
- **控制类**：`EMP` `NerveStun`（眩晕/EMP）
- **特殊**：`Psychic`（Anomaly 心灵）、`Decayed`（器官衰竭）、
  `DeadlifeDust`（Anomaly 死灵尘）、`Deterioration`（老化）
- **生化**：`ToxGas` `BulletToxic`（Biotech）

每种伤害决定它打哪个能力、用哪套护甲（`ArmorRating_Sharp`/`Blunt`/`Heat`），
与 [gear.md](gear.md) §1.4 直接对应。

### 1.8 感染路径 InfectionPathwayDef（3 个）

疾病进入身体的途径，决定**从哪个部位/多快**发作：

| 途径 | 对应 |
|---|---|
| 伤口 | 通过 `Cut`/`Bullet` 等伤害附着 |
| 呼吸道 | 吸入（毒气/疾病） |
| 胃肠道 | 食用（食物中毒） |

配合 `HediffCompProperties_DisappearsOnDeath` 等 Comp 决定疾病是否随宿主死亡消失。

---

## 2. 层级

### 2.1 stages —— 一个 Hediff 的分期机制

Hediff 最核心的层级是 `stages`：**同一个病按严重度切成若干期**，每期有独立效果。

```xml
<HediffDef>
  <defName>AlcoholHigh</defName>
  <initialSeverity>0.5</initialSeverity>
  <maxSeverity>1</maxSeverity>
  <stages>
    <li>
      <label>warm</label>
      <minSeverity>0</minSeverity>
      <capMods>
        <li><capacity>Moving</capacity><offset>-0.02</offset></li>
      </capMods>
      <painOffset>-0.2</painOffset>          <!-- 这一期反而降低痛觉 -->
    </li>
    <li>
      <minSeverity>0.25</minSeverity>
      <label>drunk</label>
      <capMods>
        <li><capacity>Consciousness</capacity><postFactor>0.8</postFactor></li>
      </capMods>
    </li>
    <li>
      <minSeverity>0.6</minSeverity>
      <label>hammered</label>
      <!-- 更严重的阶段 -->
    </li>
  </stages>
</HediffDef>
```

- 每个 stage 有 **`minSeverity`**（进入该期的严重度下限）
- 严重度上升会**自动推进到更高期**，下降会回退
- 每期可独立设 `capMods` / `painOffset` / `label` / `becomeVisible`
- `stages` 最多见到 5 期（本地统计到 `li.4`）

🔑 **这是 mod 做「渐进式疾病」的标准手法**：定义几档严重度，每档不同的能力惩罚，
比一个固定效果复杂得多。醉酒、戒断、伤口愈合（越来越痛→愈合）都用这套。

### 2.2 能力损伤的层级：CapMod → PawnCapacity

```
Hediff（病/伤）
  └─ stages（分期）
       └─ capMods（能力修改）
            └─ PawnCapacity（Consciousness/Moving/Sight/…）
                 └─ 派生能力（技能效率、能否工作、能否战斗）
```

一个部位受损 → 改能力（如 `Sight` ×0.5）→ 视野变窄、不能射击。
能力再影响技能（`Manipulation` 低则不能做精细工作）。

### 2.3 品质药品 / 耐药 / 成瘾的层级

这是 Hediff 里一个成环的子系统：

```
成瘾 AddictionBase ──┐
  │                 │ 通过 HediffComp 关联
  ├── 耐受 Hediff（DrugToleranceBase）：喝得更多才有效果
  ├── 戒断 Hediff：停用后 severityPerDay 变化，出现戒断症状
  └── 给药 Hediff（AlcoholHigh 等）：当前药效
```

`ref/content-blueprints.md` 的 Custom Drug 蓝本演示的就是给这个环加一味新药。

---

## 3. 内容

### 3.1 Hediff 全表分类（324 条）

规模分布：**Core 127 · Anomaly 72 · Royalty 45 · Biotech 45 · Ideology 22 · Odyssey 13**。
官方中文标签 324/324 全有。

> 完整 324 行清单（含每条的继承链、hediffClass、每期效果）：
> `python tools/game_defs/extract_defs.py --def HediffDef`

按性质分组（代表性，完整表见脚本）：

| 分组 | 代表 defName | 说明 |
|---|---|---|
| **酒精/兴奋剂** | `AlcoholHigh` `Hangover` `AlcoholTolerance` `GoJuiceHigh` | 4 个 Hediff 组成一个药物环 |
| **瘾症与耐受** | `*_Addiction` `*Tolerance` | `AddictionBase` / `DrugToleranceBase` 子类 |
| **感染与疾病** | `Flu` `Plague` `Malaria` `Pneumonia` 等 | 走 `InfectionPathwayDef` |
| **伤口（模板）** | `*_Injury` 系列 | `injuryProps` 系列，按 DamageDef 派生 |
| **能力损伤** | 失明、耳聋、器官衰竭 | 挂对应 BodyPart |
| **植入/义体** | `AddedBodyPartBase` 子类 | Archotech 肢体（`descriptionHyperlinks` 指向 Archotech 部件） |
| **增益** | 一些 `isBad=false` 的正面状态 | 影响心情 |
| **Ideology 特有** | 22 条（`Ideology` DLC） | 与戒律、纹身、疤痕联动 |
| **Xenogerm** | Biotech 45 条 | 基因改造相关 Hediff |

> 💡 **`addedPartProps`**（41 条带 `solid`）用于「加装部位」——装上义肢后新增一个 BodyPart。
> **`spawnThingOnRemoved`**（59 条）控制部位/Hediff 被移除时掉落什么（Archotech 部件、心脏等）。

### 3.2 身体部位全表（141 个）

规模：**Core 88 · Anomaly 25 · Biotech 17 · Odyssey 11**。

按种族/身体分组：
- **人类**：躯干（Torso）+ 头（Neck/UpperHead/FullHead）+ 四肢（Arms/Legs/ Hands/Feet）+ 器官（Heart/Lung/Kidney/眼睛/耳…）
- **动物**（各物种）：特殊部位（如 `MechanicalTermiteBodyFirstRing` 白蚁环节体、`ClawBase` 爪）
- **机械体**：金属部位，`destroyableByDamage=false`（除非特定伤害）
- **Anomaly / Biotech**：特殊存在部位

> 完整 141 行清单：`python tools/game_defs/extract_defs.py --def BodyPartDef`

### 3.3 伤害类型全表（52 种）

见 §1.7 继承树。完整表：
`python tools/game_defs/extract_defs.py --def DamageDef`

---

## 4. 解读

> 本节为机制串联，来自本地 Def 交叉印证 + 灰机 wiki。

### 4.1 从「一次攻击」到「一条 Hediff」的完整链路

```
武器 tools（近战）/ verbs + projectile（远程）
  └─ damageType → DamageDef（如 Cut / Bullet）
       └─ 打到哪个 BodyPart（命中判定，考虑护甲 ArmorRating）
            └─ 在该部位创建 Hediff（injuryProps，决定是哪种伤）
                 └─ Hediff 按 severity 走 stages
                      └─ 每期 capMods 改 PawnCapacity
                           └─ 派生：视野/移动/技能/能否工作
```

🔑 **护甲在这一步生效**：`ArmorRating_Sharp` 挡 `Cut`，`ArmorRating_Blunt` 挡 `Blunt`，
`ArmorRating_Heat` 挡 `Flame`。护甲减少传到部位的伤害量，从而降低 Hediff 的初始严重度
（见 [gear.md](gear.md) §4.2）。

### 4.2 愈合与永久伤疤

- 伤口有 `severityPerDay`（自然愈合速度）+ `averagePainPerSeverityPermanent`
- 部分伤口 `tendable`（需治疗台/绷带）
- 完全愈合后，在部位上留**伤疤 Hediff**，影响美感（`beautyRelated`）
- 疤痕与 Ideology 联动：
  - `Scarification`（刻印/疤痕）戒律——某些文化要求疤痕
  - `Scarification_Minor/Major` 影响心情
  - `requiredScars` 字段（刻印仪式需要 N 道疤）

### 4.3 器官与致命性

带功能标签的器官（`Heart`/`Lung`/`Kidney`/`Brain`）被摧毁 → 对应能力归零或直接致死。
`executionPartPriority` 决定处决优先打哪（心脏/大脑最高）。灰机对「处决」机制有专页。

### 4.4 感染的三条路径

疾病通过**伤口 / 呼吸道 / 胃肠道**进入人体：
- 伤口感染：战斗割伤后有机会感染
- 呼吸道：戴防毒面具/呼吸器可挡（`SmokeArmor` 类装备）
- 胃肠道：食物/水源

感染后 `InfectionPathway` 决定初始严重度和发作速度；`severityPerDay` 决定恶化还是自愈。

### 4.5 抗性与免疫

Hediff 可通过以下方式被压下去：
- 药品（`everCurableByItem` 的 Hediff）
- 治疗台（`tendable`）
- 抗生素类 Hediff（免疫标记）
- 基因（Biotech 的抗病基因，见 [ideology.md](ideology.md) 提到的 Biotech Hediff）

### 4.6 与心情/需求的联动

Hediff 通过 `isBad` 和 `ThoughtDef` 影响心情：
- 疼痛（`painOffset`）本身扣心情
- 重病、残疾（截肢）扣心情，触发 `ThoughtDef` 如「我失去了肢体」
- Ideology 的某些戒律会根据 Hediff（如食人后得食人肉 Hediff）产生心情奖惩

---

## 5. modder 常见坑

1. **Hediff 挂在 `BodyPart` 上**，全身性 Hediff 才留空。位置错了会显示在错误部位。
2. **`stages` 是数组，靠 `minSeverity` 分期**。不设 `minSeverity` 的 stage 默认从 0 开始——
   多期疾病**必须**给每期设 `minSeverity`，否则全都挤在第一期。
3. **`offset`（加算）和 `postFactor`（乘算）不是一回事**。乘算会放大已有值，
   混用会让数值爆炸。减能力通常用 `offset`，整体削弱用 `postFactor`。
4. **部位功能靠 `tags`**（`SightSource` 等）。自定义部位不给对标签，它就只是个摆设，不参与功能。
5. **`frostbiteVulnerability` 高 = 更容易冻伤**（手脚），改这个做气候敏感 mod。
6. **伤害是继承树**。`Cut`/`Stab`/`Blunt` 都继承 `CutBase`/`BluntBase`，
   改基类会影响所有派生伤害。做新伤害建议继承最接近的现有类型。
7. **`isBad` 影响心情和 AI 态度**。增益型 Hediff（治疗效果）设 `isBad=false`。
8. **存档兼容**：自定义 Hediff 加了新字段，老存档读不出时，
   用 `IExposable` + Scribe 的默认回退（见
   [../game-and-world-components.md](../game-and-world-components.md)）。
