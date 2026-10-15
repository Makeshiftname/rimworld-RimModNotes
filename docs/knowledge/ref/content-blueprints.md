# 内容蓝本：本地化 / 研究项目 / ThingDef 与 RecipeDef

> 参考手册层。蒸馏自 rimworldwiki `Modding Tutorials` vetted 区，**不含仓库特定结论**。
> 本仓库的纯 XML 补丁范式见 [xml-defs-and-patches.md](../xml-defs-and-patches.md)。
>
> 来源页（最后核对 2026-10-15）：
> [Localization](https://rimworldwiki.com/wiki/Modding_Tutorials/Localization) ·
> [Research Projects](https://rimworldwiki.com/wiki/Modding_Tutorials/Research_Projects) ·
> [Basic Melee Weapon](https://rimworldwiki.com/wiki/Modding_Tutorials/Basic_Melee_Weapon) ·
> [Basic Ranged Weapon](https://rimworldwiki.com/wiki/Modding_Tutorials/Basic_Ranged_Weapon) ·
> [Basic Plant](https://rimworldwiki.com/wiki/Modding_Tutorials/Basic_Plant) ·
> [Custom Drug](https://rimworldwiki.com/wiki/Modding_Tutorials/Custom_Drug)
>
> 所属 wiki 章节：Game Systems Guides + XML Tutorials
>
> 官方样例仓库：
> [Aelanna/ExampleMeleeWeapon](https://github.com/Aelanna/ExampleMeleeWeapon) ·
> [Aelanna/ExampleRangedWeapon](https://github.com/Aelanna/ExampleRangedWeapon) ·
> [Aelanna/ExamplePlant](https://github.com/Aelanna/ExamplePlant)
>
> 📌 **页面成熟度**：`Localization` 自称 stub，`Research Projects` 标 Under Review
> （最后更新 2025-10-06，对应 1.6），其余两篇是完整教程。

---

## 1. 本地化（Localization）

### 1.1 目录结构

```
MyModFolder/Languages/
  ├ English/
  │  ├ DefInjected/     翻译 XML Def 里的字段
  │  ├ Keyed/           翻译 C# 里用的字符串
  │  └ Strings/         RulePackDefs 用的词表
  ├ French (Français)/
  └ German (Deutsch)/
```

### 1.2 `DefInjected/`

翻译 **Def 内部字段**。

- **子文件夹名必须与 Def 类型名一致**——翻译 `ThingDef` 就放在 `ThingDef/` 里。
- 根标签 `<LanguageData>`，内部节点路径要**与要翻译的标签结构一致**，
  用 `.` 分隔层级：

```xml
<LanguageData>
  <Beer.label>bière</Beer.label>
  <Beer.description>Mise à part l'eau, la première boisson…</Beer.description>
  <Beer.ingestible.ingestCommandString>Boire {0}</Beer.ingestible.ingestCommandString>
  <Beer.tools.bottle.label>bouteille</Beer.tools.bottle.label>
</LanguageData>
```

即 `defName` + `.` + 标签路径。

### 1.3 `Keyed/`

翻译 **C# 里的字符串**（自定义 UI、消息）。

- ⚠️ **即使是默认语言也必须建**。
  **强烈建议**：即使你不打算翻译，自建程序集里的字符串也一律走 Keyed——
  否则别人根本无法给你的 mod 做翻译。
- **文件名和子目录都无所谓**，只按个人习惯组织。
- ⚠️ **键名本身要加前缀**——原版和所有 mod 的翻译键会**合并进同一张表**。
  官方内容不前缀，但 mod **必须**前缀（例：`ARR_UsePotionOn`）。

```xml
<LanguageData>
  <Example_KeyString>Current Class: Level {0} {1} ({2}%)</Example_KeyString>
</LanguageData>
```

```csharp
int level = 4;
string className = "Rogue";
float levelProgress = 96f;
string s = "Example_KeyString".Translate(
    level.ToString("N0"), className, levelProgress.ToString("F1"));
// → "Current Class: Level 4 Rogue (96.0%)"
```

- `{0}`、`{1}`、`{2}` 按位置对应 `Translate` 的第 0、1、2 个参数。
- ⚠️ **键找不到时会直接显示键本身**；**开 dev mode 会渲染成故障字形（zalgo text）**，
  方便一眼发现。
- 命名参数也支持，但 wiki 指向 `GrammarResolver`（并注明该链接是临时占位）。

### 1.4 `Strings/`

（wiki 标注为 Placeholder）给 `RulePackDefs` 提供词表，
用于程序化生成姓名与句子。

---

## 2. 研究项目（ResearchProjectDef）

用 `ResearchProjectDef` 定义研究项目，把建筑和合成配方锁在研究系统后面。
**除注明外全部字段可选。**

| 字段 | 说明 |
|---|---|
| `<baseCost>` | 基础研究点数。**最终点数会按玩家派系与该项目的 techLevel 差异调整**。与 `knowledgeCost` **二选一，不能同时有** |
| `<preqrequisites>` | 前置研究，全部完成才能开始。**同 tab 内的前置会画连线** |
| `<hiddenPreqrequisites>` | 同上但**不画连线**，用于减少视觉杂乱 |
| `<techLevel>` | `Undefined` / `Animal` / `Neolithic` / `Medieval` / `Industrial` / `Spacer` / `Ultra` / `Archotech`。⚠️ **是枚举而非 Def，不改源码无法新增**，硬加会有兼容问题 |
| `<requiredResearchBuilding>` | 限定必须在某个研究台做。⚠️ **研究台之间没有递进关系**——指定简单研究台后，在高科技研究台上**反而不能研究** |
| `<requiredResearchFacilities>` | 限定研究台附属设施。**推荐用它而不是自定义研究台**来做门禁——研究台占地方太大。还可以把设施设为不可建造、只能从商人或任务获取 |
| `<tags>` | 标记为特定开局 scenario 的起始项目（如 `ClassicStart`、`TribalStart`） |
| `<tab>` | 指定自定义 tab（见 §2.2） |
| `<researchViewX>` / `<researchViewY>` | 在原生研究窗口 tab 内的坐标 |
| `<discoveredLetterTitle>` / `<discoveredLetterText>` | 完成时发一封信 |
| `<discoveredLetterDisabledWhen>` | 难度设置禁用了某些机制时不发这封信。可选：`bigThreatsDisabled`、`trapsDisabled`、`turretsDisabled`、`mortarsDisabled`、`extremeWeatherIncidentsDisabled` |
| `<hideWhen>` | 同上这些开关，但用于**隐藏整个研究项目**而非只禁信件 |
| `<requiredAnalyzed>` | 必须先分析完这些 `CompAnalyzable` 物件才能开始 |
| `<techprintCount>` / `<techprintCommonality` / `<techprintMarketValue>` | **仅 Royalty**。⚠️ **Royalty 未加载时该项目免费可研究** |
| `<heldByFactionCategoryTags>` | **仅 Royalty**。限定蓝图可从哪些派系获得。原版标签：`Tribal`、`Outlander`、`Ancient`、`Empire`、`OutlanderRefugee`、`Beggars`、`Pilgrims`、`Sanguophages`、`HoraxCult`、`Salvager`、`TradersGuild` |
| `<requiresMechanitor>` | **仅 Biotech**。只能由 mechanitor 研究 |
| `<knowledgeCategory>` / `<knowledgeCost>` | **仅 Anomaly**。知识类别（Basic / Advanced）与知识点数。**不能与 `baseCost` 同时存在** |
| `<teachConcept>` | 完成后在 Learning Helper 触发指定 `ConceptDef` |
| `<generalRules>` | 蓝图（Schematic）的风味文本生成规则。⚠️ **没有 `generalRules` 的项目永远不会生成蓝图** |
| `<recalculatePower>` | 完成后强制现有电网重算负载 |
| `<requireGravEngineInspected>` | **仅 Odyssey**。必须先检查过可用的重力引擎 |
| `<customUnlockTexts>` | 往研究描述的「解锁」段追加任意文本行 |

### 2.1 已废弃 / 无用字段

| 字段 | 状态 |
|---|---|
| `<requiredByThis>` | 原版 XML 未使用，也**看不出有 C# 用它**，用途不明 |
| `<researchMods>` | 本是为「研究完成时通知 C# 类」的钩子，早期 alpha 用过（如 Gun Turret Cooling），**现已不再使用** |

### 2.2 自定义研究 tab

项目多的话值得单独开一个 tab：先建一个 `ResearchTabDef`，
再用 `<tab>` 字段指定（值就是它的 `defName`）。

```xml
<ResearchTabDef>
  <defName>MyCustomResearchTab</defName>
  <label>My Mod</label>
  <generalTitle>Custom mod research</generalTitle>
  <generalDescription>由我的 mod 解锁的高级研究。</generalDescription>
</ResearchTabDef>
```

`ResearchTabDef` 在 1.5 增加了这些字段：

| 字段 | 默认 | 说明 |
|---|---|---|
| `<generalTitle>` | `""` | tab 提示框顶部的黄色文字 |
| `<generalDescription>` | `""` | tab 提示框正文的白色文字 |
| `<visibleByDefault>` | `true` | 设为 false 可以把 tab 的信息藏到满足 `minMonolithLevelVisible` 才显示 |
| `<minMonolithLevelVisible>` | `-1` | 与 Anomaly 有关 |
| `<tutorTag>` | `null` | 学习向导高亮该 tab 时用 |

### 2.3 研究项目在 tab 里的坐标

`<researchViewX>` / `<researchViewY>` 决定项目在原生研究窗口 tab 内的位置。

⚠️ **X 轴每格 1，Y 轴每格 0.7**——不是等距坐标，Y 要按 0.7 递进。

⚠️ **多个项目占用同一坐标会被自动错开（stagger）**，
通常正是多个 mod 抢同一个位置的结果。
研究项目多到值得开一个自定义 tab（§2.2）时，就该考虑这个问题。

### 2.4 哪些 Def 能挂研究前置

| XML 对象 | 字段 |
|---|---|
| `RecipeDef` | `researchPrerequisite`（单个）/ `researchPrerequisites`（列表） |
| `BuildableDef`（含 `ThingDef`） | `researchPrerequisites`（**仅列表**） |
| `PlantProperties` | `sowResearchPrerequisites`（**仅列表**） |
| `RecipeMakerProperties` | `researchPrerequisite`（单个）/ `researchPrerequisites`（列表） |
| `TerrainTemplateDef` | `researchPrerequisites`（**仅列表**） |
| `DesignationCategoryDef` | `researchPrerequisites`（**仅列表**） |
| `PsychicRitualDef` | `researchPrerequisites`（**仅列表**） |

> ⚠️ **`BuildableDef` 没有单个 `researchPrerequisite` 字段**，只有列表版。

### 2.5 C# 判断研究是否完成

拿到 `ResearchProjectDef` 后调用 **`IsFinished`** 属性：

```csharp
if (def_MyResearch.IsFinished)
{
    // ...
}
```

**频繁查询就用 `DefOf` 拿静态引用**（见
[defs-and-patches-ref.md](defs-and-patches-ref.md)），不要每次现查。

---

## 3. ThingDef 蓝本：近战武器

> ⚠️ **通用原则**：**永远从最接近的原版 ThingDef 复制一份再改**。
> 近战武器抄 gladius（原版 XML 在 `Data/Core/Defs/ThingDefs_Misc/Weapons/MeleeMedieval.xml`）。

```xml
<ThingDef ParentName="BaseMeleeWeapon_Sharp_Quality">
  <defName>ExampleMeleeWeapon_Machete</defName>
  <label>machete</label>
  <description>…</description>
  <graphicData>
    <texPath>ExampleMod/ExampleWeapon_Machete</texPath>
    <graphicClass>Graphic_Single</graphicClass>
    <shaderType>CutoutComplex</shaderType>
  </graphicData>
  <costStuffCount>40</costStuffCount>
  <statBases>
    <WorkToMake>10000</WorkToMake>
    <Mass>0.75</Mass>
  </statBases>
  <equippedStatOffsets>
    <PlantWorkSpeed>0.10</PlantWorkSpeed>
  </equippedStatOffsets>
  <relicChance>1</relicChance>
  <tools>
    <li>
      <label>handle</label>
      <capacities><li>Blunt</li></capacities>
      <power>9</power>
      <cooldownTime>2</cooldownTime>
    </li>
    <li>
      <label>point</label>
      <capacities><li>Stab</li></capacities>
      <power>14</power>
      <cooldownTime>2</cooldownTime>
    </li>
    <li>
      <label>edge</label>
      <capacities><li>Cut</li></capacities>
      <power>14</power>
      <cooldownTime>2</cooldownTime>
    </li>
  </tools>
  <recipeMaker>
    <researchPrerequisite>Smithing</researchPrerequisite>
    <skillRequirements><Crafting>2</Crafting></skillRequirements>
    <displayPriority>409</displayPriority>
  </recipeMaker>
</ThingDef>
```

| 字段 | 要点 |
|---|---|
| `defName` | 全局唯一，**强烈建议加 mod 前缀** |
| `label` | ⚠️ **除非是专有名词，一律小写**——这样能自然嵌进句子里。用在标题或句首时游戏会自动首字母大写 |
| `graphicData.texPath` | 同样要**全局唯一**，用命名空间目录隔离 |
| `shaderType` | 用贴图遮罩做材质染色**必须**设 `CutoutComplex` |
| `costStuffCount` | 「材料」的消耗量。stuff = 物品的材质类别 |
| `statBases` | `WorkToMake` 制作工作量、`Mass` 重量 |
| `equippedStatOffsets` | 装备时给持有者加的属性偏移 |
| `relicChance` | 被 ideology 当作圣物使用的概率。machete 这类工具设 1（几乎不会） |
| `tools` | 近战攻击的种类、威力、冷却 |
| `recipeMaker.displayPriority` | 决定在合成列表里的排序，**数值越小越靠前** |

> 💡 近战武器的**同一张贴图**同时用于地面物品和装备外观——
> 所以想让「只有刀刃上材质色、握柄保持原色」，就得靠贴图遮罩 + `CutoutComplex`
> （遮罩规则见 [assets-and-rendering.md](assets-and-rendering.md) §1.3）。

---

## 4. ThingDef 蓝本：植物

> 从原版 **healroot** 的三个 Def 抄起（`Data/Core/Defs/ThingDefs_Plants/Plants_Cultivated_Farm.xml`
> 与 `Plants_Wild_General.xml`）。教程用「theragold」这个例子。

### 4.1 三段式结构：抽象基类 + 栽培 + 野生

```xml
<!-- 抽象基类：不会被单独加载，只供继承 -->
<ThingDef ParentName="PlantBase" Name="TheragoldBase" Abstract="True">
  <graphicData>
    <texPath>ExampleMod/Theragold</texPath>
    <graphicClass>Graphic_Random</graphicClass>
  </graphicData>
  <selectable>true</selectable>
  <plant>
    <immatureGraphicPath>ExampleMod/ImmatureTheragold</immatureGraphicPath>
    <growDays>8</growDays>
    <dieIfLeafless>false</dieIfLeafless>
    <harvestWork>400</harvestWork>
    <harvestTag>Standard</harvestTag>
    <harvestedThingDef>ExamplePlant_RawTheragold</harvestedThingDef>
    <harvestYield>8</harvestYield>
    <topWindExposure>0.1</topWindExposure>
    <visualSizeRange>0.3~1.0</visualSizeRange>
    <wildOrder>2</wildOrder>
    <allowAutoCut>false</allowAutoCut>
  </plant>
</ThingDef>

<ThingDef ParentName="TheragoldBase">   <!-- 栽培型 -->
  <defName>ExamplePlant_Theragold</defName>
  <label>theragold</label>
  <plant>
    <growDays>5</growDays>
    <sowWork>250</sowWork>
    <sowMinSkill>6</sowMinSkill>
    <sowTags><li>Ground</li><li>Hydroponic</li></sowTags>
    <purpose>Food</purpose>
  </plant>
</ThingDef>

<ThingDef ParentName="TheragoldBase">   <!-- 野生型 -->
  <defName>ExamplePlant_TheragoldWild</defName>
  <label>wild theragold</label>
  <neverMultiSelect>false</neverMultiSelect>
  <plant>
    <wildClusterRadius>4</wildClusterRadius>
    <wildClusterWeight>80</wildClusterWeight>
    <wildBiomes>
      <TemperateForest>0.05</TemperateForest>
      <BorealForest>0.16</BorealForest>
      <AB_FeraliskInfestedJungle MayRequire="sarg.alphabiomes">0.05</AB_FeraliskInfestedJungle>
    </wildBiomes>
  </plant>
</ThingDef>
```

> 🔑 **`Abstract="True"` + `Name="X"` 定义抽象基类，
> 其他 Def 用 `ParentName="X"` 继承。** 这是复用字段的标准手段。

### 4.2 字段要点

| 字段 | 要点 |
|---|---|
| `graphicClass=Graphic_Random` | **必须指向文件夹而非文件名**。植物还会**随机水平翻转**增加变化 |
| `selectable` | ⚠️ 继承 `PlantBase` 的植物**默认可选中**——原版只有草、灌木、anima/gauranlen 这类完全不可交互的植物才不可选。能被有意义交互就该覆写 |
| `neverMultiSelect` | 默认植物**不会**被框选选中；有价值的野生植物要设为 `false` |
| `growDays` | **整天数**。⚠️ **植物夜间休眠，实际游戏天数比这个值长** |
| `harvestedThingDef` / `harvestYield` | 收获产出什么、多少。⚠️ **最终产量会受殖民者 Plant 技能和属性影响** |
| `harvestTag` / `harvestWork` | 收获的工作量与标签 |
| `sowTags` | 决定能在哪播种。`Ground`/`Hydroponic` 可，装饰性花盆不行 |
| `sowMinSkill` | 播种最低 Plant 技能 |
| `visualSizeRange` | 格式是 `min~max`，对应 0% 和 100% 生长的缩放 |
| `wildBiomes` | ⚠️ **天然生成的生物群系 + 群系内权重**。**这比用 PatchOperation 往 BiomeDef 里塞简单得多** |
| `wildClusterRadius` / `wildClusterWeight` | 野生生成的成簇半径与权重 |
| `allowAutoCut` | 是否允许自动采收（割草机器人） |

### 4.3 野生生物群系的 mod 兼容写法

给第三方生物群系 mod 加兼容时**用 `MayRequire`**，这样对方没装就自动跳过：

```xml
<wildBiomes>
  <!-- 原版 -->
  <TemperateForest>0.05</TemperateForest>
  <!-- Alpha Biomes -->
  <AB_FeraliskInfestedJungle MayRequire="sarg.alphabiomes">0.05</AB_FeraliskInfestedJungle>
  <!-- More Vanilla Biomes -->
  <ZBiome_CloudForest MayRequire="zylle.MoreVanillaBiomes">0.05</ZBiome_CloudForest>
  <!-- Biomes! Islands -->
  <BiomesIslands_BorealIsland MayRequire="BiomesTeam.BiomesIslands">0.16</BiomesIslands_BorealIsland>
  <!-- Biomes! Oasis -->
  <BMT_ChromaticOasis MayRequire="BiomesTeam.Oasis">0.2</BMT_ChromaticOasis>
</wildBiomes>
```

> ⚠️ 这个清单**不完整**——原版生物群系和其他 mod 的生物群系都有很多，
> 这里只列了这株植物合理会出现的，且需经各作者同意后收录。

---

## 5. 原料物品 Def + RecipeDef

### 5.1 收获物 ThingDef

从 `RawRice` / `RawPotatoes` / `RawCorn` 抄
（`Data/Core/Defs/ThingDefs_Items/Items_Resource_RawPlant.xml`）：

```xml
<ThingDef ParentName="PlantFoodRawBase">
  <defName>ExamplePlant_RawTheragold</defName>
  <label>theragold flowers</label>
  <description>…</description>
  <graphicData>
    <texPath>ExampleMod/RawTheragold</texPath>
  </graphicData>
  <comps>
    <li Class="CompProperties_Rottable">
      <daysToRotStart>30</daysToRotStart>
      <rotDestroys>true</rotDestroys>
    </li>
  </comps>
</ThingDef>
```

> ⚠️ `PlantFoodRawBase` 用的是 **`Graphic_Single`**，所以 `texPath`
> **直接指向贴图而不是文件夹**——与植物的 `Graphic_Random` 相反。
> 判断依据永远是**继承的基类用了哪种 graphicClass**。

### 5.2 RecipeDef 完整字段

```xml
<RecipeDef>
  <defName>ExamplePlant_Make_MedicineHerbal</defName>
  <label>make herbal medicine from theragold</label>
  <description>…</description>
  <jobString>Making herbal medicine from theragold flowers.</jobString>
  <effectWorking>Cook</effectWorking>
  <soundWorking>Recipe_CookMeal</soundWorking>
  <workSkill>Cooking</workSkill>
  <workAmount>300</workAmount>
  <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
  <recipeUsers>
    <li>CraftingSpot</li>
    <li>DrugLab</li>
  </recipeUsers>
  <ingredients>
    <li>
      <filter><thingDefs><li>ExamplePlant_RawTheragold</li></thingDefs></filter>
      <count>12</count>
    </li>
  </ingredients>
  <fixedIngredientFilter>
    <thingDefs><li>ExamplePlant_RawTheragold</li></thingDefs>
  </fixedIngredientFilter>
  <products><MedicineHerbal>1</MedicineHerbal></products>
  <skillRequirements><Crafting>2</Crafting></skillRequirements>
</RecipeDef>
```

| 字段 | 说明 |
|---|---|
| `defName` | 命名惯例：mod 前缀 + **`Make_`** + 产物名（与原版自动生成配方的惯例一致） |
| `label` | 显示在**工作台的 Add Bill 列表**里——**带上主要原料能区分同产物的不同配方** |
| `description` | Bill 界面里显示的完整描述 |
| `jobString` | pawn 检查面板里正在做这个配方时显示的文字 |
| `effectWorking` | 制作过程中的 `EffecterDef`（特效） |
| `soundWorking` | 制作过程中持续播放的 `SoundDef` |
| `workSkill` | **做这个配方涨哪个技能的经验**，同时也是 Bill 界面的过滤技能 |
| `workAmount` | 工作量。300 ≈ 原版简易餐的量，在 100% 速度下约 5 秒 |
| `workSpeedStat` | 决定工作速度的 `StatDef` |
| `recipeUsers` | 能在哪些工作台做（用 `defName`）。⚠️ 也可以用 PatchOperation 往工作台上加，但**直接写 `recipeUsers` 通常更简单**，尤其是目标工作台不是自己的时候 |
| `ingredients` / `filter` | 消耗什么。`filter` 支持各种过滤条件，此处只用 `thingDefs` |
| `fixedIngredientFilter` | **所有能用来满足配方的东西**。与 `ingredients` 配对使用 |
| `products` | 产出什么、多少 |
| `skillRequirements` | 整个标签**可以省略**（表示无技能要求） |

---

## 6. ThingDef 蓝本：远程武器（纯 XML，无需 C#）

> 从原版 **pump shotgun** 抄（`Data/Core/Defs/ThingDefs_Misc/Weapons/RangedIndustrial.xml`）。
> 样例仓库：[Aelanna/ExampleRangedWeapon](https://github.com/Aelanna/ExampleRangedWeapon)

### 6.1 武器本体

```xml
<ThingDef ParentName="BaseHumanMakeableGun">
  <defName>ExampleGun_SawLauncher</defName>
  <label>saw launcher</label>
  <description>…</description>
  <graphicData>
    <texPath>ExampleMod/Example_SawLauncher</texPath>
    <graphicClass>Graphic_Single</graphicClass>
  </graphicData>
  <costList>
    <Steel>80</Steel>
    <ComponentIndustrial>4</ComponentIndustrial>
  </costList>
  <statBases>
    <WorkToMake>15000</WorkToMake>
    <Mass>9.9</Mass>
    <AccuracyTouch>0.85</AccuracyTouch>
    <AccuracyShort>0.80</AccuracyShort>
    <AccuracyMedium>0.70</AccuracyMedium>
    <AccuracyLong>0.56</AccuracyLong>
    <RangedWeapon_Cooldown>1.8</RangedWeapon_Cooldown>
  </statBases>
  <equippedStatOffsets>
    <ConstructionSpeed>0.10</ConstructionSpeed>
  </equippedStatOffsets>
  <verbs>
    <li>
      <verbClass>Verb_Shoot</verbClass>
      <hasStandardCommand>true</hasStandardCommand>
      <defaultProjectile>ExampleBullet_SawBlade</defaultProjectile>
      <warmupTime>1.2</warmupTime>
      <range>12.9</range>
      <soundCast>ExampleWeapon_SawLauncher</soundCast>
      <soundCastTail>GunTail_Heavy</soundCastTail>
      <muzzleFlashScale>0</muzzleFlashScale>
    </li>
  </verbs>
  <tools>
    <li>
      <label>handle</label>
      <capacities><li>Blunt</li></capacities>
      <power>9</power><cooldownTime>2</cooldownTime>
    </li>
    <li>
      <label>blade</label>
      <capacities><li>Demolish</li></capacities>
      <power>13</power><cooldownTime>2</cooldownTime>
      <soundMeleeHit>ExampleWeapon_SawLauncherMelee</soundMeleeHit>
      <soundMeleeMiss>ExampleWeapon_SawLauncher</soundMeleeMiss>
    </li>
  </tools>
  <recipeMaker>
    <skillRequirements><Crafting>5</Crafting></skillRequirements>
    <displayPriority>436</displayPriority>
  </recipeMaker>
</ThingDef>
```

- `verbs` 列表定义**远程攻击行为**：`verbClass`、`defaultProjectile`（指向自定义弹丸 Def）、
  `warmupTime`（举枪时间）、`range`、`soundCast`、`soundCastTail`（枪口回响）、
  `muzzleFlashScale`（0 = 无枪口火焰）。
- ⚠️ **武器的 `graphicData` 贴图同时用于地面物品和装备外观**；
  **弹丸贴图在单独的 ThingDef 里定义**。
- 近战 `tools` 里的每个 tool 可以单独配 `soundMeleeHit` / `soundMeleeMiss`。
- `capacities` 用 `Demolish` 可实现类似破门斧的拆建筑攻击。
- ⚠️ **没有专门影响「拆除速度」的 StatDef**，
  但 `ConstructionSpeed` 会影响拆除速度——教程认为拿它凑合说得过去。

### 6.2 弹丸 ThingDef

同样从原版 `Bullet_Shotgun` 抄（就在同一文件里武器 Def 的下方）：

```xml
<ThingDef ParentName="BaseBullet">
  <defName>ExampleBullet_SawBlade</defName>
  <label>saw blade</label>
  <graphicData>
    <texPath>ExampleMod/Example_SawBlade</texPath>
    <graphicClass>Graphic_Single</graphicClass>
  </graphicData>
  <projectile>
    <damageDef>ExampleDamage_Saw</damageDef>
    <damageAmountBase>25</damageAmountBase>
    <stoppingPower>1.5</stoppingPower>
    <armorPenetrationBase>0.19</armorPenetrationBase>
    <speed>55</speed>
  </projectile>
</ThingDef>
```

- **弹丸的 `defName` 必须与武器 `verbs` 里的 `defaultProjectile` 一致。**
- `label` 不显示在武器属性卡上，但**游戏暂停时鼠标悬停在飞行中的弹丸上会看到**。
- `damageAmountBase` / `stoppingPower`（击停力）/ `armorPenetrationBase`（穿甲）/ `speed`。

### 6.3 自定义 DamageDef

从零创建但继承原版 `Arrow`——因为它**已经是远程伤害类型**（对护盾腰带判定有效）、
造成 `Cut` 伤、被 `Sharp` 护甲克制：

```xml
<DamageDef ParentName="Arrow">
  <defName>ExampleDamage_Saw</defName>
  <label>saw</label>
  <deathMessage>{0} has been sliced to death by a saw blade.</deathMessage>
</DamageDef>
```

- `label` 会显示在武器与弹丸属性卡的伤害分解里。
- `deathMessage` 是战斗日志中的死亡信息；
  **`{0}` 是注入占位符**，由游戏的 grammar resolver 替换为 pawn 名字。

### 6.4 自定义 SoundDef（两个）

```xml
<SoundDef>
  <defName>ExampleWeapon_SawLauncher</defName>
  <context>MapOnly</context>
  <subSounds>
    <li>
      <grains>
        <li Class="AudioGrain_Clip">
          <clipPath>ExampleMod/Example_SawLaunch</clipPath>
        </li>
      </grains>
      <volumeRange>40~45</volumeRange>
      <pitchRange>0.8940217~1.084783</pitchRange>
    </li>
  </subSounds>
</SoundDef>
```

| 字段 | 说明 |
|---|---|
| `context=MapOnly` | 只在普通地图（定居点/任务图）播放；**切到世界地图会停** |
| `grains` / `AudioGrain_Clip` / `clipPath` | 指向实际音频文件，路径**相对 mod 根、不带扩展名** |
| `volumeRange` | 音量区间，每次播放**在该区间内随机**。⚠️ 不同素材基准音量不同，通常要反复试听到舒服为止 |
| `pitchRange` | 在区间内随机变调播放，**能显著减少「听起来很重复」的观感** |

---

## 7. ThingDef + HediffDef 蓝本：自定义药物

> 从原版 **Smokeleaf** 抄（`Data/Core/Defs/Drugs/Smokeleaf.xml`）。
> 样例叫 xylin，一种强效止痛药。**注意：wiki 这一页大量字段写的是 "TBA"**——
> 即该页**本身就没写完**，下面只保留它确实解释了的部分。

### 7.1 药物物品 ThingDef（继承 `MakeableDrugBase`）

```xml
<ThingDef ParentName="MakeableDrugBase">
  <defName>XylinDrug</defName>
  <label>xylin</label>
  <description>…</description>
  <possessionCount>5</possessionCount>
  <descriptionHyperlinks>
    <HediffDef>XylinHigh</HediffDef>
  </descriptionHyperlinks>
  <graphicData>
    <texPath>ExampleMod/Xylin</texPath>
    <graphicClass>Graphic_StackCount</graphicClass>
  </graphicData>
  <statBases>
    <WorkToMake>450</WorkToMake>
    <MarketValue>16</MarketValue>
    <Mass>0.05</Mass>
    <DeteriorationRate>6</DeteriorationRate>
    <Flammability>1.3</Flammability>
  </statBases>
  <ingestible>
    <baseIngestTicks>80</baseIngestTicks>
    <drugCategory>Medical</drugCategory>
    <ingestSound>Ingest_Inject</ingestSound>
    <ingestHoldUsesTable>false</ingestHoldUsesTable>
    <ingestCommandString>Inject {0}</ingestCommandString>
    <ingestReportString>Injecting {0}.</ingestReportString>
    <outcomeDoers>
      <li Class="IngestionOutcomeDoer_GiveHediff">
        <hediffDef>XylinHigh</hediffDef>
        <severity>1.0</severity>
      </li>
    </outcomeDoers>
  </ingestible>
  <recipeMaker>
    <recipeUsers><li>DrugLab</li></recipeUsers>
    <workSpeedStat>DrugCookingSpeed</workSpeedStat>
    <workSkill>Cooking</workSkill>
    <displayPriority>2000</displayPriority>
  </recipeMaker>
  <costList><Chemfuel>4</Chemfuel></costList>
  <comps>
    <li Class="CompProperties_Drug"><listOrder>20</listOrder></li>
  </comps>
</ThingDef>
```

| 字段 | 说明 |
|---|---|
| `possessionCount` | 按策略或手动携带时，pawn 会带几个 |
| `descriptionHyperlinks` | 物品信息页上要挂哪些超链接 |
| `graphicClass=Graphic_StackCount` | **按堆叠数量显示不同贴图**（单颗 vs 满堆两张图）。⚠️ 此时 `texPath` 指向**文件夹** |
| `MarketValue` | 以银计的售价（**未含殖民地劣势等修正**） |
| `Mass` | 商队负重 |
| `DeteriorationRate` | 露天存放时每天损失多少耐久点 |
| `Flammability` | 直接受火时腐坏的速度 |
| `baseIngestTicks` | 摄入耗时（tick） |
| **`drugCategory`** | ⭐ **决定 AI 什么时候会用它**：<br>`Social` 用于娱乐；`Hard` 会在特定精神崩溃时使用；<br>**`Medical` 除非玩家手动指示或策略要求，否则 AI 不会使用** |
| `ingestHoldUsesTable` | 是否需要找张桌子才能用 |
| `useEatingSpeedStat` | 摄入时间是否受进食速度属性影响 |
| `outcomeDoers` / `IngestionOutcomeDoer_GiveHediff` | **摄入后给什么 Hediff**——把药物和效果连起来的核心 |
| `CompProperties_Drug.listOrder` | 药物列表里的排序 |
| `workSpeedStat=DrugCookingSpeed` | 制作速度受哪个属性影响 |

### 7.2 效果 HediffDef

```xml
<HediffDef>
  <defName>XylinHigh</defName>
  <label>high on xylin</label>
  <labelNoun>a xylin high</labelNoun>
  <description>Xylin's active chemical in the bloodstream. Dulls pain.</description>
  <hediffClass>Hediff_High</hediffClass>
  <defaultLabelColor>(1,0,0.5)</defaultLabelColor>
  <scenarioCanAdd>true</scenarioCanAdd>
  <maxSeverity>1.0</maxSeverity>
  <isBad>false</isBad>
  <comps>
    <li Class="HediffCompProperties_SeverityPerDay">
      <severityPerDay>-0.5</severityPerDay>
      <showHoursToRecover>true</showHoursToRecover>
    </li>
  </comps>
  <stages>
    <li>
      <painOffset>-0.9</painOffset>
      <capMods>
        <li><capacity>Consciousness</capacity><offset>-0.1</offset></li>
      </capMods>
    </li>
  </stages>
</HediffDef>
```

| 字段 | 说明 |
|---|---|
| `hediffClass` | 效果类型（`Hediff_High` = 兴奋剂类） |
| `defaultLabelColor` | 健康页里的显示颜色 |
| `scenarioCanAdd` | 自定义 scenario 能否把它作为初始状态加入 |
| `isBad` | ⭐ **是否算「负面效果」**。healer mech serum 和 resurrection mech serum 会用它来判断要不要移除该效果 |
| `HediffCompProperties_SeverityPerDay` | 每天严重度变化；`showHoursToRecover` 控制健康页是否显示剩余小时数 |
| `stages` | 按严重度分段的效果：`painOffset` 痛觉减免，`capMods` 能力偏移 |

> 📌 **1.6 的相关变化**：DrugDef 上的 **`chemicalNeed`** 取代了旧的 `causesNeed`
> （见 [version-migration.md](version-migration.md) §3.2）。
> 本教程示例未用到需求系统，只演示了即时的 Hediff 效果。
