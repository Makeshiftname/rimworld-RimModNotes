# 派系与关系（Faction / Goodwill / PawnRelation / Trade）

> **mech 层**：本文只讲**派系-关系系统由什么构成、字段含义、内容清单**。
> 「外交/贸易请求的工作流程」见 [work.md](work.md)（贸易商进出的 WorkGiver）；
> 「Ideaology 如何决定派系敌意」见 [ideology.md](ideology.md)。
>
> **数据源与核对日期（2026-10-16）**：
> 本地 `F:\Games\GBL\rimworld\Data\`（RimWorld 1.6，全 6 DLC），中文取官方简中 DefInjected。
> 复现：`python tools/game_defs/extract_defs.py --def FactionDef GoodwillSituationDef PawnRelationDef TraderKindDef SitePartDef`。
>
> 规模：**FactionDef 31 · GoodwillSituationDef 39 · PawnRelationDef 28 · TraderKindDef 20 · SitePartDef 57**。

---

## 1. 框架

### 1.1 四个层次的关系

```
FactionDef（31 个派系）── 阵营：玩家/部落/外来者/海盗/虫族/机械体/帝国…
  ├─ techLevel ──────── 科技水平（决定他们用什么）
  ├─ requiredMemes ──── 必带的 Ideology 模因（如霸权主义海盗）
  ├─ pawnGroupMakers ── 这个派系能刷出什么队伍
  ├─ backstoryFilters ── 这个派系的 pawn 有什么背景
  └─ 可以有 Ideo（文化）── 决定它对其他派系的态度

GoodwillSituationDef（39 种好感处境）── 派系间好感的「修改器」
  └─ naturalGoodwillOffset（自然好感偏移）
       └─ meme + otherMeme（文化模因对比）── Ideaology 的敌意大半来自这

PawnRelationDef（28 种人际/血缘关系）── pawn 与 pawn 的关系
  └─ 父母/子女/兄弟姐妹/恋人/朋友…

TraderKindDef（20 种商人）── 谁来做哪类贸易
SitePartDef（57 种据点部件）── 派系在世界地图上的据点构成
```

🔑 **核心区分**：派系关系用 **Goodwill**（派系 vs 派系），
人际关系用 **PawnRelation**（pawn vs pawn）。两者独立。

### 1.2 派系的类继承

31 个派系**全部有 `ParentName`**（100%），继承出 9 个基类：

| 基类 | 派系 |
|---|---|
| `FactionBase` | 通用基类（机械主巢、虫巢、海盗团…） |
| `PlayerFactionBase` | 玩家派系基类（新移民 / 新部落 / 科研探险队 / 逆重飞船机组） |
| `TribeBase` / `TribeRoughBase` / `TribeSavageBase` | 部落（温和/彪悍/野蛮） |
| `OutlanderFactionBase` / `OutlanderRoughBase` | 外来者（文明/粗野） |
| `AncientsBase` | 古代人（中立/敌对） |
| `PirateBandBase` | 海盗团 |

🔑 **想改所有部落的行为→改 `TribeBase`；只改一个部落→改它的子类**。

### 1.3 派系的关键字段

| 字段 | 出现率 | 说明 |
|---|---:|---|
| `techLevel` | 52% | 科技水平（`Ultra` / `Spacer` / `Industrial`…） |
| `requiredMemes` | 26% | **必带的 Ideology 模因**（如海盗必带霸权主义） |
| `disallowedMemes` | 29% | 排除某些模因 |
| `allowedCultures` | 29% | 允许采用的文化（NPC 派系） |
| `canStageAttacks` / `canSiege` | 29%/26% | 能否发动袭击/围攻 |
| `hidden` | 29% | 隐藏派系（不显示在世界地图，如机械主巢） |
| `allowedArrivalTemperatureRange` | 32% | 可到达温度范围（决定能否在某地生成） |
| `maxPawnCostPerTotalPointsCurve` | 26% | 队伍点数→pawn 上限曲线（控制袭击规模） |
| `xenotypeSet.xenotypeChances` | 26% | 各异种人出现概率（Biotech） |
| `requiredCountAtGameStart` | 48% | 开局必须存在的数量 |
| `maxConfigurableAtWorldCreation` | 42% | 世界创建时最多配几个 |

🔑 **`requiredMemes` 是 Ideology 的钩子**——海盗天生带「霸权主义」，
野蛮部落带「劫掠成性」。这直接决定它们的行为风格（见 [ideology.md](ideology.md)）。

### 1.4 派系的队伍生成 `pawnGroupMakers`

派系在袭击/贸易/世界事件时**刷出什么队伍**，由 `pawnGroupMakers` 决定：

```xml
<pawnGroupMakers>
  <li>
    <kindDef>Combat</kindDef>       <!-- 队伍类型 -->
    <commonality>100</commonality>  <!-- 权重 -->
  </li>
  <li>
    <kindDef>Trader</kindDef>       <!-- 商人队 -->
    <commonality>80</commonality>
  </li>
</pawnGroupMakers>
```

`kindDef` 类型（本地可见）：`Combat`（战斗）· `Trader`（贸易）· `Settlement`（定居）·
`Peaceful`（和平）· `Miners`（矿工）· `Fleshbeasts`（血肉兽）· `Shamblers`（游荡者）·
`PsychicRitualSiege`（灵能仪式围攻）等。

### 1.5 好感处境 GoodwillSituationDef（39 种）

🔑 **39 种里有 36 种来自 Ideology**（Core 只有 3 种）——
这说明**派系间的敌意/好感大半由文化模因对比决定**：

| 字段 | 出现率 | 说明 |
|---|---:|---|
| `naturalGoodwillOffset` | 92% | 基础好感偏移（+10 到 −20） |
| `meme` | 90% | 触发条件：我的派系有此模因 |
| `otherMeme` | 79% | 对方派系有此模因 |
| `versusAll` | 10% | 对所有派系都生效 |
| `workerClass` | 100% | 判定逻辑类（`GoodwillSituationWorker_*`） |

**核心 3 种（Core）**：`PermanentEnemy`（永久敌对）· `NaturalEnemy`（天生敌人）·
`AttackingSettlement`（正在袭击）。

**Ideaology 的 36 种**举例——文化冲突决定敌意：
- 「食人」vs「食人」= 友好；「食人」vs「不吃人」= 敌对
- 「人类至上」vs「技术融合」= 敌对（一个反基因改造一个亲基因改造）
- 「动物平权」vs「宰杀动物」相关 = 冲突

> 💡 这就是为什么 **Ideology 让世界地图上的派系关系变得动态**——
> 你改变自己的文化，会连带改变与其他派系的敌友关系。

### 1.6 人际关系 PawnRelationDef（28 种）

| 字段 | 出现率 | 说明 |
|---|---:|---|
| `importance` | 100% | 关系重要度（如 Parent 195 > Sibling 185） |
| `opinionOffset` | 96% | 初始好感偏移（+30 / +20） |
| `workerClass` | 93% | 判定类（`PawnRelationWorker_Parent`） |
| `implied` | 100% | 是否隐含关系（Parent 蕴含 Child） |
| `diedThought` | 75% | 亲人死亡时的想法（`MyFatherDied`） |
| `killedThought` | 75% | 被杀死的想法（`KilledMyFather`） |
| `lostThought` | 75% | 失联的想法（`MyFatherLost`） |
| `labelFemale` | 71% | 女性称谓（father→mother） |
| `romanceChanceFactor` | 61% | 发展成恋爱的概率 |
| `familyByBloodRelation` | 61% | 是否血缘 |
| `inbredChanceOnChild` | 57% | 生孩子时的近亲概率 |
| `soldThoughts` | 50% | 被卖掉的创伤（`SoldMyLovedOne`） |

🔑 **`diedThought`/`killedThought`/`lostThought` 是关系与心情的接口**——
亲人死亡触发对应 `ThoughtDef`（见 [needs-mood.md](needs-mood.md)），
这是「失去亲人」心情大扣的来源。

**28 种关系分类**：
- 血缘：`Parent` `Child` `Sibling`（+ 各种 ex 前任）
- 恋爱：`Lover` `Ex-Lover` `Ex-Spouse`
- 社交：`Friend` `Rival`
- 身份：`Bonded`（宠物/驯兽）`Patient`（医患）`Coworker` `Ghoul`（Anomaly）

### 1.7 商人 TraderKindDef（20 种）

定义谁来贸易、卖什么。20 种涵盖各派系（海盗商人、外来者商人、部落商人、
帝国商人、虚空商人等）。

> 复现：`python tools/game_defs/extract_defs.py --def TraderKindDef`

### 1.8 据点部件 SitePartDef（57 种）

定义派系据点在世界地图上的**构成**（一处据点由哪些 SitePart 拼成——
城镇/营地/防御工事等）。Core/Royalty/Anomaly 等各占一部分。

> 复现：`python tools/game_defs/extract_defs.py --def SitePartDef`

---

## 2. 层级

### 2.1 好感的两层（基础 + 处境）

```
派系基础好感（关系天然值）
  └─ + GoodwillSituation 偏移（每个满足条件的处境叠加）
       └─ 条件：我的 meme（+ 对方 otherMeme）
            └─ 综合 → 实际好感 → 决定外交态度（盟友/中立/敌对）
```

🔑 **Ideaology 的敌意是「处境」叠加出来的，不是硬编码**——
两个派系的敌对程度由它们各自的文化模因对比动态决定。

### 2.2 关系的双向性与蕴含

```
Parent ⇄ Child（双向，Parent 蕴含 Child）
  └─ importance 决定排序（谁更重要）
       └─ died/killed/lost Thought 触发心情反应
            └─ 关系深 → 心情影响越大
```

`implied=true` 表示**隐含**（有 Parent 自动有 Child 关系）。

### 2.3 派系 → 文化 → 敌意 的链

```
FactionDef.requiredMemes（派系天生带的模因）
  └─ 派系的 Ideo（文化）
       └─ 与其他派系的 meme 对比
            └─ GoodwillSituation（Ideaology 的 36 种）
                 └─ 累积好感偏移 → 敌友关系
```

所以「让派系互相敌对」在 Ideaology 下**不需要改 FactionDef，改模因组合即可**。

### 2.4 关系与心情的联动

```
PawnRelation（重要度 importance）
  └─ 触发 died/killed/lost Thought（见 needs-mood.md）
       └─ 心情大扣（如 Parent 死亡 −大幅）
            └─ 长期负面 → 精神崩溃
```

---

## 3. 内容

### 3.1 派系全表（31 个）

官方中文标签 31/31 全有。按类型分组：

**玩家派系**：`PlayerColony` 新移民 · `PlayerTribe` 新部落 ·
`ResearchExpedition` 科研探险队（Anomaly）· `GravshipCrew` 逆重飞船机组（Odyssey）

**人类派系**：
- 部落：`TribeCivil` 温和的部落 · `TribeRough` 彪悍的部落 · `TribeSavage` 野蛮的部落
- 外来者：`OutlanderCivil` 文明的外来者联盟 · `OutlanderRough` 粗野的外来者联盟
- 海盗：`Pirate` 海盗团
- 特殊：`Beggars` 行乞者 · `Pilgrims` 朝圣者（Ideology）· `Empire` 破碎帝国（Royalty）
- Royalty：`OutlanderRefugee` 外来者难民 · `Empire` 破碎帝国

**非人类派系**：
- `Insect` 虫巢 · `Mechanoid` 机械主巢
- `Ancients` 中立古代人 · `AncientsHostile` 敌对古代人
- `Sanguophages` 隐秘血族（Biotech）· `Entities` 实体 · `HoraxCult` 霍拉克斯教团（Anomaly）
- `Salvagers` 打捞者 · `TradersGuild` 商会（Odyssey）

**Biotech 部落变体**：`TribeRoughNeanderthal` 彪悍的尼人部落 ·
`TribeSavageImpid` 野蛮的炎魔部落 · `OutlanderRoughPig` 粗野的猪猡联盟

**Ideology 变体**：`NudistTribe` 天体部落（裸体）· `TribeCannibal` 食人部落 ·
`CannibalPirate` 食人海盗团

> 完整 31 行：`python tools/game_defs/extract_defs.py --def FactionDef`
> 各派系的队伍/背景/模因：
> `python tools/game_defs/extract_defs.py --def FactionDef --fields requiredMemes techLevel backstoryFilters.li.categories.li`

### 3.2 好感处境全表（39 种）

- **Core 3 种**：`PermanentEnemy` 永久敌对 · `NaturalEnemy` 天生敌人 ·
  `AttackingSettlement` 正在袭击
- **Ideaology 36 种**：文化冲突驱动的敌友关系（模因对比）

> 完整 39 行：`python tools/game_defs/extract_defs.py --def GoodwillSituationDef`

### 3.3 人际关系全表（28 种）

**Core 26 种** + Biotech 2 种。血缘/恋爱/社交/身份四类，见 §1.6。

> 完整 28 行：`python tools/game_defs/extract_defs.py --def PawnRelationDef`

---

## 4. 解读

### 4.1 派系的两层：文化 + 科技

一个派系的行为风格 = **文化（Ideaology）+ 科技水平**：
- `techLevel` 决定他们用什么装备、派什么敌人
- `requiredMemes` + Ideo 决定他们**怎么打、怎么活**（海盗劫掠、部落和平）

两者独立——一个 Spacer 科技的部落仍可以是「温和」文化。

### 4.2 Ideaology 让派系关系动态化

这是 Biotech 之外 Ideaology 最大的影响之一：**36/39 的好感处境由模因驱动**。

后果：
- 你改变自己的文化 → 与某些派系变友好、某些变敌对
- 派系之间可能因为文化相近而结盟（如两个食人部落）
- 「霸权主义」等模因天然导致敌对（见 [ideology.md](ideology.md)）

做「派系外交」mod 时——**优先改模因组合，而不是硬改好感数值**，
这样能和 Ideaology 联动。

### 4.3 血缘关系的 `importance` 决定心情影响

`importance` 越高（如 Parent 195），亲人死亡/killed 的心情惩罚越大。
`diedThought`/`killedThought` 触发的 `ThoughtDef` 就是心情大扣的来源
（见 [needs-mood.md](needs-mood.md) §1.3）。

**做「关系向」mod** 的入口：给某关系调 `importance` 或改 `diedThought`。

### 4.4 派系队伍生成的可控性

`maxPawnCostPerTotalPointsCurve` 是一条曲线——
决定「袭击点数 → 派多少兵」。做难度调整 mod 常改这条曲线。

`pawnGroupMakers.kindDef` + `commonality` 控制各类型队伍出现频率。

### 4.5 贸易是派系驱动的

`TraderKindDef` 定义各派系的商人。派系的 `allowedCultures`/`requiredMemes`
也会影响他们卖什么（Ideaology 影响交易偏好，见 [ideology.md](ideology.md) §4）。

---

## 5. modder 常见坑

1. **派系改基类影响全部子类**——31 个派系全有 `ParentName`。
   改 `TribeBase` 影响所有部落，改具体子类只影响一个。
2. **敌意优先改模因，不是硬改好感**——39 个 GoodwillSituation 里 36 个由
   meme 对比驱动。硬改数值会破坏 Ideaology 联动。
3. **`GoodwillSituationDef` 的 `meme`+`otherMeme` 是双向条件**——
   「我方 meme + 对方 meme」才触发，注意方向。
4. **`diedThought`/`killedThought` 是心情接口**——改关系文案不影响心情，
   心情来自触发的 `ThoughtDef`（见 [needs-mood.md](needs-mood.md)）。
5. **`hidden=true` 的派系不出现在世界地图**（机械主巢、虫巢）——
   想让它们可见要改这个，且要确保有据点生成。
6. **`allowedArrivalTemperatureRange` 限制派系生成地点**——
   寒冷派系不会出现在热带世界。
7. **新增派系必须设 `factionNameMaker` + `settlementNameMaker`**（否则没有随机名字），
   且 `pawnGroupMakers` 要给至少一种队伍。
8. **依赖 DLC 的派系注意**——Biotech 的猪猡/炎魔/血族派系、
   Anomaly 的实体/霍拉克斯都是 DLC 专属。
