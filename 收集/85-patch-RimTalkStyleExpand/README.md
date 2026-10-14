# RimTalk StyleExpand Notes（第三方收藏）

## 来源

- **原作者**：NUOQI_P（GitHub: [NUOQIP](https://github.com/NUOQIP)）
- **GitHub**：https://github.com/NUOQIP/RimtalkStyleExpand
- **克隆 commit**：`e67021f`（2026-04-05，"chore: Bump version to v1.0.3 with bug fixes"）
- **Steam 创意工坊**：⚠️ 上游 `About/` 下**没有** `PublishedFileId.txt`，`About.xml` 里也没有自身工坊链接，
  故**无法确认工坊页面 ID**（需要时去仓库 Releases / 作者主页核对）。
- **许可**：⚠️ **上游没有任何 `LICENSE` 文件**，`README.md` 第 178–180 行只写了一句
  「基于 RimTalk 开发，遵循相同许可证」。而 RimTalk 本体（见 [80 号](../../80-standalone-RimTalk/README.md)）
  是 **CC BY-NC-SA 4.0**。也就是说：**本 mod 的许可是「转述继承」而非「自带授权文件」**，
  实际约束应按 CC BY-NC-SA 4.0（署名-非商用-相同方式共享）从严理解。本仓库仅作个人学习收藏。
- **状态**：纯学习收藏，未发布、未做运行验证。上游文档见
  [README_UPSTREAM.md](README_UPSTREAM.md)（中文）与 [README_EN.md](README_EN.md)（英文）。

## 一句话定位

RimTalk 的**文风扩展层**：用户往 `Styles/` 丢一个 `.txt`（小说片段），mod 就用 **embedding API 做语义切分
+ 向量检索**，按当前发言角色的上下文挑出最相关的文风片段，再注入 RimTalk 的 System Prompt，
让殖民者的对话「像那本书」。可选用 LLM 先把素材蒸馏成一份风格指南，效果比纯检索更好。

加载链：Core → Harmony → **RimTalk** → RimTalk StyleExpand（`modDependencies` 硬依赖
`brrainz.harmony` + `cj.rimtalk`）。

这是本仓库收的第 5 个 RimTalk 生态 mod（80 本体 / 81 ExpandMemory / 83 RPD 人格导演 / 84 事件感知 /
**85 文风扩展**）。生态拼图至此可归纳为**同一条 prompt 链上的五个插槽**：
80 提供管线 → 81 注入长期记忆 → 83 注入人格 → 84 注入事件 → **85 注入文风**，
建议按 80 → 81 → 83 → 84 → 85 顺序对照阅读。

## 值得学习的知识点

### Harmony 补丁（→ [harmony-patching.md](../../docs/knowledge/harmony-patching.md)）

- **编译期引用了 RimTalk.dll，补丁层却全程按 `object` + 字符串签名写**
  （[Source/Patches/RimTalkPatches.cs](Source/Patches/RimTalkPatches.cs)）：
  `[HarmonyPatch("RimTalk.Prompt.PromptManager", "BuildMessages")]` 用**类型名+方法名字符串**定位，
  补丁参数写成 `object __instance, object talkRequest, List<Pawn> pawns, string status`，
  静态构造里 `Type.GetType("RimTalk.Data.TalkRequest, RimTalk")` + 缓存 `PropertyInfo`。
  **好处：RimTalk 内部改名/重构时 mod 仍能加载，只是功能静默降级，而不是在 `PatchAll()` 抛异常炸掉整个 mod。**
  这与 84 号的「启动期反射 + 手工挂载」是同一思路的两种形态。
- **`IsRimTalkAvailable()` 作为总闸**：补丁体第一件事就是判 `_talkRequestType != null && _promptManagerType != null`，
  拿不到就 `return true` 放行原方法。**降级要降在补丁最外层，不要散落在每个功能里。**
- **双路径注入**：`RimTalkAPIIntegration.IsApiAvailable` 为真 → 走官方 API（注册 Scriban 变量 + PromptEntry）；
  为假 → Prefix 里直接给 `talkRequest.StylePrompt` 字段赋值，Postfix 再兜一层改写消息列表里的 System 内容。
  两条路径互斥（API 可用时 prefix 直接 `return true`）。**这是 RimTalk 生态 mod 的通用骨架，可直接抄。**
- **Postfix 反射改写泛型列表里的元组**：拿到 `__result` 后先判 `IsGenericType &&
  GetGenericTypeDefinition() == typeof(List<>)`，取元素类型，按 `Item1`（角色）/ `Item2`（内容）
  反射读 `ValueTuple`，找到 `roleValue == 0`（System）那条，用 `Activator.CreateInstance(tupleType, role, newContent)`
  重建后经索引器写回。**能改，但极脆**：RimTalk 换成 class 或字段改名就静默失效（外面包着 try/catch 只打 Warning）。
- **`catch { }` 空吞异常有 4 处**（`RimTalkPatches.cs` 读 context、`RimTalkAPIIntegration.cs` 生成确定性 ID 与
  解析 RimTalk 类型、`StyleVariableProvider.ExtractQueryFromContext`）。**反面教材**：跨 mod 反射探测失败时
  至少要留一句 `Log.Warning`，否则玩家和作者都无从判断「到底哪个环节没生效」。

### 反射式 API 集成（→ [harmony-patching.md](../../docs/knowledge/harmony-patching.md)）

[Source/API/RimTalkAPIIntegration.cs](Source/API/RimTalkAPIIntegration.cs) 是全仓最值得逐行读的
「对接第三方 mod 公共 API」样板：

- **装配体靠遍历 AppDomain 发现，不能直接引用类型**：`GetRimTalkAssembly()` 遍历
  `AppDomain.CurrentDomain.GetAssemblies()` 按 `GetName().Name == "RimTalk"` 找。
  原因在 csproj 里写死了：`<Reference Include="RimTalk"><Private>false</Private></Reference>`
  ——RimTalk.dll 不随本 mod 复制，运行时由 RimTalk 自己加载。**引用第三方 mod 的 DLL 时必须 `Private=false`，
  否则会在 `Assemblies/` 里多出一份副本，造成类型身份分裂（同一个类两份静态状态）。**
- **启动时机**：`[StaticConstructorOnStartup]` + 静态构造里 `LongEventHandler.ExecuteWhenFinished(Initialize)`，
  确保目标 mod 的静态构造已跑完再解析类型。（与 84 号同一手法。）
- **确定性 ID + upsert 注册（幂等注册的教科书写法）**：
  - `GetDeterministicId(modId, name)` 先尝试调对方的 `PromptEntry.GenerateDeterministicId` 静态方法；
    调不到才用本地规则 `mod_{modId}_{name}`（把 `.`/`-`/空格换成 `_`）兜底。
    **两边算出的 ID 必须一致**，否则第二次启动会生成重复条目。
  - `RegisterPromptEntry()` 先 `preset.GetEntry(entryId)` 查——**存在就只更新 `Content` 并 return**，
    不存在才 `Activator.CreateInstance` 新建。这样「改模板文本」与「首次创建」走同一条幂等路径。
  - 定位插入点用 `InsertPromptEntryAfterName(entry, "System Prompt")`，失败再退到 `AddPromptEntry`。
- **属性/字段双通道写入**：`SetProperty(obj, name, value)` 先试 `GetProperty(name).CanWrite`，
  失败再试 `GetField(name)`。**对方把字段改成属性（或反之）都不会炸**——写反射代码时的必备兜底。
- **复用对方的 API 配置而不落盘**：`GetRimTalkActiveConfig()` 反射读 `RimTalk.Settings.Get()` →
  `GetActiveConfig()` → 字段 `ApiKey`/`BaseUrl`/`Provider`/`SelectedModel`/`CustomModelName`。
  这正是 v1.0.1 版本记录里「修复配置文件共享违规问题」的正解：**LLM 配置只存一份，不复制到自己的存档里**。
  - ⚠️ 但 `Logger.Message($"... URL: {baseUrl}")` 会把 URL 打进日志。若用户把凭证写在 `BaseUrl` 里就会泄漏。
  - ⚠️ `GetDefaultUrl()` 的 `Google` 分支返回的 URL 里留着 `MODEL_PLACEHOLDER` 和 `API_KEY_PLACEHOLDER`
    字面量且**没有做替换**——当 `BaseUrl` 为空且 provider 是 Google 时，会拿这个占位符串真的发请求。

### 缓存与状态（→ [game-and-world-components.md](../../docs/knowledge/game-and-world-components.md)）

- **tick 缓存 60 拍 = 1 个游戏日**（[Source/API/StyleVariableProvider.cs](Source/API/StyleVariableProvider.cs)）：
  `CacheContext(pawn, context)` 由补丁 Prefix 调用，把当前发言者与上下文存进静态字段并记 `Find.TickManager.TicksGame`；
  `GetCachedContext()` 超 60 tick 返回 null。检索结果另有一层 `_cachedChunks` + `_cachedQuery` 比较，
  query 相同且未过期才复用。**「回调式变量提供者拿不到参数」时，用「补丁侧写入 + 按 tick 过期」传递上下文，
  是 RimTalk 这类模板变量框架的标准解法。**
- **缓存写在 mod 自己的目录里**（[Source/EmbeddingCache.cs](Source/EmbeddingCache.cs)）：
  `CacheDir = "Styles/.cache"`，路径 `Path.Combine(mod.Content.RootDir, CacheDir)`。
  - 好处：素材与缓存同目录，天然按 mod 实例隔离，多存档共用一份缓存（切分结果与存档无关）。
  - ⚠️ 风险：**工坊安装的 mod 目录通常是只读的**，`Directory.CreateDirectory` / `File.WriteAllText` 会失败；
    且源码随 Steam 更新被覆盖后缓存会丢。稳妥做法是写到 `Prefs` 或用户目录。
  - 缓存文件里存 `fileHash`，素材改动即失效——**缓存有效性用内容哈希判定，比用时间戳可靠**。
  - 写缓存是手写 `StringBuilder` 拼 JSON（只写 + 用 `ParseCache`/`ParseChunkCountOnly`/`ParseMetadataOnly`
    三个专用回读函数），而项目其实已经引了 Newtonsoft.Json。**同一份数据两套序列化路径，容易写出不一致。**
- **配置对象统一 `IExposable`**（[Source/StyleConfig.cs](Source/StyleConfig.cs)）：
  `StyleConfig` / `VectorApiConfig` / `RetrievalConfig` / `ChunkingConfig` / `LlmApiConfig` / `DebugConfig`
  全部 `Scribe_Values.Look(ref 字段, "key", 默认值)`。这是 ModSettings 的标准姿势（对比 84 号：
  纯缓存组件用空 `ExposeData()`，真存档组件才用 `LookMode.Deep`）。

### 文件监听与编码（→ [performance-and-gui.md](../../docs/knowledge/performance-and-gui.md)）

- **`FileSystemWatcher` + 1 秒防抖**（[Source/StyleWatcher.cs](Source/StyleWatcher.cs)）：
  监听 `Styles/*.txt`，订阅 `Created`/`Changed`/`Deleted`/`Renamed` 四个事件，
  `_lastEvent` + `_debounceTime = TimeSpan.FromSeconds(1)` 合并连续事件，
  `Stop()` 时逐个 `-=` 再 `Dispose()`。**编辑器保存一次会连发多个事件，防抖不是可选项。**
- **用户素材必须做编码嗅探**（[Source/FileEncodingHelper.cs](Source/FileEncodingHelper.cs)）：
  `DetectEncoding` 依次判 UTF-8 BOM / UTF-16LE BOM / UTF-16BE BOM，无 BOM 则读前 4096 字节跑 `IsUtf8` 校验，
  都不是才回退。**用户从网上随手下载的 txt，GBK/UTF-8/UTF-16 混杂是常态**，
  直接 `File.ReadAllText` 会让中文变乱码——v1.0.2 的「修复中文编码问题」就是这个文件。
  （本仓库读中文 `.md` 也踩过同类坑：用 read 类工具而非 shell 输出。）

### UI（→ [ui-and-windows.md](../../docs/knowledge/ui-and-windows.md)）

- `Source/UI/SettingsWindow.cs` **53 KB / 全仓最大 UI 文件之一**（84 号的 `EventFilterUI.cs` 是 1099 行，
  本文件规模相当），全部走 `Widgets.BeginScrollView` + 单页纵向滚动，没有分栏 Tab 抽象。
  **反面教材**：文件规模与 84 号那条「大 UI 文件要拆」的结论一致。
- `Dialog_StylePreview.cs`（18 KB）是切分结果/检索片段的预览弹窗。
- `VariableHelper.GetBuiltinVariables()` 反射读 RimTalk 的 `VariableDefinitions.GetScribanVariables()`，
  读不到就 `GetFallbackVariables()` 返回**硬编码的回退变量表**。
  ⚠️ 硬编码清单会随 RimTalk 升级悄悄过期——UI 上少列出几个变量，没有任何报错提示。

### 工程与版本（→ [cross-version-structure.md](../../docs/knowledge/cross-version-structure.md)）

- **输出路径模板化**：`Source/RimTalk-StyleExpand.csproj` 是 SDK-style，
  `<GameVersion Condition=" '$(GameVersion)' == '' ">1.5</GameVersion>`，
  `<OutputPath>..\$(GameVersion)\Assemblies</OutputPath>`，
  构建脚本 `Source/build.bat` 连续跑 `dotnet build -c Release /p:GameVersion=1.5` 与 `=1.6`。
  **一份 csproj 出两个版本目录，这是本仓库目前最干净的多版本做法**（对比 84 号硬编码 `..\..\1.6\Assemblies\`）。
- ⚠️ **但引用路径没跟着 `GameVersion` 走**：
  `<Reference Include="RimTalk"><HintPath>..\..\RimTalk-main\1.5\Assemblies\RimTalk.dll</HintPath></Reference>`
  写死 `1.5`。`/p:GameVersion=1.6` 构建时**编译的仍是 1.5 的 RimTalk.dll**。
  同理 `Scriban` 指向 `..\..\RimTalk-main\Libs\Scriban.dll`（`Private=true`，会被复制到输出）。
- 依赖走 NuGet 而非本地 dll 引用：`Lib.Harmony.Ref 2.*`、`Krafs.Rimworld.Ref $(GameVersion).*-*`、
  `Newtonsoft.Json 13.0.3`。**`Krafs.Rimworld.Ref` 的版本号带 `$(GameVersion)` 是跨版本构建的关键技巧**。
- **`Newtonsoft.Json.dll` 被直接放进 `1.5/Assemblies/` 与 `1.6/Assemblies/`**（各 712 KB，共 1.4 MB）。
  这是 `PackageReference` 未标 `PrivateAssets` 时 SDK 默认复制的产物，但**把公共库 DLL 塞进 mod 的
  `Assemblies/` 是版本冲突高发点**：一旦玩家同时装了另一个携带不同版本 Newtonsoft.Json 的 mod，
  加载顺序会决定胜者，症状是难以定位的 `MissingMethodException`。**正解是让 RimWorld 的
  `UnityEngine`/公共库走单一来源，第三方库要么不打包要么统一版本。**

### 反面教材清单（本 mod 踩过或留有的坑）

| 类别 | 具体 | 出处 |
|---|---|---|
| 依赖声明冗余 | `<loadAfter>` 列的 `brrainz.harmony` / `cj.rimtalk` 与 `<modDependencies>` **完全重复** | `About/About.xml` |
| 版本声明滞后 | `About.xml` 写 `modVersion 1.0.2`，而 commit 信息与 README 版本历史都已是 **v1.0.3** | `About/About.xml` |
| 许可链断裂 | 无 `LICENSE` 文件，仅 README 一句「遵循 RimTalk 相同许可证」 | 仓库根 |
| 引用版本错配 | `GameVersion=1.6` 时仍引用 `1.5` 的 RimTalk.dll | csproj |
| 公共库打包 | `Newtonsoft.Json.dll` 进 `Assemblies/`，两个版本目录各一份 | `1.5/`、`1.6/` |
| 写 mod 目录 | 缓存写 `mod.Content.RootDir/Styles/.cache/`，工坊安装通常只读 | `EmbeddingCache.cs` |
| 默认值用自身 | `Scribe_Values.Look(ref StylePromptTemplate, "stylePromptTemplate", StylePromptTemplate)`——用字段当前值当默认值，老存档不会跟随模板升级 | `StyleConfig.cs` |
| 字面量重复 | `RetrievalConfig.FullPromptTemplate` 同一段模板在字段初始化与 `Look` 的默认值参数里**各写了一遍** | `StyleConfig.cs` |
| 内容写进代码 | `GetDefaultPrompt("古风仙侠")` 把 48 行专属风格指南硬编码在 C# 里，其他文风一律返回空串 | `StyleConfig.cs` |
| 占位符未替换 | `GetDefaultUrl()` 的 Google 分支留着 `MODEL_PLACEHOLDER`/`API_KEY_PLACEHOLDER` 字面量 | `RimTalkAPIIntegration.cs` |
| 日志泄凭证 | `Logger.Message(... URL: {baseUrl})` | `RimTalkAPIIntegration.cs` |
| 静默吞异常 | 4 处 `catch { }` | 见上 |
| 死代码风险 | `VariableHelper.GetFallbackVariables()` 硬编码 RimTalk 变量表，升级后静默过期 | `VariableHelper.cs` |

## 收录时排除的上游文件

| 上游路径 | 大小 | 排除原因 |
|---|---|---|
| `Styles/.cache/古风仙侠.json` | 1.3 MB | 切分/embedding 的**派生产物**（`EmbeddingCache` 运行时重新生成），与源码无关且体积占比过半 |
| `Styles/5w字测试.txt` | 136 KB | **压测文件**——实测其内容是 `古风仙侠.txt`（775 行）**逐字重复 4 遍**拼成（`IndexOf` 命中 4 次），纯冗余 |

> ⚠️ 另外提示：`Styles/古风仙侠.txt` 是作者随 mod 附带的**示例文风素材**（一部仙侠小说片段，
> 人物「玄真子/云逸」，内容以「断剑残阳」开场）。其**著作权归属本仓库无法确认**，
> 保留仅为展示 mod 的输入格式与默认 prompt 生成效果，**不代表可自由再分发**。

## 目录结构

```
85-patch-RimTalkStyleExpand/
├── About/
│   ├── About.xml            # packageId: RimTalk.StyleExpand，作者 NUOQI_P，声明 1.5 + 1.6
│   └── Preview.png          # 工坊预览图
├── 1.5/Assemblies/          # RimTalk-StyleExpand.dll + Newtonsoft.Json.dll
├── 1.6/Assemblies/          # 同上（两份 DLL 内容一致：上游用同一构建出双版本）
├── Languages/
│   ├── ChineseSimplified/Keyed/StyleExpand.xml   # ~19 KB keyed
│   └── English/Keyed/StyleExpand.xml             # ~19 KB keyed
├── Styles/                  # 用户素材目录（.gitignore 故意不忽略 .cache，见「反面教材」）
│   ├── README.md            # 上游自带的目录说明
│   └── 古风仙侠.txt          # 示例素材（著作权存疑，见上）
├── Source/                  # 16 个 .cs + csproj + build.bat
│   ├── API/
│   │   ├── RimTalkAPIIntegration.cs    # 反射式 API 集成 + 确定性 ID + upsert（17 KB）
│   │   └── StyleVariableProvider.cs    # tick 缓存的模板变量提供者
│   ├── Patches/
│   │   └── RimTalkPatches.cs           # 字符串定位的 Harmony 补丁（5.6 KB）
│   ├── UI/
│   │   ├── SettingsWindow.cs           # 53 KB 主设置界面
│   │   └── Dialog_StylePreview.cs       # 切分/检索结果预览
│   ├── SemanticChunker.cs              # 基于 embedding 相似度的语义切分（25 KB）
│   ├── StyleRetriever.cs               # 向量检索（36.5 KB）
│   ├── VectorClient.cs                 # Embedding API 客户端（21.7 KB）
│   ├── LLMClient.cs                    # LLM API 客户端（13.3 KB）
│   ├── EmbeddingCache.cs               # 素材哈希 + 切分缓存（20 KB）
│   ├── StyleWatcher.cs                 # FileSystemWatcher + 防抖
│   ├── FileEncodingHelper.cs           # BOM/UTF-8 嗅探
│   ├── StyleConfig.cs                  # 6 个 IExposable 配置对象 + 硬编码风格指南
│   ├── StyleExpandSettings.cs
│   ├── StyleExpandMod.cs               # Mod 入口，Harmony id: RimTalk.StyleExpand
│   ├── PromptBuilder.cs
│   ├── VariableHelper.cs
│   ├── Logger.cs
│   └── build.bat                       # 连续构建 1.5 与 1.6
├── README.md               # 本文件（收藏版学习笔记）
├── README_UPSTREAM.md      # 上游中文说明
├── README_EN.md            # 上游英文说明
└── .gitignore
```

## 构建与部署

**前置条件**

1. RimWorld 1.5 或 1.6（本机当前**未安装**，故本目录沿用上游预编译 DLL，未验证能否复现构建）
2. RimTalk DLL —— csproj 的 `HintPath` 期望仓库同级存在 `RimTalk-main/1.5/Assemblies/RimTalk.dll`
   与 `RimTalk-main/Libs/Scriban.dll`（即 [RimTalk 源码](https://github.com/CET-Chengdu/RimTalk) 克隆后按此布局摆放）
3. NuGet 可恢复 `Lib.Harmony.Ref 2.*` / `Krafs.Rimworld.Ref` / `Newtonsoft.Json 13.0.3`

**构建**

```bash
cd Source
dotnet build -c Release /p:GameVersion=1.5   # 产物 → ../1.5/Assemblies/
dotnet build -c Release /p:GameVersion=1.6   # 产物 → ../1.6/Assemblies/
# 或直接跑 build.bat（额外把 1.5 的 Newtonsoft.Json.dll 复制到 1.6）
```

⚠️ 重新构建后应从 `Assemblies/` 删除 `Newtonsoft.Json.dll` 再进游戏，避免与其他 mod 的同名库冲突（见反面教材）。

**部署 / 加载顺序**

`Harmony` → `RimTalk` → `RimTalk StyleExpand`。玩家还需自备一个 Embedding API（推荐本地 Ollama）
与 `Styles/*.txt` 素材，否则设置页能开但检索恒为空。

## 相关文件

- 上游源码：[Source/](Source/)（16 个 `.cs`，约 227 KB）
- 上游说明：[README_UPSTREAM.md](README_UPSTREAM.md) / [README_EN.md](README_EN.md)
- 依赖 mod：[80-standalone-RimTalk](../../80-standalone-RimTalk/README.md)（本体）、
  [81-patch-RimTalkExpandMemory](../../81-patch-RimTalkExpandMemory/README.md)（记忆注入，prompt 链相邻插槽）
- 同生态：[83-patch-RPDPersonaDirector](../../83-patch-RPDPersonaDirector/README.md)、
  [84-patch-RimTalkEventPlus](../../84-patch-RimTalkEventPlus/README.md)、
  [86-patch-RimTalkDynamicColors](../../86-patch-RimTalkDynamicColors/README.md)
- 主题文档：[harmony-patching.md](../../docs/knowledge/harmony-patching.md)、
  [cross-version-structure.md](../../docs/knowledge/cross-version-structure.md)、
  [publishing-and-release.md](../../docs/knowledge/publishing-and-release.md)