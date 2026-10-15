# Defs / MayRequire / PatchOperations 参考

> 参考手册层。蒸馏自 rimworldwiki `Modding Tutorials` vetted 区，**不含仓库特定结论**。
> 本仓库的实操范式见 [xml-defs-and-patches.md](../xml-defs-and-patches.md)。
>
> 来源页（最后核对 2026-10-15）：
> [PatchOperations](https://rimworldwiki.com/wiki/Modding_Tutorials/PatchOperations) ·
> [MayRequire](https://rimworldwiki.com/wiki/Modding_Tutorials/MayRequire) ·
> [XML Defs](https://rimworldwiki.com/wiki/Modding_Tutorials/Defs)
>
> 所属 wiki 章节：Game Systems Guides

---

## 1. XML Defs

- Defs 放在 `Defs/` 下的 XML 里。**路径和文件名都不影响加载**（唯一例外是自己用了 `LoadFolders.xml`），
  可以随意组织；建议跟随原版惯例（`SoundDef` 放 `/SoundDefs`，`ThingDef` 放 `/ThingDef_*`）便于阅读。
- **原版 Defs 在 `<Steam>/common/RimWorld/Data`。强烈建议把整个 `Data` 目录挂进 IDE workspace**——
  这是查原版字段定义、抄模板最有效的手段。
- ⚠️ **wiki 的 `XML Defs` 页目前是占位符**：`Creating Custom Defs` 和
  `Referencing Defs from C#` 两节都只写了 `(Placeholder)`，**没有 Def 字段参考表**。
  需要字段定义时只能去原版 `Data` 目录的 XML 里查，或看 Outdated 区的
  [`ThingDef explained`](https://rimworldwiki.com/wiki/Modding_Tutorials/ThingDef)、
  [`Introduction to Def Classes`](https://rimworldwiki.com/wiki/Modding_Tutorials/Def_classes)（带过时标记）。

---

## 2. MayRequire

`MayRequire` 是随 Royalty DLC 引入的 **XML 属性**，让内容按 DLC / mod 是否加载来决定加载与否。

### 2.1 基础

```xml
<ThingDef MayRequire="Ludeon.RimWorld.Royalty,Ludeon.RimWorld.Ideology">  <!-- 两者都要在 -->
<StatDef ParentName="MeditationFocusBase" MayRequireAnyOf="Ludeon.RimWorld.Royalty,Ludeon.RimWorld.Biotech">
```

- 值是逗号分隔的 `packageId` 列表。
- `MayRequire` = **全部命中**才加载；`MayRequireAnyOf` = **任一命中**即加载。

DLC 的 `packageId`：

| DLC | packageId |
|---|---|
| Royalty | `Ludeon.RimWorld.Royalty` |
| Ideology | `Ludeon.RimWorld.Ideology` |
| Biotech | `Ludeon.RimWorld.Biotech` |
| Anomaly | `Ludeon.RimWorld.Anomaly` |
| Odyssey | `Ludeon.RimWorld.Odyssey` |

### 2.2 用在哪几种位置

| 位置 | 效果 |
|---|---|
| 任意 `<li>` 节点 | 该条目仅在条件满足时加载 |
| Def 根节点 | 整个 Def 仅在条件满足时加载（**比用 PatchOperation 注入新 Def 干净得多**） |
| 字段/列表项的 Def 引用 | 抑制跨引用报错，见下方特例 |
| `<statBases>` 等「特殊解析列表」 | 按条件包含该条目 |

> **Def 引用的特例行为**：在「直接引用字段」或「直接引用列表」上用 `MayRequire`，
> 它**只起到抑制「找不到目标 Def」报错的作用**——一旦目标 Def 确实存在（哪怕是因为别的 mod 加的），
> **照样会被加进列表**，与声明的 DLC 无关。
> 这个特例**不适用于「特殊解析列表」**（stat block、物品数量表等），那些是真正的条件加载。

### 2.3 陷阱

- **`MayRequire` 对非 Def 的顶层 XML 节点无效**。也就是说 `<Operation>` 标签本身
  **不能**挂 `MayRequire`（`PatchOperationSequence` 里的 `<li>` 可以，但 wiki 明确不推荐，见下）。
- **1.6 之前有 `_steam` 后缀 bug**：本地 mod 与 Steam 同 `packageId` 并存时，
  `MayRequire` 只认本地那份。绕过办法是 `MayRequireAnyOf="MyName.MyMod,MyName.MyMod_steam"`。
  **1.6 起已修复，不需要这个写法。**
- 原版里有两处 `MayRequire` 挂在非引用字段上（`PawnKindDef` 的 `apparelIgnorePollution`、
  `LifeStageDef` 的 `workerClass`），wiki 分析代码后认为**这两处实际不生效**，大概率只是当标记用。

---

## 3. PatchOperations

在 alpha 17 之前，mod 只能「重新定义整个 Def 来覆盖」，两个 mod 同时覆盖同一 Def 时
只有加载顺序里靠后的成功——这就是兼容问题的根源。PatchOperation 解决了它。

### 3.1 结构

补丁放 `Patches/` 下的 XML，根标签 `<Patch>`。文件名任意。

```xml
<?xml version="1.0" encoding="utf-8"?>
<Patch>
  <Operation Class="PatchOperationAdd">
    <xpath>Defs/ThingDef[defName="Wall"]/statBases</xpath>
    <value><Mass>10</Mass></value>
  </Operation>
</Patch>
```

### 3.2 XPath 定位规则

XPath 匹配的是 **RimWorld 解析后的 XML 文档结构**，跟磁盘路径无关。

- 面向 Def 的 XPath **第一段永远是 `Defs/`**。
- 方括号是谓词（条件匹配）：`Defs/ThingDef[defName="Wall"]/statBases`。
- **没有 `defName` 的抽象基类**用属性定位：
  - `Defs/ThingDef[@Name="ShelfBase"]/stuffCategories`
  - `Defs/ThingDef[@ParentName="ApparelBase"]` ← 命中所有继承 ApparelBase 的 ThingDef
- **XPath 和节点名大小写敏感**，`defName` 之类直接从原版 XML 复制，别手打。

### 3.3 操作类型速查

| 类 | 作用 | 默认顺序 |
|---|---|---|
| `PatchOperationAdd` | 把 `value` 作为**子节点**插入目标 | `Append`（追加到末尾），可 `<order>Prepend</order>` |
| `PatchOperationInsert` | 把 `value` 作为**兄弟节点**插到目标上方 | `Prepend`（插到前面），可 `<order>Append</order>` |
| `PatchOperationRemove` | 删除目标节点 | — |
| `PatchOperationReplace` | 用 `value` 替换目标节点 | — |
| `PatchOperationAttributeAdd` | 加属性，**仅当该属性尚不存在** | — |
| `PatchOperationAttributeSet` | 加属性，**已存在则覆盖** | — |
| `PatchOperationAttributeRemove` | 删属性 | — |
| `PatchOperationSequence` | 顺序执行一组操作，**任一失败即中止** | — |
| `PatchOperationAddModExtension` | 加 ModExtension，自动建 `<modExtensions>` | — |
| `PatchOperationSetName` | 改节点名（用于 stat block / 配方产物这类「字典式」节点） | — |
| `PatchOperationFindMod` | 检测其他 mod 是否加载，分支执行 | — |
| `PatchOperationConditional` | 检测节点是否存在，分支执行 | — |
| `PatchOperationTest` | 检测 xpath —— **已废弃** | — |

> ⚠️ **Add vs Insert 是最高频的困惑点**：
> `Add` 把 value 插成目标的**子节点**，`Insert` 插成目标的**兄弟节点**。

### 3.4 条件分支

```xml
<!-- 检测 mod 是否加载。命中走 match，未命中走 nomatch -->
<Operation Class="PatchOperationFindMod">
  <mods><li>RimQuest</li></mods>
  <match Class="PatchOperationAddModExtension">…</match>
</Operation>

<!-- 检测节点是否存在（用于「先建容器再填内容」） -->
<Operation Class="PatchOperationConditional">
  <xpath>Defs/WorldObjectDef[defName="Caravan"]/comps</xpath>
  <nomatch Class="PatchOperationAdd">
    <xpath>Defs/WorldObjectDef[defName="Caravan"]</xpath>
    <value><comps /></value>
  </nomatch>
</Operation>
```

⚠️ **`PatchOperationFindMod` 是唯一的例外**：它匹配 mod 的 **`<name>` 名字**，
**不是 `packageId`**（其他所有兼容机制都用 `packageId`）。
DLC 则用 `ExpansionDef` 的 `label` 值，即单词：`Royalty` / `Ideology` / `Biotech` / `Anomaly`。

因此：mod 名一旦改动，`PatchOperationFindMod` 就会失效——这也是 wiki 建议别改 `<name>` 的原因。

### 3.5 三条重要规则

1. **补丁在所有 XML Def 载入内存后执行**，顺序 = mod 列表顺序。
   与别人的 PatchOperation 冲突时，用 `About.xml` 的 `loadBefore` / `loadAfter` 引导玩家排顺序。
2. **补丁在 Def 继承展开之前执行** →
   - 你**无法**定位从父类继承来的标签；
   - 但改父类会让**所有子 Def 一起继承**你改的值。
3. 想只改一个子 Def 而不动其他继承者，加 `Inherit="False"`：

   ```xml
   <Operation Class="PatchOperationAdd">
     <xpath>/Defs/ThingDef[defName="Apparel_KidPants"]</xpath>
     <value><thingCategories Inherit="False"><li>NewValue</li></thingCategories></value>
   </Operation>
   ```

### 3.6 已废弃的写法

- **`<success>` 节点**：`Always` / `Normal` / `Invert` / `Never`。
  它早于 `PatchOperationConditional`，现在**不应再需要**，且 `Always` 会掩盖真实错误。
- **`PatchOperationTest`**：同样被 `PatchOperationConditional` 取代。
- **`PatchOperationSequence`**：wiki **明确不推荐**用它做条件控制——
  序列会**隐藏子操作的报错**，让调试变困难。若只是想控制「哪些版本加载哪些文件」，
  **用 `LoadFolders.xml`**（见 [mod-project-setup.md](mod-project-setup.md) §3.1）。

> 换句话说：`PatchOperationSequence` 只在「必须把若干操作绑成一个原子单元」时才值得用。

### 3.7 技巧

- 谓词里可以用 `or` 一次命中多个目标，比写多个操作好（前提是对每个目标做同样的改动）：
  `Defs/ThingDef[defName="Cassowary" or defName="Emu" or defName="Ostrich"]`
- 用 `/text()` 只定位标签的**文本内容**而非整个标签，适合不想误删属性的替换。
- 自定义 PatchOperation：继承 `Verse.PatchOperation`，可按 ModSettings 等自定义逻辑打补丁。
  参考 [XML Extensions](https://github.com/15adhami/XmlExtensions)（⚠️ **GPLv3，传染**）、
  [CombatExtended 的 `PatchOperationMakeGunCECompatible`](https://github.com/CombatExtended-Continued/CombatExtended/blob/Development/Source/CombatExtended/CombatExtended/PatchOperationMakeGunCECompatible.cs)。

### 3.8 排错

| 症状 | 原因 |
|---|---|
| 启动白屏 /「Recovered from a fatal error」/ 所有 mod 被卸 | XML 格式错误（标签未闭合等）。**所有 XML 都要过验证器**；查 `Player.log` |
| Def 加载失败报红 | 同级出现**重名非-`li` 节点**。除 `<li>` 外，同级 XML 节点必须唯一 |
| 补丁没生效 | 检查 XPath 拼写与**大小写**；确认 `defName` 值抄对了 |

---

## 4. Apparel Tags（附录）

服装标签是**任意字符串**，不是 Def，**无需预先声明**即可使用。
用于控制 pawn 生成时的着装选择。自定义标签**务必加唯一前缀**防冲突。

核心组：`Neolithic`、`MedievalMilitary`、`Western`、`IndustrialBasic`、`IndustrialAdvanced`、
`IndustrialMilitaryBasic`、`IndustrialMilitaryAdvanced`、`SpacerMilitary`、`Cape`、
`BestowerHood`、`Robe`、`BeltDefensePop`、`BeltDefense`。

DLC 组：`Royal` / `RoyalTier2..7` / `RoyalRobe` / `Psychic` / `PrestigeCombatGear` /
`Gunlink` / `PackJump`（Royalty）；`Mechlord` / `Apparel_Yttakin` / `BeltDefenseTox`（Biotech）；
`Horaxian` / `HoraxianCeremonial`（Anomaly）；`Vacsuit`（Odyssey）。

部分原版服装**故意不带标签**（如 tribal headdress、cataphract armor、headwrap 等），
原版另有特殊标签用于判断 pawn 状态（如 Royalty 贵族适用性）。

> 完整逐条对照表见
> [Apparel Tags](https://rimworldwiki.com/wiki/Modding_Tutorials/Apparel_Tags)（该页同样标注为 Placeholder，
> 但表格数据是全的）。
