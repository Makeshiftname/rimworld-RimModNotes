# RPD — RimTalk: Persona Director Notes（第三方收藏）

## 来源

- **原作者**：Rabirinth（GitHub: [Rabirinth-hub](https://github.com/Rabirinth-hub)）
- **GitHub**：https://github.com/Rabirinth-hub/RPD
- **克隆 commit**：`1fd225b`（2026-10-01，"Fix persona editor targeting and request formatting"）
- **版本**：packageId `RP.RimTalk.PersonaDirector`，仅 RimWorld **1.6**
- **许可**：⚠️ **上游仓库无 LICENSE 文件**（GitHub API `license: null`）。默认保留全部版权，**未做再分发授权推定**
- **状态**：纯学习收藏，未发布、未做运行验证；上游完整文档见 [README_UPSTREAM.md](README_UPSTREAM.md)（中英双语 + 附录 A–H 请求格式契约）

## 一句话定位

RimTalk 的**人格导演插件**：接管 RimTalk 的人格生成入口，把 Pawn 的深层资料（基因/背景/关系/技能/健康/装备/背包）组装成结构化 prompt 交给 AI 生成 Persona，并叠加**预设库+规则分配、导演台批量生成、人格演变（可回滚）、自动生成/自动更新、Scriban 变量注册**五层能力。

加载链：Core → Harmony → **RimTalk** → RPD（硬依赖 `cj.rimtalk`）。

## 值得学习的知识点

### Harmony 补丁（→ `harmony-patching.md`）

- **反射定位 + 版本兼容降级**（`Source/Patch_PersonaEditorWindow.cs`）：
  `AccessTools.Property/Field/Method` 拿私有成员；RimTalk 1.3 把窗口改成多 tab，代码用
  `PersonalityTabMethod ?? AccessTools.Method(..., "DoWindowContents")` 做**目标方法回退**，
  并用 `HasTabs` 切换两套布局坐标——**patch 第三方 UI 时目标方法可能消失/改名的标准应对**。
- **`[HarmonyPrepare]` 兜底 null 目标**（`Source/DirectorVariableProvider.cs`）：
  `TargetMethod()` 里 `AccessTools.TypeByName("RimTalk.Prompt.MustacheParser")` 为 null 时，
  用 `[HarmonyPrepare] => TargetMethod() != null` 让该补丁**静默跳过而不拖垮整个 PatchAll**。
- **Prefix/Postfix 跨时点传递状态用 `__state`**（`Source/Patch_AutoEvolveTriggers.cs`）：
  `CaptureOldRole(..., out DirectorPawnRoleState __state)` 在 Prefix 抓旧身份，
  Postfix 再抓新身份做 diff——**Harmony 官方推荐的"修改前后对比"范式**，比在静态字典里存快照干净。
- **增量 diff 技巧**：直系亲属死亡补丁在 Prefix 缓存 `oldLoversAndFiances?.Count`，
  Postfix 只遍历 `[旧Count, 新Count)` 区间发事件；拒绝求婚的分手则 Prefix 记 `Lover/Fiance` 是否存在，
  Postfix 复查是否已解除——**用「集合长度/关系存在性」把「事件发生」判定压到最小**。
- **`[HarmonyPriority(Priority.Last)]`**：身份变化类补丁统一放最后，尽量读其他 mod 已改完的状态。
- **`[HarmonyPatchCategory]` + 运行时开关整组摘挂**（`Source/DirectorPatchRegistry.cs`）：
  实验性补丁打上分类标签，用 `harmony.PatchCategory/UnpatchAll(id)` **运行时整体启用/停用**，
  配合 `lock` + 幂等标志避免重复打补丁。用户关掉实验功能时，连补丁都不存在。

### UI 与窗口（→ `ui-and-windows.md`）

- **IMGUI 坐标要预计算**：补丁里所有按钮 `Rect` 先算完再画（`noteRect/evolveRect/timeRect` 按 `xMax + spacing` 递推）。
- **异步结果回填范式**：Evolve 按钮把 AI 请求丢 `Task.Run`，`Postfix` 每帧检查
  `task.IsCompleted` 再取结果写回编辑框——**不在主线程 `await`**，用
  `evolvingWindow`/`evolvingPawn` 做 `ReferenceEquals` 校验防止串档。
- **主线程/后台线程边界**：`PrepareEvolveRequest` 必须在 `Task.Run` **之外**（要访问 Pawn），
  后台只处理已经准备好的字符串。
- **文本高度自适应**：`Text.CalcHeight(instructionLabel, width)` 算出动态偏移，
  并用 `try/finally` 恢复 `Text.Font`——避免污染全局 GUI 状态。

### 存档与快照（→ `game-and-world-components.md`）

- `DirectorWorldComponent : WorldComponent` + `ExposeData()` 存跨存档数据：
  每个 Pawn 的 **设置时间 tick + 生物年龄快照 + 人格历史（默认 5 条）**。
- **快照即数据**：`SetTimestamp(pawn, DirectorUtils.BuildCustomCharacterData(pawn, true, false))`
  直接存"当时的资料块文本"，演变时和当前块做文本 diff——**省掉自定义序列化结构**。

### 性能与稳定性（→ `performance-and-gui.md`）

- **全局单飞协调器**（`Source/DirectorAiTaskCoordinator.cs`）：
  `TryAcquire/Release` + `lock` 保证**同一 `World` 同时只有一个 AI 请求**，
  跨 World 自动作废旧 lease——防止批量生成打爆 API 配额。
- **熔断与倍速保护**：超时/异常时在设置页显示原因，点「清除错误」再重新启用；≥3 倍速暂停新扫描。
- **肖像差异检测**（`Source/DirectorPortraitService.cs`）：
  `RequestSize = 512`（发给 AI）、`HistorySize = 160`（本地基准）；
  `GetPixels32()` **逐像素比对**，`changedPixelThreshold = 32` 容忍 JPEG 压缩噪声；
  无变化时**不消耗 image token**。

### 提示词工程（本 mod 最有参考价值的部分）

- 上游 README 附录 **A–H 把每条请求路径的实际格式写成了契约**：
  Context（指令）与 Prompt（数据）分离、`[Character Data]` / `[Update Data]` 块、
  JSON 返回协议、批量请求用 `[ID:<PawnID>]` 分段回填。**这种"把请求格式文档化"的做法值得抄**。
- **双提示词通道**：内置提示词槽（5 个）与 RimTalk 高级预设走**两条不同的上下文路径**，
  高级预设分支**不会**自动附上资料块/备注/返回协议——需要在模板里自己引用变量。
- **省 token 的正则改写**（附录 H）：
  `string.replace` 固定替换，或
  `regex.replace \`^--- ([^\r\n]+) ---\r?$\` "$1:" "m"` 把 `--- Basic Info ---` 压成 `Basic Info:`，
  再 `regex.replace \`(?:\r?\n){3,}\` "\n\n"` 折叠空行——保留栏目标签避免内容粘连。

### 值得记的反面/坑点

- ⚠️ **上游 `About.xml` 的 `loadAfter` 有大小写笔误**：写的是 `cj.RimTalk`，
  而 `modDependencies` 里是 `cj.rimtalk`。packageId 匹配区分大小写，**这条 loadAfter 实际不生效**——
  `publishing-and-release.md` 里「patch 第三方 mod 的 packageId 必须准确」的现成反面教材。
- ⚠️ **`Source/DirectorVariableProvider.cs` 是反编译产物**：文件头带 dnSpy 警告注释，
  注册的是 RimTalk **旧 Mustache 引擎**（`director.*` 变量）的拦截补丁，
  而 README 明确说 RimTalk 1.3 已改 Scriban、`director.*` **不再注册**——
  即这段是**为兼容旧版保留的死路径**，新版走 `DirectorUtils.RenderScribanText`。
  学习时注意区分"活代码"与"遗留兼容代码"。
- ⚠️ **代码风格不统一**：`Patch_PersonaEditorWindow.cs`、`Patch_PromptService.cs` 有中文注释，
  其余补丁基本是英文；`Patch_PersonaEditorWindow.cs:241` 行尾有多余空格。
- ⚠️ **`DirectorUtils.cs` 95 KB 单文件**承担了绝大部分工具函数，是明显的"神仙文件"，
  适合作为**反面教材**（拆分参考），而非模仿对象。

## 目录结构

```
收集/83-patch-RPDPersonaDirector/
├── About/About.xml          # packageId RP.RimTalk.PersonaDirector, author Rabirinth
│   └── PublishedFileId.txt  # 3619548407
├── 1.6/Assemblies/RPD.dll   # 编译产物（282 KB，随源码提交）
├── Defs/MainButtonDefs/DirectorButton.xml   # 底部 RPD 图标（左键备注/右键导演台/Shift 设置）
├── Languages/               # ChineseSimplified + English（Keyed + DefInjected）
│   ├── Keyed/{Autogen,Experimental,Library,Preset,UI}.xml
│   └── DefInjected/MainButtonDef/DirectorButton.xml
├── Source/                  # 48 个 .cs
├── Textures/UI/Buttons/DirectorNotes.png
├── README_UPSTREAM.md       # 上游完整文档（请求格式契约 A–H + 变量索引）
├── RPD.csproj  RPD.sln  RPD.Local.props.example
└── BUILDING.md  .gitignore
```

### Source 分层（47 个补丁类按职责切分）

| 层 | 文件 | 作用 |
|---|---|---|
| 补丁注册 | `DirectorPatchRegistry.cs` | 核心/实验补丁分组开关 |
| 窗口补丁 | `Patch_PersonaEditorWindow.cs` (10K) | 编辑窗口注入 Set Time / Evolve / 备注按钮 |
| 入口接管 | `Patch_GetOrAddNew.cs` (8.6K) | 人格创建/获取、Smart Gen 分流 |
| 入口接管 | `Patch_OverrideRollGen.cs` (3K) | Random Gen → 预设浏览器 |
| 事件触发 | `Patch_AutoEvolveTriggers.cs` (9.7K) | 身份变化/婚姻/生育/亲属死亡/新增特质 |
| 上下文 | `Patch_ContextCapture.cs`、`Patch_PromptService.cs` | 日常对话上下文 + Persona 动态 Scriban 渲染 |
| 肖像 | `Patch_DailyPortraitDialogue.cs` | 对话中肖像变化检测 |
| 历史 | `Patch_PersonaHistory.cs` | 版本记录与回滚 |
| 数据引擎 | `DirectorDataEngine.cs` (29K) | Pawn 资料提取（对应全部 `d_*` 变量） |
| 状态 | `DirectorWorldComponent.cs` (21K) | 跨存档快照/历史 |
| 设置 | `DirectorSettings.cs` (43K) | ModSettings + 资料筛选 + 提示词槽位 |
| 协调 | `DirectorAiTaskCoordinator.cs` | 单飞请求协调 |
| 其他 | `DirectorApiAdapter.cs`、`DirectorPortraitService.cs`、`DirectorFeatureGate.cs` 等 | AI 适配/肖像/功能门 |

## 构建与部署

```bash
# 1. 配置本机引用（路径写在 RPD.Local.props，不入库）
cp RPD.Local.props.example RPD.Local.props
#    RimWorldManagedDir / HarmonyAssemblyPath / RimTalkAssemblyPath

# 2. 构建（VS 2022 或命令行均可）
dotnet build RPD.csproj -c Release      # 产出 bin/Release/RPD.dll

# 3. 部署：把 DLL 拷到 1.6/Assemblies/RPD.dll
```

目标框架 **.NET Framework 4.8**，`LangVersion 7.3`，直接 `<Reference>` 本机
`Assembly-CSharp.dll` / `0Harmony.dll` / `RimTalk.dll`（`<Private>False</Private>`，不打包依赖）。
`RPD.csproj` 内置 `ValidateLocalReferences` target：三个路径任一缺失即编译报错。

> 注意：这是 **.NET Framework 4.8 老式 csproj**（`ToolsVersion 15.0`，非 SDK-style），
> `dotnet build` 需要装 .NET Framework 4.8 Developer Pack。

## 相关文件

- 上游文档：[README_UPSTREAM.md](README_UPSTREAM.md)（含附录 A–H 请求格式、变量索引）
- 构建说明：[BUILDING.md](BUILDING.md)
- 知识库：知识库索引 [../../docs/knowledge/mod-index.md](../../docs/knowledge/mod-index.md)；
  Harmony 补丁 [../../docs/knowledge/harmony-patching.md](../../docs/knowledge/harmony-patching.md)、
  UI/窗口 [../../docs/knowledge/ui-and-windows.md](../../docs/knowledge/ui-and-windows.md)、
  存档组件 [../../docs/knowledge/game-and-world-components.md](../../docs/knowledge/game-and-world-components.md)、
  性能 [../../docs/knowledge/performance-and-gui.md](../../docs/knowledge/performance-and-gui.md)
- 同生态：RimTalk 本体 `../../80-standalone-RimTalk/README.md`、
  记忆扩展 `../../81-patch-RimTalkExpandMemory/README.md`
