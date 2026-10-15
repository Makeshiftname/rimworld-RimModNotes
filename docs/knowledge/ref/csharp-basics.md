# C# 基础参考：应用启动顺序 / Pawn 生成流程

> 参考手册层。蒸馏自 rimworldwiki `Modding Tutorials` vetted 区，**不含仓库特定结论**。
> 打补丁的时机与技法见 [harmony-patching.md](../harmony-patching.md)。
>
> 来源页（最后核对 2026-10-15）：
> [Application Startup](https://rimworldwiki.com/wiki/Modding_Tutorials/Application_Startup) ·
> [Pawn Generation Process](https://rimworldwiki.com/wiki/Modding_Tutorials/Pawn_Generation_Process)
>
> 所属 wiki 章节：C# Guides
>
> 🔴 **两页都带同一条警告**（务必遵守）：
> 这两页是用来诊断「mod 代码跑得太早或太晚」的**顺序参照**，
> **不要 Harmony patch 这些方法来做自定义加载**——
> 游戏对每类数据都有正规的「在正确时机执行」机制，应该用那些。
> Pawn 生成更是 RimWorld 里**性能最敏感**的系统，改它要格外小心。

---

## 1. 应用启动顺序

入口：`Verse.Root.Start()` → **`Verse.PlayDataLoader.LoadAllPlayData(bool)`**

### 1.1 Def 加载与解析（步骤 1–15）

| # | 做什么 | 代码位置 |
|---|---|---|
| 1 | 载入 mod 配置（激活列表） | `Verse.ModsConfig` 构造函数 |
| 2 | 从 mod 文件夹重建 mod 列表 | `Verse.ModLister.RebuildModList()` |
| 3 | 初始化 mod | `Verse.LoadedModManager.InitializeMods()` |
| 4 | 加载 mod 程序集（同时创建 `Mod` 子类实例）；**音频/贴图/字符串/asset bundle 此时只是排队**，真正执行在第 30–33 步 | `LoadedModManager.LoadModContent()` / `CreateModClasses()` |
| 5 | 加载并解析 Def XML | `LoadedModManager.LoadModXML()` |
| 6 | 把所有 Def 合并成**单一 XML 文档** | `LoadedModManager.CombineIntoUnifiedXML()` |
| 7 | 从 Def 里加载翻译键（极少用到） | `Verse.TKeySystem.Parse()` |
| 8 | 加载并校验补丁 | `LoadedModManager.ErrorCheckPatches()` |
| 9 | **应用 XML PatchOperations** | `LoadedModManager.ApplyPatches()` |
| 10 | 注册 Def 继承关系（`Name` / `ParentName`） | `Verse.XmlInheritance.TryRegister()` |
| 11 | 应用 Def 继承 | `LoadedModManager.ParseAndProcessXML()` |
| 12 | 把 XML 载入 Def 类。**跨 Def 引用此时只注册，不解析**（第 20 步才解） | `LoadedModManager.ParseAndProcessXML()` |
| 13 | 补丁失败则警告 | `LoadedModManager.ClearCachedPatches()` |
| 14 | 载入语言元数据 | `Verse.LanguageDatabase.InitAllMetadata()` |
| 15 | 把 Def 存进 `DefDatabase` | `GenGeneric.InvokeStaticMethodOnGenericType() → AddAllInMods` |

> 💡 **第 9 步早于第 10–11 步**——这就是为什么
> **补丁发生在 Def 继承展开之前**（详见 [defs-and-patches-ref.md](defs-and-patches-ref.md) §3.5）。

### 1.2 翻译与隐式 Def（步骤 16–22）

| # | 做什么 | 代码位置 |
|---|---|---|
| 16 | 把 Def 存进 `DefOf` 类（**宽松模式**，找不到不报错） | `RimWorld.DefOfHelper.RebindAllDefOfs(true)` |
| 17 | 建立翻译键映射 | `Verse.TKeySystem.BuildMappings()` |
| 18 | 注入 DefInjected 翻译（**第一轮**） | `LanguageDatabase.activeLanguage.InjectIntoData_BeforeImpliedDefs()` |
| 19 | 生成隐式 Def：Blueprint、Meat、Frames、Techprints、Corpses、石质地形、由 recipeMakers 派生的 Recipe、Neurotrainer | `Verse.DefGenerator.GenerateImpliedDefs_PreResolve()` |
| 20 | **解析跨引用**——把注册过的跨引用替换成指向实际 Def 的直接引用 | `DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences()` |
| 21 | 再次把 Def 存进 `DefOf`（**这次找不到会报错**） | `RimWorld.DefOfHelper.RebindAllDefOfs(false)` |
| 22 | 重载玩家知识库（Learning Helper 条目） | `PlayerKnowledgeDatabase.ReloadAndRebind()` / `LessonAutoActivator.Reset()` |

### 1.3 引用解析与静态数据（步骤 23–29）

| # | 做什么 | 代码位置 |
|---|---|---|
| 23 | 重置所有静态数据 | `LoadedModManager.DoPlayLoad()` |
| 24 | 解析引用（对所有 Def 调 `ResolveReferences`），**顺序为**：ThingCategoryDefs → RecipeDefs → 其余 Def → ThingDefs | `DefDatabase<ThingCategoryDef>.ResolveAllReferences(true,true)` … `DefDatabase<ThingDef>.ResolveAllReferences()` |
| 25 | 生成隐式 Def（按键绑定） | `RimWorld.DefGenerator.GenerateImpliedDefs_PostResolve()` |
| 26 | 再重置一部分静态数据，确保 smooth 机制就绪 | `LoadedModManager.DoPlayLoad()` |
| 27 | Dev mode 下输出全部配置错误 | `GenGeneric… → ErrorCheckAllDefs` |
| 28 | 载入按键绑定 | `Verse.KeyPrefs.Init()` |
| 29 | 分配短哈希 | `Verse.ShortHashGiver.GiveAllShortHashes()` |

> 💡 **ThingDef 的引用解析排在最后**。依赖其他 Def 已经就绪的逻辑，
> 最稳的时机是这里之后。

### 1.4 内容与收尾（步骤 30–38）

| # | 做什么 | 代码位置 |
|---|---|---|
| 30 | 加载 mod 音频 | `LoadedModManager.LoadModContent() → ModContentPack.ReloadContent() → ReloadContentInt()` |
| 31 | 加载 mod 贴图 | 同上 |
| 32 | 加载 mod 字符串 | 同上 |
| 33 | 加载 mod asset bundle | 同上 |
| 34 | 加载 PawnBio（预购 pawn 与游戏内姓名列表） | `RimWorld.SolidBioDatabase.LoadAllBios()` |
| 35 | 注入 DefInjected 翻译（含 backstory），**并对「Def 不存在却有其翻译」报错** | `LanguageDatabase.activeLanguage.InjectIntoData_AfterImpliedDefs()` |
| 36 | **调用所有 `StaticConstructorOnStartup` 类的静态构造** | `Verse.StaticConstructorOnStartupUtility.CallAll()` |
| 37 | 烘焙静态图集（建筑损坏、压缩版覆盖贴图如木箱和背包） | `Verse.GlobalTextureAtlasManager.BakeStaticAtlases()` |
| 38 | 强制 GC + 清文件系统缓存 | `RimWorld.IO.AbstractFilesystem.ClearAllCache()` / `System.GC.Collect(...)` |

> 🔑 **第 36 步 `StaticConstructorOnStartup` 是 mod 做初始化最标准的位置**——
> 此时所有 Def 已解析、翻译已注入、内容已加载完毕。
> 需要更早介入才去用前序步骤，且**优先找游戏提供的正规扩展点而不是打补丁**。

---

## 2. Pawn 生成流程

### 2.1 入口与分流

```
Verse.PawnGenerator.GeneratePawn(PawnGenerationRequest)      ← 根入口，被几十个外部方法调用
  └─ GenerateOrRedressPawnInternal(request)                 ← 校验请求条件后
       ├─ RedressPawn(Pawn, request)                        ← 决定「改造」已有 pawn
       └─ GenerateNewPawnInternal(ref request)               ← 决定「全新生成」
```

`GenerateNewPawnInternal` 反复调 `TryGenerateNewPawnInternal` 并在失败时重试。
**重试有阈值，依次降级**：

| 累计失败次数 | 行为 |
|---|---|
| **70** | 记错误日志，**开始忽略 scenario 要求** |
| **100** | **开始忽略 validators** |
| **120** | 放弃，返回 null |

> 🔑 这三个数字对写兼容补丁很重要：某个 validator 太严会导致生成器
> 在 120 次后干脆返回 null，而**中间的错误日志可能掩盖了真正原因**。

### 2.2 全新生成的 34 个步骤

| # | 做什么 | 关键方法 |
|---|---|---|
| 1 | 创建 Pawn 对象 | `ThingMaker.MakeThing(ThingDef)` |
| 2 | 设置派系 | `Pawn.SetFactionDirect(Faction)` |
| 3 | 初始化组件与 tracker | `RimWorld.PawnComponentsUtility.CreateInitialComponents(Pawn)` |
| 4 | 校验 Anomaly 已启用、且请求允许 creepjoiner（若当前被视为 creepjoiner） | inline |
| 5 | 设置性别（request > PawnKindDef > 随机） | inline |
| 6 | 设置年龄 | `PawnGenerator.GenerateRandomAge` |
| 7 | 设置初始需求等级 | `pawn.needs.SetInitialLevels()` |
| 8 | 新生儿则调整饥饿与休息需求 | inline |
| 9 | **应用类人生物（humanlike）覆写** | 见下方 9a–9k |
| 10 | 非新生儿则生成随机 pawn 关系 | `GeneratePawnRelations` |
| 11 | 玩家所属动物则驯化并开启驯服训练 | inline |
| 12 | 生成合适的 `RoyalTitleDef`（**不限于 Royalty**，含许可、好感、灵能等级） | inline |
| 13 | Royalty：按 PawnKindDef 启用卧室与服装要求 | inline |
| 14 | Ideology：随机决定「加入殖民者」还是「加入奴隶」 | `pawn.guest.RandomizeJoinStatus()` |
| 15 | 初始化玩家控制的 pawn 的工作分配 | `pawn.workSettings.EnableAndInitialize()` |
| 16 | 为派系控制的动物/机械生成名字（机械体需 Biotech） | `pawn.GenerateNecessaryName()` |
| 17 | Ideology：设置 Ideo（依请求，仅非婴儿） | inline |
| 18 | Ideology：若 MindStateTracker 嗜人肉则设初始「上次吃人肉」tick（避免食人者开局就不开心） | `pawn.mindState.SetupLastHumanMeatTick()` |
| 19 | 若有 SurroundingsTracker，清状态 | `pawn.surroundings.Clear()` |
| 20 | 生成初始 Hediff | `GenerateInitialHediffs` |
| 21 | 请求要求生成尸体但 pawn 没死，就杀掉 | `pawn.Kill()` |
| 22 | **类人生物外观装备**：a 随机发型 b 随机胡须 c Ideology 随机纹身（婴儿或无 Ideology 时强制 NoTattoo） | `PawnStyleItemChooser.RandomHairFor / RandomBeardFor / RandomTattooFor` |
| 23 | 应用 PawnKindDef 里指定的所有 AbilityDef | inline |
| 24 | 通知所有 Scenario part「新生成了 pawn」——**scenario 强制的特质和 Hediff 在这里应用** | `Find.Scenario.Notify_NewPawnGenerating` |
| 25 | **校验，不通过就丢弃**（本次尝试立即返回 null） | 见 25a–25h |
| 26 | **生成装备**（新生儿、机械体、请求强制无装备时跳过） | `GenerateGearFor` |
| 27 | pawn 死亡则通知 PawnApparelTracker（**这就是衣服被玷污的触发点**） | `pawn.apparel.Notify_PawnKilled()` |
| 28 | 生成后置 validator（请求提供时） | inline |
| 29 | Anomaly：按请求或 PawnKindDef 应用 MutantDef | `MutantUtility.SetFreshPawnAsMutant` |
| 30 | 把 pawn 加入「正在生成」静态列表 | inline |
| 31 | 有派系则通知派系有新成员加入 | `pawn.Faction.Notify_PawnJoined(Pawn)` |
| 32 | 应用 PawnKindDef 的 `existingDamage`（Anomaly 用于手术疤痕等预存伤） | `ApplyMiscDamage` |
| 33 | 把 renderer 标脏，强制重建渲染树 | `pawn.Drawer?.renderer?.SetAllGraphicsDirty()` |
| 34 | 从「正在生成」列表移除——**在 `finally` 块里执行**，即使前面提前返回或抛错 | inline |

### 2.3 第 9 步细分：humanlike 覆写

| | 做什么 | 方法 |
|---|---|---|
| a | 请求未指定则随机派系 | inline |
| b | 应用请求里的肤色覆写 | inline |
| c | 选头部类型（随机，或被基因强制） | `pawn.story.TryGetRandomHeadFromSet` |
| d | Ideology：从请求取或随机取 `ColorDef` 作为最爱颜色 | inline |
| e | Biotech：按请求与派系定 xenotype。⚠️ **这里只设 label，基因要到 9j 才应用** | `GetXenotypeForGeneratedPawn` / `AdjustXenotypeForFactionlessPawn` |
| f | 挑选 solid 或随机 bio（姓名 + backstory） | `PawnBioAndNameGenerator.GiveAppropriateBioAndNameTo` |
| g | 请求指定则覆写名字（如儿童） | inline |
| h | 应用特质 | `GenerateTraits` |
| i | 应用体型 | `GenerateBodyType` |
| j | 生成基因 | `GenerateGenes` |
| k | 生成技能热情与等级 | `GenerateSkills` |

### 2.4 第 25 步细分：丢弃条件

任一条命中即丢弃本次结果：

- a. pawn 已死，但请求不允许尸体
- b. 请求不允许死亡或倒地，pawn 却倒地了
- c. 请求要求「具备暴力能力」，pawn 不具备
- d. PawnKind 要求特定技能能力，pawn 不具备
- e. PawnKind 要求最低技能等级，pawn 不满足
- f. PawnKind 要求最低合计技能等级，pawn 不满足
- g. 这是玩家开局 pawn、不满足 scenario 要求、且未达忽略阈值
- h. 请求提供了专用 pre-gear validator、pawn 未通过、且未达忽略阈值

### 2.5 第 26 步细分：装备生成

| | 做什么 | 方法 |
|---|---|---|
| a | 生成起始服装 | `PawnApparelGenerator.GenerateStartingApparelFor` |
| b | 生成背包物品 | `PawnInventoryGenerator.GenerateInventoryFor` |
| c | 生成手持武器（可被请求禁用） | `PawnWeaponGenerator.TryGenerateWeaponFor` |

### 2.6 实操提示

- **反射工厂方法**：想找「pawn 是怎么被造出来的」，静态搜索找不到——
  入口是 `PawnGenerator.GeneratePawn`，物件则是 `ThingMaker.MakeThing`。
- 想改服装生成，走 **apparel tag** 而不是 patch 生成流程（见
  [defs-and-patches-ref.md](defs-and-patches-ref.md) §4）。
- 第 33 步 `SetAllGraphicsDirty()`：在运行时改完外观后要手动触发，否则渲染树不重建。
