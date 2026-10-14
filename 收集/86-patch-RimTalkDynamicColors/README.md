# RimTalk DynamicColors（边缘世谭-言出多彩）Notes（第三方收藏）

## 来源

- **原作者**：Maiya0126（GitHub: [Maiya0126](https://github.com/Maiya0126)）
- **GitHub**：https://github.com/Maiya0126/RimTalkDynamicColors
- **克隆 commit**：`a513b5d`（2026-10-11，"fix: fix 2-line dialogue obstruction in history window by
  properly handling newlines in SplitRichTextFirstLine; add hanging indent support to RimTalk overlay
  with toggle and 5-lang keys"）
- **Steam 创意工坊**：<https://steamcommunity.com/sharedfiles/filedetails/?id=3628773219>
  （见 `About/PublishedFileId.txt`）
- **许可**：⚠️ **上游没有任何 `LICENSE` 文件**，`About.xml` 与 README 均未声明许可，
  `Properties/AssemblyInfo.cs` 只有一行空的 `Copyright © 2025`。
  即**默认保留所有权利**，未获授权不得再分发。本仓库仅作个人学习收藏。
- **状态**：纯学习收藏，未发布、未做运行验证。上游说明见 [README_UPSTREAM.md](README_UPSTREAM.md)。

## 一句话定位

RimTalk 的**纯表现层增强**：不改对话内容，只改「怎么显示」——给每个 pawn 上专属文字色（可自动取
Ideology DLC 的「最爱颜色」）、把括号里的动作描写自动变暗/斜体、自定义关键词高亮，
并把这些富文本**同步到头顶的 Interaction Bubbles 气泡**；另附会话级历史查看器与
TXT / HTML 导出。三种「整活物品」（彩虹糖 / 黑白药丸 / 环缘水）用 Hediff 驱动说话风格。

加载链：Harmony → **Interaction Bubbles** → **RimTalk** → RimTalk DynamicColors。
⚠️ 注意：它**只写了 `loadAfter`，没写 `modDependencies`**（见「反面教材」）。

这是本仓库收的第 6 个 RimTalk 生态 mod，也是**第一个只做 UI 呈现、不碰 prompt 链**的**：
80 提供管线 / 81 记忆 / 83 人格 / 84 事件 / 85 文风 都在改「说什么」，
86 改的是「长什么样」。正因如此它的 Harmony 目标全在**渲染路径**（`Overlay.DrawMessageLog`、
`PawnNameColorUtility.PawnNameColorOf`、`InteractionBubbles` 的 `Draw`/`get_Text`）而非对话管线。

## 值得学习的知识点

### Transpiler（→ [harmony-patching.md](../../docs/knowledge/harmony-patching.md)）

`Patch_CommonTranspiler`（[Source/RimTalkDynamicColors.cs](Source/RimTalkDynamicColors.cs) 第 3714 行起）
是本仓库目前**最值得抄的 transpiler 范例**：

```csharp
var methodLabel   = AccessTools.Method(typeof(Widgets), nameof(Widgets.Label),
                                      new Type[] { typeof(Rect), typeof(string) });
var methodColorize = AccessTools.Method(typeof(DynamicColorMod),
                                       nameof(DynamicColorMod.ColorizeStringForBubbles));
foreach (var instruction in instructions)
{
    if (instruction.Calls(methodLabel))
        yield return new CodeInstruction(OpCodes.Call, methodColorize);
    yield return instruction;   // ← 原指令保留
}
```

**它做的事**：在 `Widgets.Label(Rect, string)` 的 `call` 指令**之前**插入一次
`ColorizeStringForBubbles(string)`。调用瞬间 IL 栈顶就是那个待显示的字符串，
新指令把它弹出、染色后压回，**栈深度不变**，于是原来那条 `call Widgets.Label` 自然就拿到了富文本版本。

- **思路价值**：目标方法签名不能改、又不想要 postfix 拿到结果再回写时，
  **「替换调用点的参数来源」比「拦截返回值」更干净**。
- **必须配套**：替换函数签名要与原调用的**参数类型逐一对应**（这里是 `string → string`），
  且**只对特定重载生效**——`AccessTools.Method(..., new[]{typeof(Rect), typeof(string)})`
  锁死了 `Label(Rect, string)` 这一个重载，`Label(Rect, string, GUIStyle)` 不受影响。
- **它必须手工挂载**：这个类**没有 `[HarmonyPatch]` 特性**（因为目标方法类型来自第三方、
  编译期不可知），所以不能靠 `PatchAll()`，而是在 `TryPatchInteractionBubbles()` 里显式
  `harmony.Patch(drawMethod, transpiler: new HarmonyMethod(typeof(Patch_CommonTranspiler), ...))`。
  **带 transpiler 的补丁几乎总是手工挂载**，这是特性声明方式的边界。

### 反射与软补丁（→ [harmony-patching.md](../../docs/knowledge/harmony-patching.md)）

- **双通道判定第三方 mod 是否存在**（`TryPatchInteractionBubbles`）：
  ```csharp
  bool modActive = ModLister.GetActiveModWithIdentifier("Jaxe.Bubbles") != null;
  if (!modActive && ModLister.AllInstalledMods.Any(x => x.Active && x.Name.Contains("Interaction Bubbles")))
      modActive = true;
  ```
  先按 `packageId` 精确查，再按显示名模糊兜底——**兼容作者改过 packageId 的旧版本**。
  `hasBubblesMod` 静态标志保证幂等，设置页里的「重新检测」按钮会再调一次。
- **结构未知时的自适应字段发现**：
  `foreach (var field in AccessTools.GetDeclaredFields(targetType))
       if (field.FieldType == typeof(Pawn)) BubblePawnFields.Add(field);`
  之后按「第一个 Pawn = 说话者、第二个 = 接收者」取用。**这是应对第三方类内部结构可能变化的聪明做法**，
  代价是**依赖字段声明顺序**，对方调换顺序就静默取错人。
- **目标类型全靠字符串解析**：`AccessTools.TypeByName("RimTalk.UI.Overlay")` →
  `AccessTools.Method(overlayType, "DrawMessageLog")` → `harmony.Patch(...)`，
  全部塞进 `LongEventHandler.ExecuteWhenFinished` 里延后执行，且每步都判 null。
  与 84 / 85 号同一套「启动期反射 + 手工挂载 + 缺目标就静默跳过」的做法。
- **Harmony id 用 `com.rimtalkdynamiccolors.ui`**（反写域名式，与另两个 mod 的
  `saltgin.rimtalkeventmemory` / `RimTalk.StyleExpand` 风格不同，不影响功能）。

### 用「帧号哨兵」把渲染上下文传给深层调用（→ [performance-and-gui.md](../../docs/knowledge/performance-and-gui.md)）

这是本 mod 最巧的一处设计。气泡绘制路径上有一串很深的调用（`Bubble.Draw` → … → `PawnNameColorOf`），
中间的类无法直接改。做法是：

- `Patch_Bubbles_Bubble_Draw.Prefix` 设 `IsDrawingBubble = true` 并记 `lastBubbleFrame = Time.frameCount`，
  同时把反射扫到的 Pawn 存进 `CurrentProcessingPawn` / `CurrentRecipientPawn`；
  `.Postfix` 复位。
- `Patch_PawnNameColorOf_ContextAware.Prefix` 里判断
  `IsDrawingBubble && lastBubbleFrame == Time.frameCount` 才知道「我现在在气泡渲染路径上」。

**帧号校验是关键**：同一帧内 IMGUI 可能有多次嵌套绘制，只靠 bool 会误判；
`Time.frameCount` 保证了哨兵的生命周期不超过一帧。
⚠️ 反过来它的限制也很明确——**该状态是进程级静态的，无法处理真正的重入**：
两个气泡在同一帧内相互嵌套时，后者的 Prefix 会覆盖前者的 pawn。

### Prefix 接管返回值（→ [harmony-patching.md](../../docs/knowledge/harmony-patching.md)）

`Patch_PawnNameColorOf_ContextAware`（第 2341 行）是最标准的「覆盖结果」写法：
按优先级逐条判定——手动指定名 → Ideology `story.favoriteColor` → 阵营色 → 非人类默认色 → 关键词表——
命中就写 `__result` 并 `return false`（跳过原方法），全不命中才 `return true` 放行。
**`return false` + 写 `__result` 是 prefix 覆盖结果的惯用法**，比 postfix 改值更早、成本更低。

### Defs 与 C# 协作（→ [xml-defs-and-patches.md](../../docs/knowledge/xml-defs-and-patches.md)）

[Defs/RimTalkDynamicColors.xml](Defs/RimTalkDynamicColors.xml) 定义 2 个 Hediff + 3 个可制作物品：

- **纯 XML 挂 Hediff**：`IngestionOutcomeDoer_GiveHediff` + `<hediffDef>`，
  彩虹糖 / 黑白药丸各挂一个 `HediffWithComps` + `HediffCompProperties_Disappears`
  （`disappearsAfterTicks 60000~60000`，即一天）。
- **XML 里引 C# 自定义 outcomeDoer**（本 mod 的重点）：
  环缘水用 `<li Class="RimTalkDynamicColors.IngestionOutcomeDoer_RemoveRimTalkEffects" />`，
  C# 侧 `IngestionOutcomeDoer_RemoveRimTalkEffects : IngestionOutcomeDoer` 覆写
  `DoIngestionOutcomeSpecial`，按 `HediffDef.Named("RimTalk_RainbowStatus")` 取到两个 Hediff 并 `RemoveHediff`。
  **「用 XML 声明行为、用 C# 承载逻辑」的标准分工**，类名必须写全命名空间。
- 三个物品都 `ParentName="MakeableDrugBase"` + `recipeMaker` 挂 5 个工作台 + `costList` 1 个 `MedicineHerbal`。
  ⚠️ `ClearSoup` 的 `recipeUsers` 少了 `DrugLab`（另两个有），疑似漏项。

### 设置入口与 UI（→ [ui-and-windows.md](../../docs/knowledge/ui-and-windows.md)）

- **[Defs/MainButtonDef.xml](Defs/MainButtonDef.xml) 往主界面按钮栏插一个图标页**：
  `<defName>RTDC_HistoryTab</defName>` + `<order>79</order>` + `<minimized>True</minimized>`（默认折叠），
  `tabWindowClass` 挂历史窗口、`workerClass` 挂 `MainButtonWorker` 决定图标与提示。
  **想给自己的 mod 加一个独立设置页，`MainButtonDef` 比往 `Mod` 设置里塞更独立。**
- **每帧重复计算的典型优化**：`_colorizeMemo`（`Dictionary<string,string>`）缓存「纯文本 → 富文本」，
  注释写明原因——*同一段纯文本每个 GUI 帧都会被重新染色*。
  容量上限 96 条，`if (_colorizeMemo.Count > 96) _colorizeMemo.Clear()`。
  **这就是 [performance-and-gui.md](../../docs/knowledge/performance-and-gui.md) 那条「GUI 热路径要么缓存要么限长」的实例。**
- **反射句柄全量静态缓存**：`Patch_DrawMessageLog_Manual` 顶部有 **25 个**
  `private static FieldInfo/MethodInfo` 成员（`_cachedMessagesField`、`_drawParticipantsMethod`、
  `_overlayFontSizeField` …），全部在类初始化时解析一次。
  连玩家名都按帧缓存（`_cachedPlayerName` + `_cachedPlayerNameFrame`）。
  **反面教材**：25 个句柄意味着这个类与 RimTalk `Overlay` 的**私有字段名强耦合**——
  RimTalk 一改字段名，对应功能静默消失，没有任何告警。
  （这与 84 号那条「缓存句柄 ≠ 缓存值」是同一课题的两面：缓存了句柄，但没做「句柄为空的可见反馈」。）
- **会话级状态在 `Game.FinalizeInit` 复位**：`Patch_Game_FinalizeInit.Postfix` 清 `SessionHistory`、
  重置 `filterPawnName` / `filterDimMode` / 滚动位置，并 `settings.RebuildCache()` + `ApplyTabVisibility()`。
  **每局游戏重置 UI 与缓存，和 84 号的 `Map_FinalizeInit_OngoingEventsDump` 是同一个纪律。**
- **ModSettings 用 `Scribe_Collections.Look(..., LookMode.Deep)`** 存三个 `IExposable` 列表
  （`nameEntries` / `keywordEntries` / `bracketStyleEntries`），
  并在 `RebuildCache()` 里重建 `_combinedCache` 字典供热路径 O(1) 查。
  **「存档里存对象列表 + 运行时派生扁平缓存」是 UI 配置类的标准形态。**

### 导出（→ [game-and-world-components.md](../../docs/knowledge/game-and-world-components.md)）

`ExportChatLog(bool toHtml, bool silent)`：
- 路径取 `settings.customExportPath`，为空则落 `GenFilePaths.SaveDataFolderPath`；
- 文件名固定前缀 + 秒级时间戳 `RTDC Auto-MM-dd_HH-mm-ss.{html|txt}`；
- HTML 分支按 `<b>{说话者} → {接收者}:</b> {富文本内容}` 逐行拼 `<p>`；
- `Find.TickManager.Pause()` 暂停游戏防导出期间玩家操作；`silent` 参数供**自动导出**调用
  （历史满 200 条时 `autoExportHistory` 会自动导一份再 `ClearSessionHistory`）。
- 复制到剪贴板用 `GUIUtility.systemCopyBuffer`。
- ⚠️ 秒级时间戳 + 固定前缀，意味着**同一秒内两次导出会互相覆盖**。

### 源码内的决策记录（值得学）

`DynamicColorMod` 类顶部（第 312–335 行）留了一段 24 行的**版本事件 + 决策记录**注释块，
记录了 2026-09-17 「决定不新增 AI 内容识别」的完整推理：三个玩家反馈的边缘问题、为什么不做、
RimTalk 10 天内从 v1.2.6 跳到 v1.2.14 且 `BuildFinalRichText` 被移除的兼容负担、以及
四个「未来可选的轻量方案」清单（本地确定性染色 / 无括号表情扫描器 / 自解析 JSONL / 默认关闭的 AI 兜底）。

**「为什么不做某件事」和已放弃方案的备选清单，比任何 changelog 都值钱**——
这是本仓库收录的所有 mod 里最完整的一处设计留痕。

## 反面教材清单

| 类别 | 具体 | 出处 |
|---|---|---|
| 依赖声明缺失 | ⚠️ **只写 `<loadAfter>`（Harmony / Jaxe.Bubbles / cj.rimtalk），完全没有 `<modDependencies>`**，而 README 明确写 Bubbles 与 RimTalk「**必须安装 / Required**」。玩家没装 RimTalk 时**游戏不会给出依赖缺失警告**，只会在 `AccessTools.TypeByName` 返回 null 后静默失效 | `About/About.xml` |
| 描述与功能不符 | README 列 5 项功能，`About.xml` 描述里**只有 4 项**（漏了「关键词高亮」） | `About/About.xml` |
| 版本号不规范 | `<modVersion>1.2.03</modVersion>` 用前导零做第三段，非标准 semver | `About/About.xml` |
| 程序集信息未维护 | `AssemblyInfo.cs` 仍是 VS 模板值：`AssemblyTitle("ClassLibrary2")`、`AssemblyVersion("1.0.0.0")`，与 modVersion `1.2.03` 不符；`Copyright` 为空 | `Properties/AssemblyInfo.cs` |
| 本地路径泄漏 | csproj 的 `PostBuildEvent` 整段是作者本机路径 `D:\games\steamapps\common\Rimworld\Mods\MaiyaMod01 ...`，且用 `%25MOD_DIR%25` 转义、注释写「注意结尾不要带斜杠」 | `RimTalkDynamicColors.csproj` |
| 单文件巨石 | `Source/RimTalkDynamicColors.cs` **210 KB / 3755 行 / 24 个类型**；其中 `Patch_DrawMessageLog_Manual` 一个类约 833 行 | `Source/RimTalkDynamicColors.cs` |
| 补丁声明脆弱 | 全文件**只有 2 处 `[HarmonyPatch]`**（`PawnNameColorUtility.PawnNameColorOf`、`Game.FinalizeInit`），其余补丁类靠 `Patch_` 前缀 + 全程序集按方法名搜索的**类名约定**定位，目标改名即失效 | 同上 |
| Defs 硬编码中文 | `Defs/RimTalkDynamicColors.xml` 与 `Defs/MainButtonDef.xml` 的 `<label>`/`<description>` **直接写中文**，只补了 5 种语言的 DefInjected；玩家若用其他语言会看到中文 | `Defs/` |
| 静默吞异常 | 多处 `catch { }` / `catch (Exception) { }`（如第 3606 行记录对话、第 3441 行） | `Source/RimTalkDynamicColors.cs` |
| 仓库残留研究资料 | 仓库根有两个 **RimTalk 本体的 ILSpy 反编译产物**（`RimTalk_il.txt` 4.9 MB + `RimTalk_il.res`），既非 mod 产物又涉第三方代码 | 已排除，见下 |
| 静态状态不可重入 | `CurrentProcessingPawn` / `IsDrawingBubble` 是进程级静态 + 帧号哨兵，同帧嵌套绘制会互相覆盖 | 同上 |

## 收录时排除的上游文件

| 上游路径 | 大小 | 排除原因 |
|---|---|---|
| `RimTalk_il.txt` | 4.9 MB | RimTalk 本体的 **ILSpy 反编译全文**——作者的开发期参考资料，既非 mod 产物，又是被反编译的第三方代码 |
| `RimTalk_il.res` | 644 B | 同上的附属资源文件 |

> 另：上游仓库的 mod 本体在 `RimTalkDynamicColors/` **子目录**内（仓库根是解决方案目录），
> 本仓库已把 mod 本体内容提升到 `收集/86-patch-RimTalkDynamicColors/` 根下以符合
> `收集/NN-功能-名称/About/About.xml` 的统一约定；`.sln` 与 `.csproj` 一并保留在同层。

## 目录结构

```
86-patch-RimTalkDynamicColors/
├── About/
│   ├── About.xml                 # packageId: Maiya.RimTalkDynamicColors，作者 Maiya0126
│   ├── PublishedFileId.txt       # 3628773219
│   ├── ModIcon.png
│   └── Preview.png
├── Assemblies/
│   └── RimTalkDynamicColors.dll  # 上游预编译产物（本机无 RimWorld，未复现构建）
├── Defs/
│   ├── MainButtonDef.xml         # RTDC_HistoryTab：主界面按钮栏新增一页
│   └── RimTalkDynamicColors.xml  # 2 个 Hediff + 3 个可制作物品
├── Languages/                    # 5 种语言（较 84 号的 24 种少很多）
│   ├── ChineseSimplified/
│   ├── English/
│   ├── Japanese/
│   ├── Korean/
│   └── Russian/
│   └── 每种含 Keyed/ + DefInjected/{HediffDef,ThingDef}/
├── Properties/
│   └── AssemblyInfo.cs           # ⚠️ 仍是 VS 模板值
├── Source/
│   └── RimTalkDynamicColors.cs   # 全部源码：210 KB / 3755 行 / 24 个类型
├── Textures/
│   ├── RimTalk/{RainbowCandy,BlackWhitePill,ClearSoup}.png
│   └── UI/Icons/RTDC_Icon.png
├── RimTalkDynamicColors.csproj   # 旧式（非 SDK-style），HintPath 指向本地 Steam 目录
├── RimTalkDynamicColors.sln
├── README.md                     # 本文件（收藏版学习笔记）
├── README_UPSTREAM.md            # 上游说明（含 1.5 版本 PR 征集）
├── .gitignore
└── .gitattributes                # 全是注释掉的 VS 模板，仅 `* text=auto` 生效
```

## 构建与部署

**前置条件**

1. RimWorld **1.6**（上游 `About.xml` 只声明 1.6；但仓库 README 正在征集 1.5 版本的 PR，1.5 支持尚未落地）
2. csproj 的 `HintPath` 期望本机存在下列路径（**硬编码为三层 `..\`，需按实际安装位置改**）：
   - `Games/steamapps/common/RimWorld/RimWorldWin64_Data/Managed/Assembly-CSharp.dll`（及 UnityEngine 三件套）
   - `Games/steamapps/workshop/content/294100/2009463077/Current/Assemblies/0Harmony.dll`
   - `Games/steamapps/workshop/content/294100/3551203752/1.6/Assemblies/RimTalk.dll`
     （`3551203752` 是 RimTalk 的工坊 ID）

> ⚠️ **本仓库发现的一处生态不一致**：同一个 `packageId` `cj.rimtalk`，在四个已收录 mod 里
> 声明了两个不同的工坊 ID——
> `3551203752`（83 号、84 号的 `About.xml`，以及本 mod 的 csproj HintPath）
> vs `3381946324`（81 号、85 号的 `About.xml`，后者还附了 GitHub 下载地址）。
> 收录时未深究哪个是当前有效条目，**但这是「第三方 mod 的依赖元数据会随时间腐化」的直接实例**：
> RimWorld 只会用 `<steamWorkshopUrl>` 弹下载提示，ID 失效对游戏加载无影响，玩家点不动才发现。

**构建**

```bash
cd RimTalkDynamicColors   # 上游布局下 csproj 在 mod 子目录内
dotnet build -c Release
```

⚠️ 生成的 `PostBuildEvent` 会尝试往作者硬编码的 `D:\games\...` 目录拷贝文件，
换机器必然失败——**编译前先把 `RimTalkDynamicColors.csproj` 里的 `PostBuildEvent` 整段清空**。

**部署 / 加载顺序**

`Harmony` → `Interaction Bubbles` → `RimTalk` → `RimTalk DynamicColors`。
缺少 Interaction Bubbles 时「气泡同步」不可用（其余功能仍工作），缺少 RimTalk 时**大部分功能不可用**
且**不会得到依赖缺失警告**（见反面教材）。

## 相关文件

- 源码：[Source/RimTalkDynamicColors.cs](Source/RimTalkDynamicColors.cs)（单文件，含全部补丁与 UI）
- Defs：[Defs/RimTalkDynamicColors.xml](Defs/RimTalkDynamicColors.xml)、
  [Defs/MainButtonDef.xml](Defs/MainButtonDef.xml)
- 上游说明：[README_UPSTREAM.md](README_UPSTREAM.md)
- 依赖 mod：[80-standalone-RimTalk](../../80-standalone-RimTalk/README.md)（本体）
- 同生态：[83-patch-RPDPersonaDirector](../../83-patch-RPDPersonaDirector/README.md)、
  [84-patch-RimTalkEventPlus](../../84-patch-RimTalkEventPlus/README.md)、
  [85-patch-RimTalkStyleExpand](../../85-patch-RimTalkStyleExpand/README.md)
- 主题文档：[harmony-patching.md](../../docs/knowledge/harmony-patching.md)（Transpiler）、
  [xml-defs-and-patches.md](../../docs/knowledge/xml-defs-and-patches.md)、
  [performance-and-gui.md](../../docs/knowledge/performance-and-gui.md)、
  [publishing-and-release.md](../../docs/knowledge/publishing-and-release.md)