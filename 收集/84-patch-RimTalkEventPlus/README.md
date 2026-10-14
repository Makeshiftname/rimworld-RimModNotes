# RimTalk Event+ Notes（第三方收藏）

## 来源

- **原作者**：SaltGin（GitHub: [SaltGin](https://github.com/SaltGin)）
- **GitHub**：https://github.com/SaltGin/RimTalkEventPlus
- **克隆 commit**：`da884c`（2026-10-05，"Improve event accuracy, cache efficiency, and filter UI"）
- **Steam 创意工坊**：<https://steamcommunity.com/sharedfiles/filedetails/?id=3612632140>
- **许可**：**CC BY-NC-SA 4.0**（见 [LICENSE](LICENSE)）。⚠️ 含 **NonCommercial（非商用）** 与
  **ShareAlike（相同方式共享）** 两项限制——**可用于个人学习，转载/再分发必须署名且保持同协议**，
  且不得用于商业用途。本仓库仅作个人学习收藏。
- **状态**：纯学习收藏，未发布、未做运行验证。上游 README 见 [README_UPSTREAM.md](README_UPSTREAM.md)（内容极简，仅指向工坊页 + 许可声明）

## 一句话定位

RimTalk 的**事件感知扩展层**：把殖民地「此刻正在发生什么」——进行中的任务、地图条件、活跃威胁（袭击/偷窃者/
集群 mech/通用 Lord）、站点部件——实时收集成结构化 prompt 块注入对话，并附带一整套**事件过滤设置 UI**
（按类型过滤 / 按实例过滤 / 上下文相关过滤）。

加载链：Core → Harmony → **RimTalk** → RimTalk Event+（`modDependencies` 硬依赖 `brrainz.harmony` + `cj.rimtalk`）。

这是本仓库收的第 4 个 RimTalk 生态 mod（80 本体 / 81 ExpandMemory / 83 RPD 人格导演 / **84 事件感知**），
与 83 号改同一条 prompt 链路：**83 往对话里加"人"，84 往对话里加"事"**，建议对照阅读。

## 值得学习的知识点

### Harmony 补丁（→ `harmony-patching.md`）

- **不用 `[HarmonyPatch]` 特性、改手工挂载**（`Source/RimTalkEventMemory/PromptService_OngoingEventsPatch.cs`）：
  代码注释点明了原因——RimTalk 的 `RimTalk.Data.Constant` 有早期静态初始化问题，
  所以推迟到 `[StaticConstructorOnStartup]` 里用 `AccessTools.TypeByName` + `AccessTools.Method(...)`
  手工解析目标，再 `harmony.Patch(method, postfix: ...)`。
  **patch 第三方 mod 时，"特性声明"不一定安全，"启动期反射 + 手工挂载"才是可控姿势。**
- **签名必须逐字对齐**：注释写明 `void DecoratePrompt(TalkRequest, List<Pawn>, string)`，
  并用 `AccessTools.Method(type, "DecoratePrompt", new[]{ talkRequestType, typeof(List<Pawn>), typeof(string) })`
  按**完整参数类型数组**定位——同名重载存在的场景下避免打错。
- **运行期能力探测 + 双路径降级**：先找 `TalkRequest.Context` 属性（新版 RimTalk 注入系统指令），
  找不到就退到 `talkRequest.Prompt`（旧版注入用户消息）。`ResolveContextProperty()` 带
  `_contextPropertyResolved` 一次性标志，失败只 `Log.Warning` 不抛。**跨版本兼容的教科书写法。**
- **`TargetMethods()` 动态发现补丁目标**（`Quest_CacheInvalidation_Patches.cs`）：
  `QuestPart.ReplacePawnReferences` 是虚方法且大量子类**不调 base**，所以遍历 `AccessTools.AllTypes()`
  找出所有 `QuestPart` 子类的实现，逐个 yield——**给一整族覆写方法打补丁的正规解**。
- **缓存失效补丁的完整组合**（同上）：
  - `Quest.Notify_SignalReceived` → Prefix 用 `signal.global || signal.tag.StartsWith($"Quest{id}.")`
    复刻原版分发规则，命中才在 Postfix 失效缓存（**不是无脑全失效**）
  - `Quest.AddPart` / `RemovePart` → `__state` 记 `PartsListForReading.Count`，Postfix 比数量变化；
    且额外判 `QuestState.Ongoing` 跳过建造期无效调用
  - `Quest.End` → 独立小补丁 `Quest_End_ClearCache_Patch.cs`，防已完成任务残留进 per-game 缓存
  三者合起来是**「主动作补丁 + 一组失效补丁 + 状态判定」的标准缓存一致性模板**，
  比 83 号的单点 `__state` diff 更完整。
- **`__state` 承载自定义上下文对象**（`ThreatCapture.cs`）：
  `IncidentWorker.TryExecute` 用 `out ThreatCaptureContext __state` 在 Prefix 建作用域、Postfix 按
  `__result` 决定是否提交记录——**把"这次调用期间的临时状态"用 `__state` 传出，不用静态字段**。
- **`LongEventHandler` 延后重活**（`Map_FinalizeInit_OngoingEventsDump_Patch.cs`）：
  `Map.FinalizeInit` 的 Postfix 里**不直接做反射预热**，而是
  `LongEventHandler.ExecuteWhenFinished(...)` 延到长事件结束，且回调里重新校验
  `Current.Game != null && Find.Maps.Contains(__instance)`——**地图可能在回调执行前被移除**。
  这是 1.6 长事件机制的标准用法。

### UI 与窗口（→ `ui-and-windows.md`）

- **`Mod` 类接管设置界面**（`RimTalkEventPlus.cs`）：
  `SettingsCategory()` 返回 `Translate()` 后的分类名，`DoSettingsWindowContents` 转发给
  `EventFilterUI.DoFilteringUI(inRect, settings)`。**这是 mod 自带设置页的最小完整骨架。**
- **过滤能力按类别声明，而不是一刀切**（`EventFilterSettings.cs`）：
  - `SupportsTypeFiltering(category)` 只对 **Quest / MapCondition / SitePart** 返回 true
    ——**Threat 类别刻意不支持类型过滤**，因为威胁是运行时生成的 `Lord` 实例、没有对应 Def，
    只能按实例走。对应字段是三个 `HashSet<string>`：
    `disabledQuestDefNames` / `disabledMapConditionDefNames` / `disabledSitePartDefNames`
  - `SupportsInstanceFiltering(category)` **只有 Quest 为 true**，字段是
    `Dictionary<string, DisabledInstanceSet>`（key = `colonyId`），
    解决"同一个任务 def，A 殖民地屏蔽 B 殖民地不屏蔽"
  - `disabledEventDefNames` 是**遗留字段**，代码注释写明"read once and migrated"，
    启动时由 `TryMigrateLegacyTypeFilters()` 搬进新规则集
  **UI 必须先问能力再画控件，否则会画出永远不生效的开关。**
- **遗留字段与新字段并存**：`ClearTypeFilters()` 只清三个新 `HashSet`，**不碰 `disabledEventDefNames`**——
  迁移一旦失败，旧数据就会静默残留。
- **大文件拆分思路**：`EventFilterUI.cs` **1099 行 / 53.7 KB**，是本仓库目前最大的单个 UI 类，
  内部用 `Render*Section` + `Draw*` 切成优化/高级模式/类别过滤/类型过滤/实例过滤等小节，
  另有 `TwoColumnLayout` 私有 `struct` 承载分栏布局。**该拆但没拆，是现成的反面教材。**
- **设置项生效值与界面值分离**：`showQuests` 是用户勾选值，`ShowQuestsEffective =>
  showQuests && !IsEnhancedPromptLockActive` 是**叠加第三方 mod 冲突锁后的实际生效值**——
  UI 读 Effective、业务代码读原始值并各自判断，边界清楚。

### 存档与设置持久化（→ `game-and-world-components.md`）

- **两个 `GameComponent` 承担不同生命周期**：
  - `QuestCacheComponent`——**纯运行时缓存**，`ExposeData()` 是空的
    （配套还有个空的 `QuestAffectsMapCacheComponent : GameComponent`），
    说明缓存**刻意不跨存档**，读档后自然重建
  - `ThreatTrackerComponent`——**真存档数据**，三个 `List<T>` 用
    `Scribe_Collections.Look(..., LookMode.Deep)` 存 `raidThreats` / `sightstealerThreats` /
    `genericLordThreats`
  **同一 mod 里"要不要 ExposeData"是按数据性质决定的，不是按类决定——很值得记住。**
- **`ModSettings.ExposeData()` 逐字段 Scribe**（`EventFilterSettings.cs`）：
  十余个 `Scribe_Values.Look` / `Scribe_Collections.Look`，配合 `filterSchemaVersion` 版本号。
- **一次性迁移 + 幂等标志**（`BlacklistMigrationHelper.cs` + `BlacklistMigrationStartup.cs`）：
  `[StaticConstructorOnStartup]` 里（此时 `DefDatabase` 已就绪）跑三步迁移——
  `TryMigrateBlacklist` / `TryMigrateLegacyTypeFilters` / `PruneUnsupportedTypeRules`——
  任一改动过就 `WriteSettings()`。每步都以 `questBlacklistMigrated` 之类布尔位做**只跑一次**保证；
  连 `catch` 分支都会把标志置 `true`，避免存档损坏时每次启动重复炸日志。
  迁移时用 `GetNamedSilentFail` 校验 def 是否存在，**当前整合包里不存在的 def 会被跳过而不是报错**——
  这是 mods 之间共享 Def 数据的标准姿势。

### 跨 mod 依赖与冲突检测（→ `publishing-and-release.md`）

- **主动探测另一个 mod 的开关**（`EnhancedPromptDetector.cs`）：
  `ModLister.GetActiveModWithIdentifier("ruaji.rimtalkpromptenhance")` 判断
  RimTalk Enhanced Prompt 是否在，然后反射读它的 `EnableAutoEventCapture`。
  **先存 `FieldInfo`/`PropertyInfo` 元数据，属性优先于字段**，每次读值再 `Invoke` 拿最新设置——
  **反射结果缓存的是"句柄"而不是"值"**，否则改了设置不生效。
- **互斥锁定而非硬冲突**：检测到对方开了自动事件采集，就把本 mod 的任务/条件/威胁三个分类
  锁死（`IsEnhancedPromptLockActive`），并在 UI 顶部画横幅 `DrawEnhancedPromptBanner`。
  **两个 mod 抢同一条 prompt 链路时的优雅降级范式。**

### 性能（→ `performance-and-gui.md`）

- **字段/属性反射句柄全缓存**（`QuestCacheComponent.cs`）：
  `Dictionary<(Type,string), FieldInfo>` / `Dictionary<(Type,string), PropertyInfo>`，
  避免每帧反射；`QuestLinkUtil.GetQuestKeyPawns` 也走同一套缓存。
- **反射型缓存包 try/catch**：`PrewarmActiveQuestsForMap` 注释明确写
  "Prewarm is optional; never let it interfere with gameplay"——单个任务预热失败只 `Log.Warning`。
- **prompt 体积硬预算**（`OngoingEventsFormatter.cs`）：
  `maxChars`（1200 / 1500 / 2000 / 800 按场景不同）+ 单条 body 超 600 字截断 + 正文换行缩进对齐。
  **给 LLM 的文本必须显式限长**，否则 token 成本失控——这条对做 AI mod 普遍适用。
- **上下文相关性剪枝**（`ContextPawnMatcher.cs`）：
  可选开启，只保留与对话双方（发起者/听者）及其 20 格内动物相关的任务事件。

### 序列化格式（与 83 号对照）

- 输出块格式：`[Ongoing events]` → `1) 标题` + 缩进正文 → `[Event list end]`，
  正文过 `StripTags()` 清掉富文本标记。**和 83 号 RPD 的请求格式契约是同一条链路的两个写入方**，
  两者叠加时的分隔与预算分配值得单独研究。

## 坑与反面教材

1. **三处命名不一致**：`packageId` 是 `saltgin.rimtalkeventmemory`（**小写 + 旧名**），
   mod 显示名是 "RimTalk Event+"，源码目录是 `Source/RimTalkEventMemory/`，
   而程序集名 / `RootNamespace` 是 `RimTalkEventPlus`。
   Harmony id 也沿用旧名 `"saltgin.rimtalkeventmemory"`。**典型的改名残留**——
   读代码时按目录名找命名空间会扑空。
2. **`supportedVersions` 声明 1.5 + 1.6，但 csproj 只输出到 `1.6/Assemblies\`**：
   `OutputPath` 硬编码单版本，没有任何 1.5 分支。1.5 支持很可能是历史遗留，实际只有 1.6 产物。
3. **`Source/RimTalkEventMemory/REFRENCE ONLY-RimTalkEvent_QuestBlacklistDefs.xml` 是死文件**：
   文件名带空格、"REFRENCE" 还是拼错的，且**没有被 `.csproj` 的任何 `Compile`/`None` 项收录**——
   编译时根本不参与，纯开发者参考件。
4. **`QuestBlacklist.cs` 已标 `[System.Obsolete]` 但仍保留在项目里**，
   与新 `EventFilterSettings` 双轨并存。迁移期这样做合理，但长期是负担——
   印证第 3 点：`RimTalkQuestBlacklistDef` 这套 Def 现在只服务于迁移入口。
5. **`About.xml` 的 `<loadAfter>cj.rimtalk</loadAfter>` 与 `<modDependencies>` 里的
   `cj.rimtalk` 重复**。写法本身没错（packageId 大小写和依赖项一致），
   但依赖已由 `modDependencies` 表达，`loadAfter` 是冗余的。
   **对照 83 号 RPD：它在 `loadAfter` 里写成 `cj.RimTalk`（大写 R/T），实际失效——同一个坑它踩了，这里没踩。**
6. **csproj 用 7 层 `..\` 相对路径引用 Steam 目录**：
   `<HintPath>..\..\..\..\..\..\..\Program Files (x86)\Steam\...\Assembly-CSharp.dll</HintPath>`。
   作者显然是把工程塞进 `Steam\steamapps\workshop\content\294100\<id>\Source\` 里编译的。
   脱离那个目录结构就编不过——**上游没提供可移植的构建方式**。

## 目录结构

```
84-patch-RimTalkEventPlus/     # 上游为 flat 布局，无版本目录
├── About/                      # About.xml / ModIcon.png / preview.png / PublishedFileId.txt
├── Languages/                  # 24 种语言，统一放在 <Lang>/Keyed/RimTalkEventPlus_Settings.xml
├── Source/RimTalkEventMemory/  # ⚠️ 目录名是旧名，命名空间却是 RimTalkEventPlus
│   ├── ThreatHandlers/         # 4 种威胁处理器
│   ├── Properties/             # AssemblyInfo.cs（版本 1.0.0.0，未随发布递增）
│   ├── RimTalkEventPlus.csproj # 非 SDK-style，ToolsVersion 15.0，net48，LangVersion 默认
│   └── … 30 个 .cs
├── LICENSE                     # CC BY-NC-SA 4.0
├── README_UPSTREAM.md          # 上游 README
└── README.md                   # 本文件
```

## 构建与部署

⚠️ **本仓库不含编译产物 DLL**。原因有二：

1. **上游仓库本来就没有提交 DLL**——`.gitignore` 里明确写了 `*.dll`，只发源码；
2. 本机**没有 RimWorld 安装**，无法补编。

若要自行构建，需要三个前置条件（缺一不可）：

- **RimWorld 安装**（`Assembly-CSharp.dll` + `UnityEngine.CoreModule/IMGUIModule/TextRenderingModule`）
- **RimTalk 的 DLL**：工坊 id `3551203752`，csproj 硬编码指向
  `workshop\content\294100\3551203752\1.6\Assemblies\RimTalk.dll`
- **Lib.Harmony 2.4.2**（`packages.config` 声明，net48）

然后：

```bash
cd Source/RimTalkEventMemory
nuget restore      # 或 msbuild /t:Restore，拉取 Lib.Harmony 2.4.2
msbuild RimTalkEventPlus.csproj /p:Configuration=Release
```

`OutputPath` 是 `..\..\1.6\Assemblies\`（相对 `Source/RimTalkEventMemory/`），
产物应放到 **`1.6/Assemblies/RimTalkEventPlus.dll`**。
若日后补入 DLL，位置就是这里，**不要放 `1.5/`**（见上文坑 2）。

## 相关文件

| 想看什么 | 去哪 |
|---|---|
| 上游说明 | [README_UPSTREAM.md](README_UPSTREAM.md) |
| mod 元数据 / 依赖 | [About/About.xml](About/About.xml) |
| 主类 + 设置入口 | `Source/RimTalkEventMemory/RimTalkEventPlus.cs` |
| prompt 注入 + 手工挂载 | `Source/RimTalkEventMemory/PromptService_OngoingEventsPatch.cs` |
| RimTalk 高级模式 API 注册 | `Source/RimTalkEventMemory/RimtalkAPIIntegration.cs` |
| 缓存失效补丁组 | `Source/RimTalkEventMemory/Quest_CacheInvalidation_Patches.cs` |
| 威胁采集生命周期 | `Source/RimTalkEventMemory/ThreatCapture.cs` |
| 设置持久化 + 过滤模型 | `Source/RimTalkEventMemory/EventFilterSettings.cs` |
| 设置 UI（最大文件） | `Source/RimTalkEventMemory/EventFilterUI.cs` |
| 存档迁移 | `Source/RimTalkEventMemory/BlacklistMigrationHelper.cs` |
| 跨 mod 冲突检测 | `Source/RimTalkEventMemory/EnhancedPromptDetector.cs` |

## 相关 mod（本仓库内）

- [80-standalone-RimTalk](../80-standalone-RimTalk/README.md) — 本体
- [81-patch-RimTalkExpandMemory](../81-patch-RimTalkExpandMemory/README.md) — 记忆扩展
- [83-patch-RPDPersonaDirector](../83-patch-RPDPersonaDirector/README.md) — 人格导演（同改 prompt 链路）