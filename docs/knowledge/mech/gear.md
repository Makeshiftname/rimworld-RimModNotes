# 装备 Gear（武器 / 护甲 / 品质 / 武器特性）

> **mech 层**：本文只讲**装备系统由什么构成、字段含义、数据模型、内容清单**。
> 「怎么造一件能用的武器/护甲」见 [../ref/content-blueprints.md](../ref/content-blueprints.md)
> （里面有可直接抄的近战/远程武器 ThingDef 蓝本）；
> 「怎么改服装生成逻辑」见 [../ref/csharp-basics.md](../ref/csharp-basics.md)（pawn 生成第 26 步 `GenerateGearFor`）。
>
> **数据源与核对日期（2026-10-16）**：
> 本地游戏数据 `F:\Games\GBL\rimworld\Data\`（RimWorld 1.6，全 6 DLC），中文取官方简中 DefInjected。
> 复现：`python tools/game_defs/extract_defs.py --def ThingDef --source apparel` 与 `--source misc/weapons`。
>
> 官方样例仓库：[Aelanna/ExampleMeleeWeapon](https://github.com/Aelanna/ExampleMeleeWeapon) ·
> [Aelanna/ExampleRangedWeapon](https://github.com/Aelanna/ExampleRangedWeapon)。
> ⚠️ 装备是**跨 DLC 跨系统**的东西——护甲涉及护甲值、保温、遮蔽、灵能、美感、姿态；
> 品质是**代码内置**的；武器特性只在 Royalty / Odyssey 出现。

---

## 1. 框架

### 1.1 一句话数据模型

装备**不是独立的 Def 类型**——武器和护甲**都是 `ThingDef`**，靠 `<thingClass>` 区分：

| thingClass | 是什么 | 本地实例数 |
|---|---|---:|
| `Apparel` | 护甲/衣物 | 115（`Apparel_*.xml`） |
| `Weapon` / `ThingWithComps` | 武器 | 106（`Weapons/` 目录） |

- 护甲的判定靠 `<apparel>` 块（`ApparelProperties`）是否存在
- 武器的判定靠 `<tools>`（近战）或 `<verbs>`（远程）是否存在

### 1.2 护甲的类继承链

1.6 的护甲基类是这样组织的（来自 `Apparel_Various.xml` 的 abstract Def）：

```
ApparelNoQualityBase          （抽象，规定 HP/易燃性/腐蚀率/美感等通用底子）
  └── ApparelBase             （加上 CompQuality + CompStyleable）
        ├── ApparelShieldBeltBase
        ├── HatMakeableBase    （可裁缝制作的一类，如 CowboyHat）
        └── …
```

🔑 **`ApparelNoQualityBase` 与 `ApparelBase` 的区别**是 1.6 的关键设计：
前者不带品质，后者带 `CompQuality`。做「不可有品质的基础件」时继承前者。

护甲基类默认携带的 `statBases`：`MaxHitPoints 100`、`Flammability 1.0`、
`DeteriorationRate 2`、`Beauty -3`。

### 1.3 护甲的 `<apparel>` 块

护甲最核心的结构，决定它挡什么、穿哪、算不算衣服：

| 字段 | 出现率 | 说明 |
|---|---:|---|
| `apparel.layers` | 68% | 占据的**装备层**，见 §2.1 |
| `apparel.bodyPartGroups` | 74% | 覆盖的**身体部位组**，可多个 |
| `apparel.wornGraphicPath` | 83% | 穿戴者身上显示的贴图路径（不带 `Textures/` 前缀） |
| `apparel.tags` | 47% | 标签，用于 outfit / 生成逻辑筛选 |
| `apparel.defaultOutfitTags` | 39% | 自动分配到的着装方案（`Worker` / `Soldier` / `Slave`…） |
| `apparel.countsAsClothingForNudity` | 41% | **是否算「衣服」**（裸体教派等会判它有伤风化） |
| `apparel.canBeDesiredForIdeo` | 17% | 能否作为 Ideology「偏好服装」（**装甲不能**） |
| `apparel.soundWear` / `soundRemove` | 2% | 穿/脱音效（如动力甲） |
| `apparel.developmentalStageFilter` | 20% | 限定成长阶段，如只给 `Child, Adult` |

### 1.4 护甲的属性都在 `statBases` 下

🔑 **护甲没有单独的 `<armorRating>` 块**——1.6 把护甲值做成普通 Stat，写在 `statBases` 里：

| 字段 | 含义 |
|---|---|
| `statBases.ArmorRating_Sharp` | **锐器**护甲（刀、箭） |
| `statBases.ArmorRating_Blunt` | **钝器**护甲（锤、棍） |
| `statBases.ArmorRating_Heat` | 火焰/热护甲 |
| `statBases.Insulation_Cold` | 保温（抗寒） |
| `statBases.Insulation_Heat` | 隔热（抗热） |
| `statBases.StuffEffectMultiplierArmor` | **材料**对护甲值的乘数 |
| `statBases.StuffEffectMultiplierInsulation_Cold` | 材料对保温的乘数 |
| `statBases.StuffEffectMultiplierInsulation_Heat` | 材料对隔热的乘数 |

「Stuff 乘数」是护甲的精妙处：护甲本体给一个基准值，**用什么材料（`stuffCategories`）再乘一个系数**。
45/115 件护甲带 `StuffEffectMultiplier*`，说明「同一版型、不同材质」是核心玩法。

> 💡 这解释了为什么护甲要 `costStuffCount`（耗材数量）和 `stuffCategories`（可用材料）：
> 做一件「钢制」还是「丝绸制」的同款护甲，是靠材质乘数 + 耗材成本驱动的。

护甲还能通过 `equippedStatOffsets` 附带各种装备时修正（本地产物实际出现的）：

| 修正 | 出现 | 例值 | 场景 |
|---|---:|---|---|
| `SlaveSuppressionOffset` | 20 | −0.05 ~ −0.2 | 奴隶主服饰压制反抗 |
| `PsychicEntropyRecoveryRate` | 11 | +0.033 ~ +0.091 | 灵能恢复 |
| `SocialImpact` | 8 | +0.10 ~ +0.2 | 社交影响（礼服等） |
| `MechBandwidth` | 8 | +3 ~ +9 | 机械带宽（驾驶服） |
| `PsychicSensitivity` | 7 | −0.90 / +0.05 | 灵能屏蔽/增强 |
| `MoveSpeed` | 7 | −0.8 ~ −0.12 | 重甲减速 |
| `VacuumResistance` | 7 | 0 ~ 0.67 | 真空防护 |
| `ToxicEnvironmentResistance` | 5 | +0.2 ~ +0.5 | 防毒 |
| `ShootingAccuracyPawn` | 3 | +3 / −5 | 精准类装备 |

### 1.5 武器的结构

武器分**近战**和**远程**，字段截然不同：

**近战武器**——靠 `<tools>` 列表，一件武器可以有多个攻击方式：

```xml
<tools>
  <li>
    <label>handle</label>
    <capacities><li>Blunt</li></capacities>   <!-- 打击方式 -->
    <power>9</power>
    <cooldownTime>2</cooldownTime>
  </li>
  <li>
    <label>blade</label>
    <capacities><li>Stab</li></capacities>
    <power>15.7</power>
    <cooldownTime>1</cooldownTime>
  </li>
</tools>
```

`capacities` 决定攻击形态：`Blunt`（钝击）· `Stab`（穿刺）· `Poke`（戳）·
`Demolish`（拆墙）· `Cut`（切割）。

**远程武器**——靠 `<verbs>`：

| 字段 | 说明 |
|---|---|
| `verbs.verbClass` | 射击行为：`Verb_Shoot`（连射）· `Verb_ShootOneUse`（单发）· `Verb_LaunchProjectile`（抛射） |
| `verbs.defaultProjectile` | 默认弹丸 ThingDef（如 `Bullet_Revolver`） |
| `verbs.warmupTime` | 开火前摇 |
| `verbs.range` | 射程 |
| `verbs.hasStandardCommand` | 是否有标准开火指令 |
| `statBases.RangedWeapon_Cooldown` | 射击间隔 |
| `statBases.AccuracyTouch/Short/Medium/Long` | 分距离段命中率 |
| `projectile.damageDef` | 伤害类型（`Bullet` / `Flame` / `Smoke`…） |
| `projectile.speed` | 弹丸速度 |

**近战武器还要标 `weaponClasses`**：`Melee` · `Neolithic` · `Medieval` · `RangedLight` 等
（决定它被算作什么类别，用于 Ideology 武器推崇、Trader 交易等）。

🔑 **尖端朝右**约定见 [../ref/assets-and-rendering.md](../ref/assets-and-rendering.md) §1.4。

### 1.6 🔴 品质是代码内置的，不是 Def

本地搜索**找不到 `QualityDef`、`<Quality>` 标签**——品质档位由 `CompQuality` 读一张**代码硬编码表**。
标签文本来自官方翻译的 `Strings`/`Keyed`，不是 DefInjected。

护甲/武器基类挂 `CompQuality`（见 §1.2）。品质机制本身不可直接 XML 扩展，
但**品质的效果**可通过 `WeaponTraitDef` 等间接挂钩（见 §1.7）。

> ⚠️ 想给某个 mod 物件加品质，**继承带 `CompQuality` 的基类**即可（如 `ApparelBase`）；
> 不要试图去 patch 一个 `QualityDef`——它不存在。

### 1.7 武器特性 WeaponTraitDef

| 量 | 值 |
|---|---|
| 总数 | **61**（**Royalty 19 + Odyssey 42**，Core 没有） |
| 核心字段 | `weaponCategory`（`Gun` / `Pistol` / `BulletFiring`…）· `traitAdjectives`（品质形容词，生成「迅捷的精确手枪」这种名字）· `commonality`（稀有度 0.5–1）· `statOffsets.MarketValue` · `exclusionTags`（互斥） |

`traitAdjectives` 最多三层（`li` / `li.1` / `li.2`），是武器命名「品质形容词」的来源。

---

## 2. 层级

### 2.1 装备层（ApparelLayer）——护甲之间如何共存

`ApparelLayerDef` 共 **6 个层**（全 Core，官方中文名）：

| 层 defName | 中文 | 典型物品 |
|---|---|---|
| `Shell` | 外套 | 大衣、动力甲 |
| `Middle` | 夹层 | 衬衫、中衣 |
| `OnSkin` | 贴身 | 内衣、贴身衣 |
| `Overhead` | 头饰 | 帽子、头盔 |
| `EyeCover` | 眼饰 | 护目镜、眼罩 |
| `Belt` | 配件 | 腰带、工具带 |

🔑 **规则：同一层只能穿一件**。这就是「外壳套夹克」这类着装冲突的来源——
`apparel.layers` 可写多个层（4/115 件写了双层，如 `Shell` + `Middle`）。

### 2.2 材质（stuff）→ 护甲

```
护甲实例（Apparel_XXX）
  ├─ stuffCategories     ← 能用什么材料做（Fabric / Woody / Metallic / Leathery）
  ├─ costStuffCount      ← 做一件要多少材料
  └─ statBases.StuffEffectMultiplier*
        ├─ ...Armor              ← 材料影响护甲值
        ├─ ...Insulation_Cold   ← 材料影响保温
        └─ ...Insulation_Heat   ← 材料影响隔热
```

材质本身是独立 `ThingDef`（`Stuff_*`，如钢 `Steel`、丝绸 `Cloth`），
在 `costList` 里以 `costList.<Stuff>` 出现（如 `costList.Steel 50`）。

### 2.3 制作（recipeMaker）

护甲和武器的制造挂在 `<recipeMaker>`：

| 字段 | 说明 |
|---|---|
| `recipeMaker.recipeUsers` | 哪些工作台能做（`CraftingSpot` / `ElectricTailoringBench` / `TableMachining`…） |
| `recipeMaker.researchPrerequisite` | 需要的项目 |
| `recipeMaker.workSkill` | 技能（原版护甲统一是 `Crafting`） |
| `recipeMaker.skillRequirements.Crafting` | 技能门槛 |
| `recipeMaker.unfinishedThingDef` | 中间产物（`UnfinishedBelt` 等） |
| `recipeMaker.displayPriority` | 制作菜单排序 |
| `recipeMaker.effectWorking` / `soundWorking` | 制作特效/音效（`Smith` 锻造 / `Sculpt` 雕刻） |

### 2.4 交易的边界

护甲用 `tradeTags` 标记能否交易：`Armor`（护甲）· `Clothing`（衣物）·
`BasicClothing`（基础衣物）等。60% 的护甲带 `tradeTags`。

---

## 3. 内容

### 3.1 护甲清单（115 件，按 DLC 与层）

数量：DLC 分布 **Core 33 · Royalty 31 · Biotech 19 · Ideology 13 · Anomaly 13 · Odyssey 6**。
官方中文标签 115/115 全有。

> 完整 115 行清单（含每件的继承链、层、部位覆盖）可用脚本生成：
> `python tools/game_defs/extract_defs.py --def ThingDef --source apparel --fields apparel.layers apparel.bodyPartGroups statBases.ArmorRating_Sharp`
> 这里只列有代表性的类别：

**动力甲 / 高级外骨骼（Royalty）**：护甲值最高档，带 `PsychicEntropyRecoveryRate` 等灵能修正，
部分带 `soundWear`/`soundRemove` 音效。

**防弹/工业护甲（Core）**：`ArmorRating_Sharp` 0.09~1.00 跨度最大的一类，
`tradeTags` 含 `Armor`，通常锁 `Spacer`/`Industrial` techLevel。

**宗教/文化服饰（Ideology）**：与 [ideology.md](ideology.md) 的 `Precept_Apparel` 联动，
部分 `canBeDesiredForIdeo` 开启，可作为偏好服装。

**工装 / 制服（Core）**：带 `defaultOutfitTags = Worker`，`SlaveSuppressionOffset` 常见。

**部位专精**：单 `bodyPartGroups` 的一件（如只有 `Overhead` 的帽子、只有 `Belt` 的腰带）。

### 3.2 武器清单（106 件）

数量：**Core 68 · Biotech 19 · Royalty 10 · Anomaly 5 · Odyssey 4**。官方中文标签 106/106 全有。

> 完整清单：`python tools/game_defs/extract_defs.py --def ThingDef --source misc/weapons --fields weaponTags damageType.default techLevel`

按 `weaponClasses` 主要分：近战（`Melee`）· 远程轻武器（`RangedLight`）· 远程重武器 ·
石器时代（`Neolithic`）· 中世纪（`Medieval`）等。

### 3.3 武器特性清单（61 个）

- **Royalty 19 个**：多为灵能相关（`PsychicSensitivityUp/Down`、`PsychicHalucination`…）
- **Odyssey 42 个**：覆盖更广的词条
- 全部有官方中文标签 + 描述

> 完整清单：`python tools/game_defs/extract_defs.py --def WeaponTraitDef`

---

## 4. 解读

### 4.1 品质与武器名

一把带品质 + 多个 `WeaponTraitDef` 的武器，显示名会由
`traitAdjectives`（最多三层）拼成。品质档位来自代码内置表，常见档位（结合官方中文）：
**破损 → 劣 → 普通 → 良好 → 优秀 → 大师作品 → 传奇**。
传奇品质**圣物武器必定是传奇**（见 [ideology.md](ideology.md) §4.5）。

### 4.2 护甲与身体部位的关系

护甲不是「全身一个减伤」——`bodyPartGroups` 决定它**盖住哪些部位**，
只有命中的部位才吃到对应的 `ArmorRating_*`。这也解释了为什么：
- 护肩只护肩甲、挡不住躯干伤害
- 全身甲的 `ArmorRating` 通常比同材质的局部甲高
- `OnSkin`（贴身）层影响被外层盖住时的判定顺序

### 4.3 保温 / 隔热是独立于护甲值的一整套

护甲除了挡伤害，还管温度：`Insulation_Cold`（抗冷）+ `Insulation_Heat`（抗热），
再叠加 **Hediff「过热 / 冻伤」**（见 [hediff.md](hediff.md)）。
热带/极地 mod 的装备平衡基本都压在这两个 Stat 上。

### 4.4 美感（Beauty）与心情

护甲基类默认 `Beauty -3`（多数护甲不好看）。
带 `SocialImpact` 修正的礼服、好看的工作装能改善心情——
这与 Ideology「多元文化」对服饰的态度、[needs-mood.md](needs-mood.md) 的心情系统联动。

### 4.5 贸易与 AI 意图

`tradeTags` 决定商人是否交易这件装备；`weaponClasses` 决定 AI 在战斗/任务中
把这件当近战还是远程用（与 Ideology 武器推崇的「5 种组合」对应，见 [ideology.md](ideology.md) §4.7）。

---

## 5. modder 常见坑

1. **武器/护甲都是 `ThingDef`**，靠 `<thingClass>` 和 `<apparel>`/`<tools>`/`<verbs>` 区分，
   没有 `WeaponDef` / `ApparelDef` 这种类型。别去找不存在的类型。
2. **护甲值写 `statBases.ArmorRating_*`，不是 `<armorRating>` 块**——1.6 已统一成 Stat。
3. **品质不是 Def**，靠继承带 `CompQuality` 的基类（`ApparelBase`）获得。
   纯护甲基类 `ApparelNoQualityBase` **不带**品质。
4. **同一 `ApparelLayer` 只能穿一件**。写 outfit 逻辑或生成护甲时，
   要考虑层冲突，别以为能叠。
5. **改护甲别改基类**。`ApparelBase` 被大量继承；单件调整用 `<Inherit>False</Inherit>`
   或新建子类。
6. **材质乘数（`StuffEffectMultiplier*`）容易漏**。只写 `ArmorRating_Sharp` 不写乘数，
   换材料时数值不会变，和原版手感不同。
7. **武器尖端朝右画**；`verbs.defaultProjectile` 指向的弹丸才有命中判定。
8. **WeaponTraitDef 只在 Royalty / Odyssey 存在**（Core 没有），
   引用 `PsychicSensitivity` 类 trait 前确认 DLC 已加载。
