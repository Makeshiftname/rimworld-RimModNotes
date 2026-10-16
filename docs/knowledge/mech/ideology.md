# 文化 Ideology（Ideology DLC）

> **mech 层**：本文只讲**这个系统由什么构成、数据模型长什么样、内容有哪些**。
> 「它在什么时候被调用」见 [../ref/csharp-basics.md](../ref/csharp-basics.md)（pawn 生成流程第 17 步会设 Ideo），
> 「怎么写一个能用的 Def」见 [../ref/xml-defs-and-patches.md](../xml-defs-and-patches.md)。
>
> **数据源与核对日期（2026-10-16）**：
> - **本地游戏数据**：`F:\Games\GBL\rimworld\Data\`，RimWorld **1.6**（Unity `2022.3.35f1`），全 6 DLC。
>   中文名与描述取自 `Data/<DLC>/Languages/ChineseSimplified (简体中文).tar` 内的 `DefInjected/`——
>   **官方简中译文**，每条在 XML 注释里带 `<!-- EN: 英文原文 -->`，零翻译风险。
> - **机制解读**：灰机 wiki [文化](https://rimworld.huijiwiki.com/wiki/%E6%96%87%E5%8C%96)
>   与 [戒律](https://rimworld.huijiwiki.com/wiki/%E7%82%AD%E5%BE%8B)（2026-10-15 更新）。
> - **复现方式**：`python tools/game_defs/extract_defs.py --def MemeDef PreceptDef IssueDef ...`
>
> ⚠️ 本文用「模因 / 戒律」这两个中文类别名，源自灰机 wiki；游戏内实例标签则是官方简中译文。
> ⚠️ **Ideology DLC 未加载时**，本系统全部内容不存在，且依赖它的 Def 会报错。

---

## 1. 框架

### 1.1 关键前提：Ideo 本身不是 Def

最反直觉的一点：**Ideo（一个文化）不是 Def**，它是运行期由 C# `Ideo` 类构造的对象。
写在 XML 里的是它的各个**组成零件**，游戏在创建/加载文化时把这些零件装配成一个 `Ideo`。

- 想「整体定义一个文化」→ 用 **`IdeoPresetDef`**（预设）或玩家自定义界面
- 想「单独改一条戒律 / 加一个模因」→ 改 `PreceptDef` / `MemeDef`
- 想「拿到运行期的 Ideo 对象」→ 读 `Pawn.Ideo`

### 1.2 Def 类型总览

| Def 类型 | 实例数 | 中文类别 | 一句话职责 |
|---|---:|---|---|
| `MemeDef` | 36 | 模因 | 文化的**中心基调**，1–4 个 |
| `PreceptDef` | 215 | 戒律 | 对各类事物的**态度**，产生心情奖惩 |
| `IssueDef` | 72 | 议题 | 把戒律**分组**（一个议题下 1–13 条戒律） |
| `IdeoPresetDef` | 25 | 预设文化 | 开局可直接选的整套文化 |
| `IdeoPresetCategoryDef` | 6 | 预设分类 | 预设文化的下拉分组 |
| `MemeGroupDef` | 7 | 模因互斥组 | 决定哪些模因不能共存 |
| `IdeoColorDef` | 33 | 文化色 | 文化代表色 / 禁止色 |
| `IdeoIconDef` | 94 | 文化图标 | 图标装饰 |
| `StyleCategoryDef` | 11 | 风格样式 | 影响家具/建筑/物品外观与仪式音效 |
| `ThingStyleDef` | 123 | 物件样式 | 具体某类物件的样式定义 |
| `StyleItemCategoryDef` | 12 | 样式物品类别 | 样式作用到哪类物品 |
| `TattooDef` | 31 | 纹身 | 文化认可的纹身 |
| `RitualPatternDef` | 27 | 仪式模式 | 仪式的具体形态（流程） |
| `DutyDef` | 108 | 义务 | NPC 派系的文化义务 |
| `IdeoStoryPatternDef` | 6 | 叙事模式 | 文化起源叙述模板 |
| `IdeoFoundationDef` | 1 | 基础 | 强制存在的基石文化 |

字段级细节全部可查本地 Def，例如
`Ideology/Defs/MemeDefs/Memes_Misc.xml`、`Ideology/Defs/PreceptDefs/Precepts_*.xml`。

### 1.3 PreceptDef 的内部结构

一条戒律是「**若干条心情触发器**」的集合，这是它最重要的设计。

```xml
<PreceptDef>
  <defName>Corpses_Ugly</defName>
  <issue>Corpses</issue>              <!-- 归入哪个议题 -->
  <impact>Low</impact>                <!-- 影响档位：Low / Medium / High（只有三档）-->
  <displayOrderInIssue>0</displayOrderInIssue>
  <displayOrderInImpact>-100</displayOrderInImpact>
  <comps>
    <li>
      <thought>ObservedLayingCorpse</thought>      <!-- 触发哪个 ThoughtDef -->
      <eventDef>ObservedLayingCorpse</eventDef>    <!-- 触发哪个 HistoryEventDef -->
      <description>有人埋了尸体</description>
      <points>-1</points>                          <!-- 违反时的想法阶段 -->
      <tooltipShowMoodRange>true</tooltipShowMoodRange>
    </li>
    <li>
      <thought>ObservedLayingRottingCorpse</thought>
      <eventDef>ObservedLayingRottingCorpse</eventDef>
      <gender>Male</gender>                          <!-- 只对某性别生效 -->
    </li>
  </comps>
</PreceptDef>
```

`comps` 的其他能力（字段取自本地 215 条戒律的实际统计）：

| 能力 | 字段 | 用得多的实例数 |
|---|---|---|
| 按性别区分 | `comps.gender`（`Male` / `Female` / `SubordinateGender`） | 10 |
| 阈值分档描述 | `comps.thoughtStageDescriptions` | 9+ |
| 抵消指定特质 | `comps.nullifyingTraits.DrugDesire` | 3 |
| 反向移除想法 | `comps.removesThought` | 3 |
| 自定义事件文案 | `comps.eventLabel` | 3 |
| 点数直接写入 | `comps.points` | 3 |

戒律还能带**属性乘区与角色绑定**，这是它跨系统的地方：

| 字段 | 作用 | 示例 |
|---|---|---|
| `statFactors.<Stat>` | 属性**乘算**修正 | `ResearchSpeed` ×0.25、`TameAnimalChance` ×2 |
| `statOffsets.<Stat>` | 属性**加算**修正 | `WorkSpeedGlobal` −0.08、`FishingYield` −0.10 |
| `preceptClass` | 专用 UI 扩展类 | `Precept_Apparel` / `Precept_Animal` / `Precept_Ritual` |
| `workerClass` | 自动执行的 Worker | `PreceptWorker_Apparel` / `PreceptWorker_Building` |
| `sourcePawnRoleDef` / `sourceAbilityDef` | 由哪个角色/能力产生 | `IdeoRole_Leader` / `ConversionRitual` |
| `grantedAbilities` | 额外授予能力 | `Convert` / `MarksmanCommand` |
| `roleEffects` / `roleDisabledWorkTags` | 角色效果与禁做工作 | 禁用 `Crafting` / `Mining` |

> 🔴 **modder 坑**：想「让某个戒律不再让人破坏」时，**加新的 `PreceptDef`（高影响）通常比改原条目安全**——
> 原条目常被 `ParentName` 继承（215 条里 29 条有父类），改父类会波及所有子条目。
> 用 `<Inherit>False</Inherit>` 可以只覆盖单个实例。

---

## 2. 层级

### 2.1 五层结构

```
Ideo（运行期对象）
│
├─ 结构 Structure ────── MemeDef 里 category=Structure 的 9 个
│                        + 传统 Tradition（探索/铁血/图腾/罪恶/索菲亚）
│
├─ 模因 Meme ──────────── MemeDef，固定模式 1–4 个 / 演进模式开局 1 个
│      │  associatedMemes ──▶ 自动捆绑哪些戒律随之生效
│      │  requireOne ──────▶ 「以下 N 组里至少满足一组」
│      │  grantedAbilities ─▶ 解锁能力
│      │  thingStyleCategories ─▶ 自动附加风格样式（带 priority）
│      │  consumableBuildings / requiredRituals / addDesignators
│      ▼
├─ 戒律 Precept ────────── PreceptDef（215），按 IssueDef（72）分组
│      │                     每条有 impact：Low / Medium / High
│      │  comps ──────────▶ ThoughtDef + HistoryEventDef（心情的实际来源）
│      ▼
├─ 角色 Role ───────────── 🔴 代码生成，不在 Def 里（见 §2.4）
│      领袖 Leader（唯一）· 传道者 Proselytizer（唯一）· 8 种专家
│
└─ 表现层（互不依赖，各自独立）
       风格样式 StyleCategory（≤3 种）──▶ ThingStyleDef ──▶ 具体物件
       仪式 RitualPatternDef · 圣物 Relic · 武器推崇/鄙夷
       圣兽 · 推崇异种 · 偏好服装 · 代表色
```

### 2.2 模因 → 戒律的绑定

模因**本身几乎不直接改数值**，它是通过捆绑戒律来影响玩法的。三种绑定字段：

| 字段 | 方向 | 出现数 | 含义 |
|---|---|---:|---|
| `associatedMemes` | Precept → Meme | 87/215 | 「我属于哪些模因」——**这条戒律随该模因自动进入文化** |
| `requiredMemes` | Meme ↔ Precept | 42/215 | 该戒律**要求**文化拥有某些模因才可用 |
| `conflictingMemes` | Meme ↔ Precept | 41/215 | 与某些模因**互斥** |
| `requireOne` | Meme 内部 | 22/36 | 模因自带的**条件组**（原版预制多条包装好的戒律） |

`MemeDef.requireOne` 是个值得注意的设计——原版把整套复杂戒律打包成组：

```xml
<requireOne>
  <li><li>Slavery_Acceptable</li><li>Charity_Essential</li><li>WorkDrive_Tripled</li></li>
  <li><li>Execution_Required</li><li>Pain_Idealized</li></li>
</requireOne>
```

`requireOne` 里的**每个顶层 `<li>` 是一个「或」组**，组内是「且」。

### 2.3 议题（Issue）→ 戒律 → 影响

`IssueDef` 是纯分组容器，作用是让 215 条戒律在设置界面里可读：

| 量 | 值 |
|---|---|
| 议题数 | **72**（Core 11 · Ideology 54 · Biotech 4 · Anomaly 3 · Odyssey 3 · Royalty 2） |
| 每个议题的戒律数 | 1–13 条（中位数 2–3） |
| 戒律最多的议题 | `Ritual` 仪式 13 · `Nudity_Female`/`Nudity_Male` 各 9 · `Cannibalism` 食人 8 · `Lovin` 肉欲 7 · `Execution` 处决 7 · `Research` 科研 7 · `IdeoDiversity` 多元文化 7 |
| 影响档位 | **只有三档**：`Low` / `Medium` / `High` |

排序由两个字段控制：`displayOrderInIssue`（议题内）与 `displayOrderInImpact`（按影响分组内）。

### 2.4 🔴 角色层是代码生成的

XML 里**没有任何 `IdeoRoleDef`**，也没有 `IdeoRole_Leader` / `IdeoRole_Moralist` 的 `PawnKindDef`——
全库搜索只在三处**引用**它们（`Precepts_Role.xml`、`Precepts_Ritual.xml`、`Ritual_Behaviors.xml`）。

角色 PawnKind 由 C# 在文化创建时**运行期生成**。这带来两个直接后果：

- **不能用 `<PatchOperationFindMod>` 之类去 patch `IdeoRole_Leader`**——它不在 Def 数据库里
- 要改角色效果，走 `PreceptDef` 上的 `roleEffects` / `roleApparelRequirements` / `roleDisabledWorkTags`，
  或用 Harmony 改 `IdeoRoleDef` 类的字段

---

## 3. 内容

### 3.1 模因全表（36 个）

按性质分四组。`Structure_*` 系列的 `category` 是 `Structure`，属**结构**而非普通模因。

**① 核心模因（Ideology DLC，21 个）**

| defName | 中文 | 归属源文件 |
|---|---|---|
| `MaleSupremacy` | 男权至上 | `Memes_GenderSupremacy.xml` |
| `FemaleSupremacy` | 女权至上 | `Memes_GenderSupremacy.xml` |
| `Supremacist` | 霸权主义 | `Memes_GroupRelation.xml` |
| `Collectivist` | 集体主义 | `Memes_Individualism.xml` |
| `Individualist` | 个人主义 | `Memes_Individualism.xml` |
| `Loyalist` | 赤胆忠心 | `Memes_GroupRelation.xml` |
| `Guilty` | 原罪主义 | `Memes_GroupRelation.xml` |
| `HumanPrimacy` | 人类至上 | `Memes_Primacy.xml` |
| `NaturePrimacy` | 自然至上 | `Memes_Primacy.xml` |
| `Cannibal` | 食人狂热 | `Memes_Misc.xml` |
| `FleshPurity` | 血肉纯洁 | `Memes_Misc.xml` |
| `Transhumanist` | 技术融合 | `Memes_Misc.xml` |
| `PainIsVirtue` | 苦痛美德 | `Memes_Misc.xml` |
| `HighLife` | 化学极乐 | `Memes_Misc.xml` |
| `Nudism` | 裸体主义 | `Memes_Misc.xml` |
| `Tunneler` | 地底之心 | `Memes_Misc.xml` |
| `TreeConnection` | 灵木共鸣 | `Memes_Misc.xml` |
| `Darkness` | 黑暗亲和 | `Memes_Misc.xml` |
| `AnimalPersonhood` | 动物平权 | `Memes_Misc.xml` |
| `Raider` | 劫掠成性 | `Memes_Misc.xml` |
| `Rancher` | 大牧场主 | `Memes_Misc.xml` |

**② 结构模因（9 个，`category=Structure`）**

| defName | 中文 | 性质 |
|---|---|---|
| `Structure_Animist` | 万物有灵 | 基础结构，附「泛灵文化」样式 |
| `Structure_Archist` | 机械神教 | 基础结构 |
| `Structure_Ideological` | 无神论 | 基础结构，解锁「象征物焚烧仪式」 |
| `Structure_TheistAbstract` | 抽象神论 | 基础结构，可指定神明 |
| `Structure_TheistEmbodied` | 具象神论 | 基础结构，可指定神明 |
| `Structure_OriginBuddhist` | 佛教起源 | 抽象神论分支，解锁「大麻聚会」 |
| `Structure_OriginChristian` | 基督教起源 | 抽象神论分支，**规定「圣诞节」** |
| `Structure_OriginIslamic` | 伊斯兰起源 | 抽象神论分支 |
| `Structure_OriginHindu` | 印度教起源 | 具象神论分支，解锁「大麻聚会」 |

> 这 9 个只影响命名、样式和仪式解锁，**对玩法影响极小**（灰机原话：「与模因/戒律之间也没有任何捆绑关系」）。
> 三个「Origin」分支通过 `ParentName=StructureOriginBase` 继承公共字段。

**③ DLC 新增模因（6 个）**

| defName | 中文 | DLC |
|---|---|---|
| `Inhuman` | 非人异化 | Anomaly |
| `Ritualist` | 仪式热忱 | Anomaly |
| `Bloodfeeding` | 汲血热忱 | Biotech |
| `Shipborn` | 星舟永嗣 | Odyssey |

**④ `MemeGroupDef`（7 个互斥组）**

模因用 `groupDef` 字段加入互斥组，组内只能选一个：

`GroupRelation` · `Individualism` · `Primacy` · `Misc` · `Human` · `Animal` · `AnimalTreatment`

组内互斥通过 `MemeDef.exclusionTags` 实现——例如同时带「霸权主义」和「个人主义」时，
后者会被标记冲突而无法共存。

### 3.2 议题全表（72 个，按 DLC 分）

**Core（11）**：食人 `Cannibalism` · 尸体 `Corpses` · 处决 `Execution` · 虫肉 `InsectMeat` ·
肉欲 `Lovin` · 婚后姓氏 `MarriageName` · 食用营养膏 `NutrientPasteEating` · 使用器官 `OrganUse` ·
仪式 `Ritual` · 奴隶贸易 `SlaveTrading` · 女性的配偶 `SpouseCount_Female` / 男性的配偶 `SpouseCount_Male`

**Ideology（54）**：动物共鸣 `AnimalConnection` · 宰杀动物 `AnimalSlaughter` · 圣兽 `AnimalsVenerated` ·
变节 `Apostasy` · 服装喜好 `ApparelDesire` · 自动炮塔 `AutonomousWeapons` · 生体塑形 `Biosculpting` ·
灵视 `BlindPsysense` · 失明 `Blindness` · 身体改造 `BodyModification` · 牵绊 `Bonding` · 仁善 `Charity` ·
舒适 `Comfort` · 夜战 `DarknessCombat` · 成瘾品 `DrugUse` · 日蚀 `Eclipse` · 菌类 `FungusEating` ·
母树共鸣 `GauranlenConnection` · 建筑 `IdeoBuilding` · 多元文化 `IdeoDiversity` · 圣物 `IdeoRelic` ·
仪式坐席 `IdeoRitualSeat` · 职位 `IdeoRole` · 室内 `Indoors` · 杀戮无辜的动物 `KillingInnocentAnimals` ·
照明 `Lighting` · 肉食 `MeatEating` · 采矿 `Mining` · 矿产 `MiningYield` · 神经超频 `NeuralSupercharge` ·
女性装束 `Nudity_Female` · 男性装束 `Nudity_Male` · 痛苦 `Pain` · 传教 `Proselytizing` ·
掠袭 `Raiding` · 畜牧 `Ranching` · 科研 `Research` · 餐风宿露 `RoughLiving` · 疤痕 `Scarification` ·
枭示 `Skullspike` · 板床 `SlabBed` · 奴隶制 `Slavery` · 睡眠加速 `SleepAccelerator` ·
适温 `Temperature` · 伐木 `TreeCutting` · 树 `Trees` · 武器 `Weapons` · 工作激励 `WorkDrive` ·
年龄逆转 `AgeReversal`

**Biotech（4）**：汲血者 `Bloodfeeders` · 儿童劳工 `ChildLabor` · 培育舱 `GrowthVat` ·
机械族劳工 `MechanoidLabor` · 推崇的异种人 `PreferredXenotypes`

**Anomaly（3）**：非人化 `Inhumanizing` · 心灵仪式 `PsychicRituals` · 虚空调查 `VoidStudy`

**Odyssey（3）**：钓鱼 `Fishing` · 游牧 `Nomadic` · 太空居住 `SpaceHabitat`

**Royalty（2）**：并入 Core 项（`Execution` / `OrganUse` 有 Royalty 增量条目）

### 3.3 风格样式（11 个）

| defName | 中文 | 主要影响的物品（灰机整理） |
|---|---|---|
| `Morbid` | 病态文化 | 病态意象地板/地毯/雕板、斥候·海军·骑士头盔 |
| `Totemic` | 图腾文化 | 图腾地板/雕板/象形地板、斥候·海军·骑士头盔 |
| `Spikecore` | 尖刺文化 | 尖刺地板/金属板、简易·防弹头盔、防弹夹克、风雪大衣 |
| `Rustic` | 民俗文化 | 民俗文化地毯、简易·防弹头盔 |
| `Animalist` | 泛灵文化 | 泛灵文化雕板、战争面具、斥候·海军·骑士头盔 |
| `Techist` | 技术文化 | 六角地毯/地砖、花盆、自动门、物品架 |
| `Buddhist` | 佛学文化 | 小/大/宏伟雕塑 |
| `Christian` | 基督文化 | 小/大/宏伟雕塑 |
| `Islamic` | 伊斯兰文化 | 小/大/宏伟雕塑 |
| `Hindu` | 印度教文化 | 小/大/宏伟雕塑、简易·防弹头盔 |
| `Horaxian` | 霍拉克斯教徒 | Anomaly DLC |

具体到单个物件的样式是 `ThingStyleDef`（123 个），通过 `StyleItemCategoryDef`（12 个）绑定到物件类别。

### 3.4 预设文化（25 个）

对应 `IdeoPresetDef`，可直接在开局选，也可作为写自定义文化的模板。
模因组合来自灰机 [文化](https://rimworld.huijiwiki.com/wiki/%E6%96%87%E5%8C%96) §预设文化表：

| 中文 | defName | 模因组合 |
|---|---|---|
| 类经典 | `ClassicLike` | 无 |
| 集体家庭 | `Unity_Family` | 赤胆忠心 + 集体主义 |
| 纯粹至高 | `Human_Purists` | 人类至上 + 血肉纯洁 |
| 极乐集体 | `High_Collectivists` | 集体主义 + 化学极乐 |
| 自由畜牧 | `Free_Ranchers` | 个人主义 + 大牧场主 |
| 血腥支配者 | `Violent_Dominators` | 劫掠成性 + 霸权主义 |
| 技术乌托邦 | `Techno_Utopians` | 技术融合 + 集体主义 |
| 苦痛行者 | `Painful_Purists` | 苦痛美德 + 血肉纯洁 |
| 暗夜教派 | `ProHuman_Darklovers` | 黑暗亲和 + 人类至上 |
| 自然教派 | `NatureOverMan` | 原罪主义 + 自然至上 |
| 征服者 | `Domination_Gang` | 霸权主义 + 传教热忱 |
| 食人族 | `Cannibals` | 食人狂热 |
| 裸体布道者 | `Nudist_Recruiters` | 裸体主义 + 传教热忱 |
| 犯罪团伙 | `Criminal_Gang` | 劫掠成性 + 个人主义 |
| 阿玛宗掠袭者 | `Feminine_Ravagers` | 女权至上 + 劫掠成性 |
| 赎罪者 | `Martyrs` | 原罪主义 + 苦痛美德 |
| 牛仔 | `Cowboys` | 男权至上 + 大牧场主 + 赤胆忠心 |
| 穴居人 | `Mole_People` | 地底之心 + 黑暗亲和 |
| 拜树教派 | `Tree_Worshippers` | 灵木共鸣 + 自然至上 |
| 盲视教派 | `Blindness_Cult` | 苦痛美德 + 盲眼视界 |
| 野性教派 | `Feral_People` | 动物平权 + 自然至上 |
| 裸体食人族 | `Naked_Maneaters` | 裸体主义 + 食人狂热 |
| 超人类主义 | `Progressive_Humanism` | 技术融合 + 个人主义 + 人类至上 |
| 鲜血崇拜 | `Blood_Worshippers` | 汲血热忱 + 黑暗亲和（Biotech） |
| 虚空信徒 | `VoidFollowers` | 机械神教 + 仪式热忱 + 苦痛美德（Anomaly） |

### 3.5 其他内容层

| 类型 | 数量 | 备注 |
|---|---:|---|
| `TattooDef` | 31 | 文化认可的纹身；`MemeDef.styleItemTags` 用 `weightFactor` 决定该模因对纹身的偏好权重 |
| `RitualPatternDef` | 27 | 仪式形态；`PreceptDef.ritualPatternBase` 可指定某戒律对应的仪式 |
| `DutyDef` | 108 | NPC 派系的文化**义务**（与玩家无关） |
| `IdeoColorDef` | 33 | 文化代表色兼梳妆台预设色 |
| `IdeoIconDef` | 94 | 图标装饰（`IdeoIcon` 显示用） |
| `IdeoStoryPatternDef` | 6 | 文化起源叙述模板 |

---

## 4. 解读

> 本节来自灰机 wiki，用于补本地 Def 数据讲不了的部分。

### 4.1 三种开局模式

| 模式 | 行为 |
|---|---|
| **经典模式** | **完全禁用文化体系**，规则与原版一致；DLC 其他内容（领袖职位、仪式、圣物）仍可用，但传教、风格、推崇全部失效 |
| **固定模式** | 开局定死模因与戒律（**最多 4 个模因**），之后不变 |
| **演进模式** | 开局只能选 **1 个模因**（标「影响：高」的初始不可用），游戏中通过仪式/行为赚**发展点数**，再消耗点数增删模因、戒律、风格 |

`ClassicLike` 预设是「类经典」——你的殖民者按经典规则玩，但**世界其他派系仍有各自的文化**，
和经典模式（全世界同一文化）不同。

### 4.2 认可度与转化

每个人对所属文化有一个**认可度**（灰机称「认可度」，对应 `StatDef` 里的 `Certainty`）。
**降到 0 时自动接受转化发起者的文化**。

```
认可度损失量 = 6% × 转化者的转化能力
                × 传道者系数（转化者是本文化传道者时 ×2）
                × 全局认可度损失系数
                × 圣物加成系数
                × 特性/模因认可度系数
                × 叙事者设定教化补正系数
```

**圣物加成系数**（展台内的圣物数量 → 系数）：

| 圣物数 | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10+ |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 系数 | 1.0 | 1.1 | 1.15 | 1.2 | 1.22 | 1.25 | 1.26 | 1.27 | 1.28 | 1.29 | 1.3 |

相关的三个属性在灰机各有专页：`文化传播系数`（发起教化的几率）、
`全局动摇系数`（= 基础值 × 戒律系数 × 其他系数）、`教化能力`（= 基础值 × 技能系数 × …）。

> 💡 **modder 视角**：想让派系更好被同化 → 加**戒律**影响 `CertaintyLossFactor`
> （本地已见 `statFactors.CertaintyLossFactor` 取值 0.4 / 0.6 / 0.8）。
> 想提高转化效率 → 展台多放圣物，或提高教化者的 `Conversion Power`。

### 4.3 职位

| 职位 | 约束 | 心情代价 |
|---|---|---|
| **领袖** Leader | 同时只有 1 人 | **额外 2 档期望 ≈ −12 心情** |
| **传道者** Proselytizer | 同时只有 1 人 | **额外 2 档期望 ≈ −12 心情** |
| 8 种专家 | **不限人数、无心情代价**，但受模因限制，且**禁用一部分工作** | 无 |

专家职位与模因的绑定（本地 `PreceptDef.roleApparelRequirements` / `sourcePawnRoleDef` 印证）：

| 职位 | 需要的模因 |
|---|---|
| 射击专家 · 格斗专家 | 劫掠成性、霸权主义 |
| 科研专家 | 技术融合 |
| 种植专家 | 化学极乐、自然至上、灵木共鸣 |
| 生产专家 | 人类至上 |
| 采矿专家 | 黑暗亲和、地底之心 |
| 动物专家 | 动物平权、大牧场主 |
| 医学专家 | 血肉纯洁、盲眼视界 |

职位还能强制着装（`roleApparelRequirements`）——**职位服装与偏好服装冲突时只警告不阻止**。

### 4.4 仪式

- 仪式有**四档结果**（类 TRPG 的大成功/成功/失败/大失败）
- 以「任意时间」举办时，**20 天内连续举办会因间隔太短而加成功率惩罚**
- 以「特定日期」举办则被视为**节日**，接近时提前提醒
- 演进模式中，**「成功」与「大成功」会提供发展点数**
- 需要对应仪式建筑的，若选了仪式没选建筑（或反之），界面**红字阻止文化创建**

绑定仪式的集合点：坟墓/棺材（葬礼）· 祭坛/符号图像/仪式举办点（社会节日）· 灯球（劲舞派对）·
点燃的营火（天灯节、太鼓派对）· 圣诞树 · 烟壶/香炉（大麻聚会）· 木雕/柴堆/献祭旗（象征物焚烧）·
全人宴（食人盛宴）。

### 4.5 圣物

圣物是**可展示在圣物展台的特殊物品**，开局可选、演进模式中盘可加。

- 武器类圣物**必定是传奇品质**（若该武器支持品质）
- 圣物在仪式中展示：全体与会者 **+3 心情，持续 4 天**
- **找齐本文化全部圣物**：+15 心情，持续 15 天
- **遗失或损坏一件**：−5 心情，持续 30 天，**最多叠加 5 次**

**寻回圣物**需完成至少 **5 个子任务**才开启最终阶段，子任务四类：

| 子任务 | 机制 |
|---|---|
| 骇入村庄终端机 | 生成中立小村庄；接触终端机 / 逗留超 **10 小时** / 敌对行为 / 在村庄内建造 → 全村转敌对 |
| 骇入无人机数据 | 本图坠落一架无人机；敌对派系每 **8 小时**派队摧毁它并立刻撤离；无人机有 **750 点**骇入进度，骇入完成 **30 秒**后自爆，或坠落 **30 天**后自爆 |
| 远古建筑群 | 附近生成远古遗迹，派远征队骇入其中所有无人机 |
| 隐藏的建筑群 | 友好派系要求派出指定规模调查队，用穿梭机运送；**目的地在出发前不显示** |

集齐 5 个后最终目标出现在附近，圣物供奉在巨大建筑中心，有休眠机械族守卫；
**取走圣物的瞬间全图机械族立刻激活**。

### 4.6 风格优势

户外按**最大 27 格**范围、室内**无视距离**统计风格家具。
某风格类型的优势点数**比所有其他类型高出至少 10 点**时成为该环境/房间的主流风格。

部分点数（完整表见灰机页）：

| 物件 | 点数 | 物件 | 点数 |
|---|---:|---|---|
| 宏伟祭坛 | 50 | 中型祭坛 | 20 |
| 地板（宽·家具类） | 40 | 桌子(2×4) | 20 |
| 宏伟雕塑 | 40 | 符号图像 | 20 |
| 大祭坛 | 40 | 小型祭坛 / 桌子(2×2) | 10 |
| 地板（家具类） | 30 | 长凳 / 香炉 / 柴堆 / 献祭旗 / 全人宴 / 烟壶 | 10 |
| 小雕塑 | 10 | 落地灯 / 火把 / 跪垫 / 跪枕 / 桌子(1×2) | 5 |
| 微光火把 / 微光菌炬 | 5 | 物品架(小) | 2.5 |
| 地板（普通） | 1 | | |

> ⚠️ **覆盖规则会让风格优势归零**：同一文化同时有「病态」和「图腾」时，
> 图腾被病态覆盖，**属于图腾风格的家具大多会被算作「多元化风格」而非统一风格**。
> 多风格混搭时这不是 bug 而是设计。

### 4.7 各项偏好的心情数值

**武器推崇 / 鄙夷**（同一武器类型不能既被冲突又被鄙夷）：

| 情形 | 心情 |
|---|---:|
| 使用推崇武器获胜 | +3 |
| 持有推崇武器 | +3 |
| 使用被鄙夷武器获胜 | −5 |
| 持有被鄙夷武器 | −5 |

原版有 5 种组合：近战/远程 · 近战利器/近战钝器 · 长距射击/短距射击 ·
远程重武器/远程轻武器 · 石器时代/极致时代。

**圣兽**（动物被规定为尊崇对象后禁止狩猎与宰杀）：

| 情形 | 心情 |
|---|---:|
| 驯服的圣兽死亡 | −5 |
| 吃了圣兽的肉 | −8 |
| 罕有 → 随处可见圣兽（规模 1→6） | +1 → +5 |

**偏好服装**：意见「宽松」+1、「深刻」+2。⚠️ **所有装甲衣物都不能被设为偏好服装**。
**推崇异种**（Biotech）：意见层面推崇 +10 / 反感 −30；心情层面全推崇 +2 到「我是被反感的异种人」−6。
若推崇异种为「智人种」，信徒**会拒绝一切基因植入手术**。

**文化代表色**：穿戴中**至少 60% 被染成代表色**时 +1 心情（用裁缝台做的服装不计入）。

---

## 5. modder 常见坑

1. **Ideo 不是 Def，`IdeoRole_*` 也不是。** 想改角色效果只能改 `PreceptDef` 的 `role*` 字段或打 Harmony 补丁
   （见 §2.4）。
2. **改 `PreceptDef` 前先看它有没有 `ParentName`。** 215 条里 29 条有父类（如 `SpeechPreceptBase`、
   `CharityPositiveBase`），改父类会波及全部子条目；只想改一条就用 `<Inherit>False</Inherit>`。
3. **`impact` 只有三档**，别自己编第四档。展示排序靠 `displayOrderInIssue` 和 `displayOrderInImpact`，
   不靠 `impact` 本身。
4. **模因靠 `associatedMemes` 捆绑戒律。** 想让新模因自动带上一批戒律，改模因；
   想让某戒律跟着新模因走，改戒律的 `associatedMemes`。两边都要看。
5. **`MemeDef.requireOne` 的嵌套语义**：顶层 `<li>` 之间是「或」，同层内是「且」。
6. **模因互斥**用 `groupDef` + `exclusionTags` 两套机制，别只改一处。
7. **风格样式最多 3 种，只有第 1 种的仪式音效会被用**；后面的会被前面的覆盖。
8. **依赖 Ideology DLC 的 mod 必须处理 DLC 未加载的情况**——本系统内容会整体消失。
9. **中文名不要自己翻译**。用 `DefInjected/<DefType>/<file>.xml` 的 `<defName>.label` / `.description`，
   那是官方译文；若要提供自己的翻译，参考
   [../ref/translations-localization.md](../translations-localization.md)。
