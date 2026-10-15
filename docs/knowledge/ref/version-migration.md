# 版本迁移参考：1.5 / 1.6 / 1.7

> 参考手册层。蒸馏自 rimworldwiki `Modding Tutorials` 的 Updates and Migrations 区
> + Testing Mods + 顶层的 Steam Workshop 上传段，**只记事实，不含仓库特定结论、不含改造 TODO**。
>
> 来源页（最后核对 2026-10-15）：
> [1.7 Mod Updates](https://rimworldwiki.com/wiki/Modding_Tutorials/RimWorld_1.7_Mod_Updates) ·
> [1.6 Mod Updates](https://rimworldwiki.com/wiki/Modding_Tutorials/RimWorld_1.6_Mod_Updates) ·
> [1.5 Mod Updates](https://rimworldwiki.com/wiki/Modding_Tutorials/RimWorld_1.5_Mod_Updates) ·
> [Testing mods](https://rimworldwiki.com/wiki/Modding_Tutorials/Testing_mods) ·
> [Modding Tutorials](https://rimworldwiki.com/wiki/Modding_Tutorials)（Workshop 段）

---

## 0. 三代总览

| | 1.5 (Anomaly) | 1.6 (Odyssey) | **1.7 (Waylith)** |
|---|---|---|---|
| 运行时 | Unity 2022.3 | Unity 2022.3 | **Waylith 自研引擎 + Vulkan** |
| 目标框架 | net472 | net472 | **net10.0** |
| 引用 DLL | Assembly-CSharp + UnityEngine\* | 同左 | Assembly-CSharp + **UnityStubs** + **Waylith** |
| csproj 格式 | 传统 | 传统 | **SDK-style** |
| Harmony | 2.x | 2.x | **3.0**（强制类型检查） |
| 调试 | Doorstop / Rider 插件 | 同左 | **IDE 直接附加** |

⚠️ 三份迁移页都有剧透警告：1.5 是 Anomaly，1.6 是 Odyssey。
⚠️ 三份页面的「1.7 Update Specifics（Placeholder）」目前**仍是空的**——1.7 的具体 API 变更清单尚未整理。

---

## 1. 通用迁移流程（1.5 / 1.6 页给出的标准步骤）

1. 更新 `About.xml` 的 `supportedVersions`。
2. 考虑用 `LoadFolders.xml` **冻结旧版本内容**，让老版本仍走原分支（1.6/1.7 页都明确建议）。
3. 重新编译程序集，逐个解决编译错误。
4. 加载 mod，**从上到下处理错误列表**。
5. 检查功能——**这时正好建一份功能清单**，以后每次大版本升级复用。
6. **持续回看后续补丁**。首发后几个月内仍会出改动，**不要假设「更新完就再也不会坏」**。

---

## 2. RimWorld 1.7（Waylith）

### 2.1 引擎替换的真实影响

- 1.7 跑在**全新自研引擎 Waylith** 上，靠一个**自制 shim 把旧的 Unity 调用翻译成 Waylith 调用**。
- **「99% 的 1.6 mod 开箱即用，连重编译都不用」**（官方原话）。
  换言之 1.7 的破坏性远小于 1.5、1.6 那两次大版本。
- **唯一确认的破坏点：Prepatcher 失效**，因为 Prepatcher 本身需要先适配 1.7。
- 官方表示：把代码改成直接调用**原生 Waylith 方法**（而非 Unity stub）会有明显性能提升。

### 2.2 程序集迁移到 .NET 10

| 步骤 | 说明 |
|---|---|
| IDE | 必须 **VS2026** 或 **Rider 2026**。**VS2022 和 2026 之前的 Rider 不支持 .NET 10** |
| csproj | 传统格式必须迁移到 **SDK-style** |
| 目标框架 | `net10.0`——**`.0` 不能省** |
| 清理引用 | **从 csproj 移除所有 Unity DLL 引用**（`.cs` 里的 `using UnityEngine;` 留着不动） |
| 新增引用 | `Assembly-CSharp.dll`、`UnityStubs.dll`、`Waylith.dll` |
| 其他库 | 所有框架/库引用也都要为新引擎重新编译 |

两个防呆属性：

```xml
<AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
<!-- 否则 Assemblies/ 里会多出一层 net10.0/ 目录 -->

<GenerateDependencyFile>false</GenerateDependencyFile>
<!-- 抑制 .deps.json——游戏会忽略它 -->
```

**csproj 迁移工具**：
- Visual Studio 扩展 **Upgrade Assistant**（GUI）
- 命令行：`upgrade-assistant upgrade project.csproj -o feature.sdkstyle -o Inplace`
- 第三方：<https://github.com/hvanbakel/csprojtovs2017>

> 迁完如果报「重复的 `System` 属性」之类错误——
> **删掉 `Source/Properties/AssemblyInfo.cs`**，SDK-style 格式下不需要它。

**取巧办法**：新建一个空白 `net10.0` 项目，把旧项目的文件覆盖回去。

### 2.3 Harmony 3.0（破坏性最强的一条）

- 1.7 **强制要求 Harmony 3.0**。NuGet 引用更新到 **`3.0-rc2`**。
- ⚠️ **Harmony 3 对注入参数做类型检查**：`__instance`、`__result` 等的类型必须严格匹配，
  **不匹配会直接拒绝打补丁**。1.6 时代能"歪着打上"的补丁在 1.7 会失败。

### 2.4 日志路径变更

```
C:\Users\[用户名]\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Logs\Player.log
```
旧路径下多了一个 `Logs` 子目录。

### 2.5 调试方式变更

- **Doorstop 不再需要**——游戏已不用 Unity，IDE 直接附加进程即可调试。
- 新增开发者模式工具：**Frame Profiler**（Actions/tools → General → Frame Profiler）。
- 新增命令行参数 **`-mod=`**：用逗号或分号分隔 mod id 列表启动游戏，
  游戏会连同本体和所有已装 DLC 一起加载，并**按游戏内 mod 管理器的同套逻辑排序**。
  做多 mod 联调 / 复现兼容问题非常有用。

### 2.6 着色器与 Asset Bundle

- 1.6 编的 shader（打包在 asset bundle 里）**继续可用**。
- **新 shader 必须用 GLSL 写**（不是 Unity ShaderLab）。
- GLSL shader **不再需要预编译**，源码直接放游戏目录，由 Waylith 在加载时编译。
- ⚠️ **因为用 Vulkan，1.7 只会加载你提供的 shader 的 Linux 版本**。
  以前没给 Linux 构建过 shader 的 mod，1.7 下 Windows 专用 shader 会加载失败。
- 1.6 的 asset bundle 继续可用，但 **1.7 里已不再必要**——首次加载会缓存资源，之后无论有没有
  asset bundle 都快很多。

### 2.7 官方文档

Ludeon 直接提供的
**[RimWorld 1.7 Modder Primer](https://docs.google.com/document/d/e/2PACX-1vSykw5_DYaaw42nEznI9zmBZVT_Vgg2bxYq_v2XWvAsHC3QSKFuDK2OyiQ5WC9jvVRUcrjpFt7xLLF5/pub)**
（Google Doc）——wiki 说后续细节会补进这份文档。

---

## 3. RimWorld 1.6（Odyssey）

### 3.1 官方文档

- [1.6 Change Log](https://docs.google.com/document/d/e/2PACX-1vRCjqVtPQDFGu4POiKTUd_8o3U2Asdhx99SOvcgU66ABdYtk3Cgndd53yJ6BC4tZX530pp_m6lf4Z9P/pub)
- [1.6 Modder Primer](https://docs.google.com/document/d/e/2PACX-1vRKE9u5ZW_zG45pxzwNvy4sxvozDeqtxlxpac5jwenOeW6liQCPgmPl9bIbtcMuqL1NPIDHOLFg64M_/pub)

### 3.2 字段改名 / 移除

| 旧 | 新 |
|---|---|
| `BuildingProperties.placingDraggableDimensions` | `drawStyleCategory`（`DrawStyleCategoryDef`） |
| `DraggableDimensions`（designator 虚属性） | `DrawStyleCategory` |
| `BuildingProperties.soundAmbient` | **已移除**，改用 `CompProperties_AmbientSound` |
| `HediffDef.causesNeed` | 药物用根节点 `<chemicalNeed>`；非药物用 `HediffStage` 的 `<enablesNeeds>` 列表 |
| `HediffDef.disablesNeeds` | 移到 `HediffStage` |
| `TerrainDef.holdSnow` | `holdSnowOrSand` |
| `PlantDef.hideAtSnowDepth` | `hideAtSnowOrSandDepth` |
| `InteractionUtility` | `SocialInteractionUtility` |
| `ActiveDropPodInfo` | `ActiveTransporterInfo` |
| `<canTargetMutants>` | `<canTargetSubhumans>` |
| PawnFinder 里带 `TransportPods` 的列表名 | 改 `Transporters`（如 `AllMapsCaravansAndTravelingTransporters_Alive_Colonists`） |

### 3.3 新增

- `RaceProperties.forceGender`、`RaceProperties.wildness` 改为 `statBases` 里的 `Wildness` StatDef（首字母大写）。
- `DamageDef.ignoreShields`——忽略护盾腰带（但**不**绕过投射物拦截器）。
- `ScenarioBase` 抽象基类，`ScenarioDef` **必须继承它**，否则加载报错。

### 3.4 环境音效改法（实际例子）

`soundAmbient` 在 1.6 被移除，改成 Comp：

```xml
<li Class="CompProperties_AmbientSound">
  <sound>Toxifier_Working</sound>
  <disabledOnUnpowered>true</disabledOnUnpowered>
</li>
```

（wiki 直接引用了 Toxifier Generator 作为示例——本仓库 30 号 mod 正是同一目标。）

### 3.5 反编译源码时的坑

把反编译导出的 `.cs` 丢进 Visual Studio 后频繁崩溃/卡死时：
**删掉 `Properties/AssemblyInfo.cs` 里所有含 `BurstCompiler` 的行**。

### 3.6 Odyssey DLC 锁定内容（data-mine 得来，可能与最终版有出入）

Gravships、穿梭机、地标/Tile Mutator/多种 GenStep、太空与陨石任务、氧气与真空、
捕食性栖息地动物、沿海动物、钓鱼、混合生物群系地图、
动物能力扩展（采集 / 定向攻击 / 感知）、
新天气（干旱、冰封水体、洪水、沙尘暴）、植物栽培、
新任务给予者（商人/乞丐/书籍/轨道扫描器 ?）、Substructure、
新教条（钓鱼、太空、殖民地搬迁）。

---

## 4. RimWorld 1.5（Anomaly）

### 4.1 建筑与家具

- 原版新增壁挂灯；`BuildingProperties.isAttachment` 支持壁挂。
- 原版新增隐藏导管。
- `Building_MultiTileDoor`（华丽门）。
- `Dialog_Rename` 改为针对 `IRenamable` 的抽象泛型，参考 `Dialog_RenameArea`。
- `ShelfBase` 拆成 `StorageShelfBase` 和 `BookcaseBase`。
- 新搬运接口 `IHaulEnroute`：允许多个 pawn 搬往同一目的地。

### 4.2 Thing / 渲染

- `Thing.Draw` 移除 → `Thing.DynamicDrawPhase`；投射物改用 `DrawAt`。
- `MapMeshFlags` 从枚举改成 **Def**。
- `RimWorld.HiddenItemsManager` 管理未发现物品，配 `<hiddenWhileUndiscovered>`；
  未发现物品不会出现在储存设置里。

### 4.3 Pawn 渲染（1.5 最大的重写）

`PawnRenderer` 完全重做：

- 新增 `PawnRenderTree`、`RenderNodes`、`RenderNodeWorkers`（Ludeon 有[官方文档](https://docs.google.com/document/d/e/2PACX-1vRpvIu49_CpHFMCk-8vLuzIsLqbFFDnf0DNFlYRpwAYXjNlYH6ZudPoox7Gj6K8wuCLPDCWkWEGARrO/pub)）
- 装备与服装的动态渲染改由 `PawnRenderUtility.DrawEquipmentAndApparelExtras` 处理，不再在 `PawnRenderer` 里。

`ApparelProperties` 变更：

- 新增：`<renderSkipFlags>`、`<drawData>`、`<parentTagDef>`
- 移除：`<hatRenderedFrontOfFace>`、`<hatRenderedAboveBody>`、`<hatRenderedBehindHead>`、
  `<shellRenderedBehindHead>`、`<forceRenderUnderHair>`、`<coversHeadMiddle>`、
  `<shellCoversHead>`、`<countsAsMaskForBreathing>`

### 4.4 身体与 Pawn 数据

- `BodyDef` 改动：舌头覆盖度 `0` → `0.001`；眼部新增标签；
  左眼/左耳/左肩/锁骨/左臂/左手新增 `flipGraphic` 标签。
- `BodyPartDef` 新增 `<executionPartPriority>`，决定处决时的破坏顺序；
  `ExecutionUtility.ExecuteCutPart` 改用它而非硬编码列表。许多部位新增 `Mirrored` 标签。
- 特质（Trait）的 `<degreeDatas>` 列表项新增 possessions 字段——**开局时带上额外物品**。
- 背景故事移除 `<baseDesc>`，改用 `<description>`。
- **技能改用自定义 XML 解析**，可用正则批量迁移：
  `<li>\s*<key>(.*)</key>\s*<value>(.*)</value>\s*</li>` → `<$1>$2</$1>`
  ⚠️ **注意要把 `<li>` 本身也删掉**。
- 尸体 Def 现在会从来源种族复制 `race` 属性——**原来只用 `def.race != null` 判断 ThingDef
  是不是种族的代码会误判**，现在还要加 `!def.IsCorpse`。
- `FleshTypeDef` 新增 `<isOrganic>`，`RaceProperties.IsFlesh` 改用它而不是硬编码判断机械体。
- pawn 生成时，背故事战利品由 `BackstoryThingDefCountClass` 改为 `PossessionThingDefCountClass`。

### 4.5 RaceProperties 新增字段

`deathAction`（原 `deathActionWorkerClass`，改为 `DeathActionProperties` 对象以便缓存）、
`<doesntMove>`、`<renderTree>`（`PawnRenderTreeDef`）、`<startingAnimation>`、
`<linkedCorpseKind>`、`<canOpenFactionlessDoors>`、`<alwaysAwake>`、`<alwaysViolent>`、
`<isImmuneToInfection>`、`<bleedRateFactor>`（默认 1.0）、`<canBecomeShambler>`（默认 false）、
`<disableIgniteVerb>`、`<detritusLeavings>`、`<overrideShouldHaveAbilityTracker>`、
`<hasMeat>`（false 则不生成肉）、`<hasCorpse>`（false 则不生成尸体）、
`<corpseHiddenWhileUndiscovered>`、`<soundMoving>`、`<bloodSmearDef>`。

> ⚠️ **`<bloodDef>` 不再默认回落到 `Filth_Blood`**。留空则：健康页不显示流血、
> 不会流血致死、受伤时不产生污渍。

其他行为变化：`ShouldHaveAbilityTracker` 现在除 `Humanlike` / `IsMechanoid` 外还读
`<overrideShouldHaveAbilityTracker>`；`Animal` 属性判定除 `!ToolUser && IsFlesh` 外还检查
`!IsAnomalyEntity`。

### 4.6 类迁移与签名变更

| 旧 | 新 |
|---|---|
| `ManhunterPackIncidentUtility` | `AggressiveAnimalIncidentUtility` |
| `IncidentWorker_ManhunterPack` | `IncidentWorker_AggressiveAnimals` |
| `StatPart_ApparelStatOffset` | `StatPart_GearStatOffset`（另新增 `StatPart_GearStatFactor`） |
| `MapMeshFlag`（枚举） | `MapMeshFlagDef`（带 `index`，枚举值移到 `MapMeshFlagDefOf`） |
| `SerializablePawnList` | `List<Pawn>` |
| `Verse.IngredientValueGetter` | `RimWorld.IngredientValueGetter` |
| `Verse.DebugActionAttribute` 及一批 Debug 相关类 | `LudeonTK.*` |
| `CompUseEffect.CanBeUsedBy(Pawn, out string)` | `AcceptanceReport CanBeUsedBy(Pawn)` |

寻路相关：`Pawn.TicksPerMoveCardinal`、`Pawn.TicksPerMoveDiagonal`、
`Pawn_PathFollower.CostToMoveIntoCell` 均从 **int 改为 float**。

### 4.7 StatDef / ResearchTabDef 新字段

- `StatDef`：`<offsetLabel>`、`<showOnEntities>`（默认 true）、`<showOnNonPowerPlants>`（默认 true）、
  `<overridesHideStats>`（默认 false）、`<displayMaxWhenAboveOrEqual>`（默认 false）
- `ResearchTabDef`：`<generalTitle>`、`<generalDescription>`、`<visibleByDefault>`（默认 true）、
  `<minMonolithLevelVisible>`（默认 -1，Anomaly 相关）、`<tutorTag>`

### 4.8 DLC 依赖归属变化

- **Skulls**：骷髅 ThingDef 从 Ideology 迁到 Core；「提取骷髅」的 Designation/WorkGiver/Job
  也迁到 Core（仍需正确教条或 Anomaly 研究解锁）。
- **StatDef**：一批战斗/通用属性从 Biotech 迁到 Core，不再需要 DLC。
  但 `Stats_Pawns_General` 那批**后来又回归**，现在仍需 Biotech 或 Anomaly。
  `MechStatBase`、`EMPResistance` 已废弃删除。
- **Robes**：长袍从 Ideology 迁到 Core，**无需任何 DLC 即可制作**。

---

## 5. 测试与调试

### 5.1 Development Mode

主菜单 `Options → Gameplay → Development Mode`。**开启后一直保持**，关掉游戏再开仍然有效。

1.6（Odyssey）起运行时是 Unity 2022.3.35 + Unity 分支的 Mono。

### 5.2 调试器挂载

- **Rider**：装 [RimWorld Development Environment](https://plugins.jetbrains.com/plugin/21583-rimworld-development-environment) 插件即可。
- **非 Rider**：手动装 [Rimworld-Doorstop](https://github.com/pardeike/Rimworld-Doorstop)，
  或用 RimWorld4Debugging 向导。
- **1.7 起**：不再需要 Doorstop，IDE 直接附加进程。

### 5.3 按内容类型的测试命令

| 内容 | 命令 |
|---|---|
| 武器 / 服装 | `Spawn weapon` / `Spawn apparel`；另有输出生成概率的调试命令（排查 apparel tag 是否配对） |
| Pawn | `Spawn pawn`——会列出游戏里**所有** PawnKindDef |
| 事件 | `Incident` 系列命令，含袭击 |
| Job / AI | `Toggle job logging`、`Draw pawn debug` |
| Lord（群体 AI 控制器，从商队到袭击队都是） | View settings → `Draw Duties`（每个 pawn 在做什么）、`Draw Lords`（Lord 在做什么）、`Log Lord Toil Transitions`（转换过程） |

### 5.4 快速测试

- 开启 dev mode 后，主菜单出现 **Quicktest** 选项，直接进 Crashlanded。
- 命令行参数 **`-quicktest`** 启动即进：

  ```sh
  C:/Path/To/Game/RimWorld.exe -quicktest
  /Users/.../RimWorldMac -quicktest          # macOS
  ```

  可以给游戏做个带该参数的快捷方式。
- 放一个名为 **`autostart.rws`** 的存档文件，启动即自动载入。

### 5.5 存档目录改写

**`-savedatafolder=<路径>`** 覆盖存档位置（可把游戏装在 U 盘上随处运行），支持相对路径：

```sh
C:/Path/To/Game/RimWorld.exe -savedatafolder=C:/Path/To/The/Folder
```

⚠️ 要确保游戏对该目录有写权限。与 `-quicktest` 组合可得到干净的 modding 环境。

### 5.6 上传辅助工具

**PublisherPlus**（原 Jaxe 创建，NightmareCorporation 维护分支）——
上传到工坊时可**排除指定文件/目录**。（多个版本兼容的 fork 版本见工坊。）

### 5.7 排查加载了哪些文件

开 Development Mode 后，主菜单有 debug action **`Loaded Files For Mod`**，
列出任意 mod 实际加载的完整文件路径表。排查「补丁没生效」的首选工具。

---

## 6. 上传 Steam Workshop

1. `Options` 里启用 Development Mode。
2. 原版 mod 管理器 → `Advanced` 按钮 → `Upload`。
3. ⚠️ **必须持有 Steam 版的 RimWorld**，GOG / Epic 版无法上传。
4. **Preview.png**：640×360 或 1280×720 PNG，**必须 < 1MB**。
   超限直接拒收，报 `Error: Limit Exceeded`。
5. `OnItemSubmitted Fail`：
   - 关掉所有正在访问 mod 目录的程序；
   - 也可能是工坊服务端临时故障——**持续出现就只能等几小时**。
6. 工坊描述**不用 markdown**，用 BBCode 变体，参考
   [Steam 文本格式指南](https://steamcommunity.com/comment/Guide/formattinghelp)。

> 本仓库的发布约定与 `_PublisherPlus.xml` 用法见 [publishing-and-release.md](../publishing-and-release.md)。
