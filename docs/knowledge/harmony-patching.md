# Harmony Patching（C# 补丁模式）

> 来源 mod：01、07、08、11、12、15、19、20、21、23、28、33、38、39、40、41、49、51、53、55、59、64、71、72、77、83、84
> 适用：对原版或第三方 mod 的功能做运行时修改。

本仓库的 Harmony 补丁有 **两种风格**，分别适用于不同场景。

## 1. Attribute 风格（声明式，适合静态目标）

早期补丁 mod 的标准写法，见 [`收集/01-standalone-AlertUtility/1.4/Source/TimerSetWindow.cs`](../../收集/01-standalone-AlertUtility/1.4/Source/TimerSetWindow.cs)：

```csharp
[HarmonyPatch(typeof(PlaySettings), "DoPlaySettingsGlobalControls", MethodType.Normal)]
public class ToggleIconPatcher
{
    [HarmonyPostfix]
    public static void AddIcon(WidgetRow row, bool worldView)
    {
        // 在顶部切换栏追加一个图标按钮
        row.ToggleableIcon(ref flag, ContentFinder<Texture2D>.Get("UI/timer_mail", true),
            "AlertUtility".Translate(), SoundDefOf.Mouseover_ButtonToggle, null);
    }
}
```

启动装配（`[StaticConstructorOnStartup]` 会在游戏启动时执行一次）：

```csharp
[StaticConstructorOnStartup]
public static class LoadingScreen
{
    static LoadingScreen()
    {
        var harmony = new Harmony("com.RunningBugs.AlertUtility"); // 全仓唯一 ID
        harmony.PatchAll(Assembly.GetExecutingAssembly());          // 扫描所有 [HarmonyPatch]
    }
}
```

见 [`收集/01-standalone-AlertUtility/1.4/Source/AlertUtility.cs`](../../收集/01-standalone-AlertUtility/1.4/Source/AlertUtility.cs)。

## 2. Manual 风格（动态探测，适合目标可能缺失/改名的第三方 mod）

`收集/55-patch-CommonModCompatibilityPatches` 用 `TryApply(Harmony)` 模式：每个补丁组一个
`internal static class`，启动时逐个尝试，目标 mod/类型/方法不存在就静默跳过并返回 `false`。
见 [`收集/55-patch-CommonModCompatibilityPatches/1.6/Source/CommonModCompatibilityPatches.cs`](../../收集/55-patch-CommonModCompatibilityPatches/1.6/Source/CommonModCompatibilityPatches.cs) 与其
[`AGENTS.md`](../../收集/55-patch-CommonModCompatibilityPatches/AGENTS.md)（含维护约定）。

```csharp
public static bool TryApply(Harmony harmony)
{
    Type componentType = AccessTools.TypeByName("NewRatkin.GameComponent_WanderingCaravan");
    MethodInfo target = AccessTools.Method(componentType, "CleanupDeadPawns");
    if (target == null) return false;              // 目标不存在 → 跳过
    MethodInfo prefix = AccessTools.Method(typeof(NewRatkinWanderingCaravanCompatibility), nameof(Prefix));
    harmony.Patch(target, prefix: new HarmonyMethod(prefix));
    return true;
}
```

**为什么优先 manual 风格**：第三方 mod 升级后方法签名/类名常变，attribute 风格会在
PatchAll 时直接抛异常，manual 风格可按需 `AccessTools.Method(..., new[]{...})`
带参数类型精确匹配，找不到就跳过。

## 3. 常用技巧

- **给原版 `PlaySettings` 切换栏加图标**：patch `DoPlaySettingsGlobalControls`，postfix 里用
  `WidgetRow.ToggleableIcon`，配合 `Find.WindowStack.IsOpen(typeof(Window))` 判断开关状态
  （见 01 的 `ToggleIconPatcher`）。
- **动画/渲染 hook**：`Pawn_DrawTracker.Notify_MeleeAttackOn` 触发近战动作回调，配合
  `Pawn.IsGhoul` 限制目标（见 [`收集/75-standalone-GhoulAttackSpin/README.md`](../../收集/75-standalone-GhoulAttackSpin/README.md)）。
- **多 prefix/finalizer**：一个 patch 可以同时挂 `prefix` 与 `finalizer`（见 55 的
  `NalsDynamicPortraitsWorkItemsCompatibility`）。
- **依赖顺序**：patch 第三方 mod 时在其 `packageId` 写入 `About.xml` 的 `<loadAfter>`，
  不写入 `<modDependencies>`（依赖=必须存在，loadAfter=只是排序，见 55 的 AGENTS.md 约定）。
  ⚠️ 注意这只是**单向 patch 不要求对方存在**时的约定；若确实要求对方在场（如 83/84 都要 RimTalk），
  应写 `<modDependencies>`，否则游戏不会因为缺 RimTalk 而拒绝加载。详见
  [publishing-and-release.md](publishing-and-release.md)。
- **`__state` 承载自定义上下文对象**（见 [`收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/ThreatCapture.cs`](../../收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/ThreatCapture.cs)）：
  `Prefix(..., out ThreatCaptureContext __state)` 建作用域，`Postfix(__state, __result)` 按返回值决定是否提交。
  **把「这次调用期间的临时状态」用 `__state` 传出，比用静态字段干净**——天然并发安全、不会跨调用串味。

## 4. 手工挂载的第三种时机：启动期装配（84 号）

上面两种风格是「Mod 构造函数里 `PatchAll()`」与「外部逐组 `TryApply`」。84 号给出第三种：
**完全不用 `[HarmonyPatch]` 特性，改在 `[StaticConstructorOnStartup]` 里反射解析后 `harmony.Patch(...)` 手工挂载**。
见 [`收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/PromptService_OngoingEventsPatch.cs`](../../收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/PromptService_OngoingEventsPatch.cs)。

代码注释写明动机：**被 patch 的 RimTalk 存在早期静态初始化问题**（`RimTalk.Data.Constant`），
特性声明式会在其初始化完成前就去解析目标类型而炸掉；推迟到启动期就安全了。

```csharp
[StaticConstructorOnStartup]
public static class PromptService_OngoingEventsPatcher
{
    static PromptService_OngoingEventsPatcher()
    {
        var harmony  = new Harmony("saltgin.rimtalkeventmemory.prompt");
        var svcType  = AccessTools.TypeByName("RimTalk.Service.PromptService");
        var reqType  = AccessTools.TypeByName("RimTalk.Data.TalkRequest");
        if (svcType == null || reqType == null) { Log.Warning("…skipping"); return; }

        // 按完整参数类型数组定位，避免同名重载打错
        var method = AccessTools.Method(svcType, "DecoratePrompt",
            new[] { reqType, typeof(List<Pawn>), typeof(string) });
        if (method == null) { Log.Warning("…skipping"); return; }

        harmony.Patch(method, postfix: new HarmonyMethod(AccessTools.Method(typeof(Patch), "Postfix")));
    }
}
```

配套的**运行期能力探测 + 双路径降级**（同一文件）：先找 `TalkRequest.Context` 属性（新版 RimTalk
注入系统指令），找不到就退到 `talkRequest.Prompt`（旧版注入用户消息）。解析函数带一次性标志，
失败只 `Log.Warning` 不抛。**跨 RimTalk 大版本兼容的教科书写法。**

## 5. 缓存失效补丁组（84 号范式）

缓存型 `GameComponent` 必须配套一组「什么时候缓存过期」的补丁，否则会读到陈旧数据。
84 号的 `QuestCacheComponent` 配了三处，是**目前仓库里最完整的一套**：

| 补丁目标 | 触发判定 | 文件 |
|---|---|---|
| `Quest.Notify_SignalReceived` | Prefix 用 `signal.global \|\| signal.tag.StartsWith($"Quest{id}.")` **复刻原版分发规则**，命中才 Postfix 失效 | [`Quest_CacheInvalidation_Patches.cs`](../../收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/Quest_CacheInvalidation_Patches.cs) |
| `Quest.AddPart` / `RemovePart` | `__state` 记 `PartsListForReading.Count`，Postfix 比数量变化；**额外判 `QuestState.Ongoing`** 跳过建造期无效调用 | 同上（`TargetMethods()` yield 两个） |
| `Quest.End` | 独立小补丁，防已完成任务残留进 per-game 缓存 | [`Quest_End_ClearCache_Patch.cs`](../../收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/Quest_End_ClearCache_Patch.cs) |

要点：**第一条不是无脑全失效**——它先复刻原版 signal 分发条件，只在信号真的属于这个 quest 时才失效。
这是「补丁要贴着原版语义写」的好例子。

## 6. 给整族覆写方法打补丁：`TargetMethods()` 动态发现

`QuestPart.ReplacePawnReferences` 是虚方法，且**大量子类实现并不调用 base**，
所以只 patch 基类方法会漏掉绝大多数子类。84 号遍历所有已加载类型逐个 yield：

```csharp
private static IEnumerable<MethodBase> TargetMethods()
{
    var parameters = new[] { typeof(Pawn), typeof(Pawn) };
    foreach (Type type in AccessTools.AllTypes())            // 全程序集扫
    {
        if (type == typeof(QuestPart) || !typeof(QuestPart).IsAssignableFrom(type)
            || type.ContainsGenericParameters) continue;
        MethodInfo m = AccessTools.DeclaredMethod(type, nameof(QuestPart.ReplacePawnReferences), parameters);
        if (m != null && !m.IsAbstract && !m.IsStatic && !m.ContainsGenericParameters)
            yield return m;
    }
}
```

过滤条件四件套：**排除基类本身 / 只取继承链上的类型 / 跳过开放泛型 / 跳过抽象与静态**。
这种扫描有一次性成本，宜放在 `[StaticConstructorOnStartup]` 而非 `TargetMethods()` 每帧调用——
Harmony 只在打补丁时枚举一次，但要确保返回的 `IEnumerable` 是惰性的（上面用的就是 yield）。

## 7. 把重活推出关键路径：`LongEventHandler`

`Map.FinalizeInit` 的 Postfix 里**不直接做反射预热**，而是延到长事件结束后再跑：

```csharp
LongEventHandler.ExecuteWhenFinished(() =>
{
    // 地图可能在回调执行前已被移除，必须重新校验
    if (Current.Game == null || Find.Maps == null || !Find.Maps.Contains(__instance)) return;
    Current.Game.GetComponent<QuestCacheComponent>()?.PrewarmActiveQuestsForMap(__instance);
});
```

两个要点：**回调里必须重新校验对象有效性**（地图会没了），以及**预热类逻辑整段包 try/catch**——
84 号的注释写着 "Prewarm is optional; never let it interfere with gameplay"，单个任务失败只 `Log.Warning`。

## 8. Transpiler：替换调用点的参数来源（86 号范式）

目标方法签名不能改、又不想要 postfix 拿到结果再回写时，可以**改 IL：在某个 `call` 指令前插入一条
自己的调用**，让栈顶的参数被换掉。86 号的 `Patch_CommonTranspiler` 是本仓库目前最干净的范例：

```csharp
var methodLabel    = AccessTools.Method(typeof(Widgets), nameof(Widgets.Label),
                                       new Type[] { typeof(Rect), typeof(string) });
var methodColorize = AccessTools.Method(typeof(DynamicColorMod),
                                       nameof(DynamicColorMod.ColorizeStringForBubbles));
foreach (var instruction in instructions)
{
    if (instruction.Calls(methodLabel))
        yield return new CodeInstruction(OpCodes.Call, methodColorize);
    yield return instruction;   // ← 原指令保留，让它照常执行
}
```

`ColorizeStringForBubbles(string) → string` 执行时，IL 栈顶正好是待显示的字符串：它弹出、染色、
再压回，**栈深度不变**，原来那条 `call Widgets.Label` 自然就拿到了富文本版本。

三条硬性要求：

1. **替换函数的签名必须与原调用的参数类型逐一对应**（此处 `string → string`），
   否则栈会失衡，运行时抛 `InvalidProgramException`。
2. **必须按完整重载签名锁定目标**：`AccessTools.Method(..., new[]{typeof(Rect), typeof(string)})`
   只命中 `Label(Rect, string)`，`Label(Rect, string, GUIStyle)` 不受影响。
3. **带 transpiler 的补丁通常只能手工挂载**：目标类型来自第三方、编译期不可知，
   拿不到可用于 `[HarmonyPatch(typeof(X), ...)]` 的类型引用。86 号的
   `Patch_CommonTranspiler` **没有 `[HarmonyPatch]` 特性**，而是在
   `TryPatchInteractionBubbles()` 里显式
   `harmony.Patch(drawMethod, transpiler: new HarmonyMethod(typeof(Patch_CommonTranspiler), "Transpiler"))`。
   这与第 4 节的「启动期装配」是同一个动作，只是多了 transpiler 参数。

对比第 3 节的常用技巧：**postfix 改 `__result` 是「事后修」，transpiler 换参数是「事前换」**。
需要在原方法内部逻辑之前就改变输入时，才值得付 transpiler 的复杂度与脆弱性。

## 9. 对接第三方公共 API：确定性 ID + upsert 注册（85 号范式）

给 RimTalk 这类提供公共 API 的 mod 注册内容时，最容易写出「每次启动都多一条」的 bug。
85 号的 `RimTalkAPIIntegration` 是标准解法，两个部件缺一不可：

**① 确定性 ID —— 两边算出的值必须一致**

```csharp
// 优先用对方提供的生成器，保证与对方内部的 ID 规则对齐
var generate = _promptEntryType.GetMethod("GenerateDeterministicId",
                                          BindingFlags.Public | BindingFlags.Static);
if (generate != null) return (string)generate.Invoke(null, new object[] { modId, name });

// 兜底：本地规则 sanitize（. - 空格 → _），并保证兜底规则本身跨启动稳定
return $"mod_{modId.Replace(".", "_").Replace("-", "_").Replace(" ", "_")}_{name.Replace(" ", "_")}";
```

**② upsert —— 先查后写**

```csharp
var existing = preset.GetType().GetMethod("GetEntry")?.Invoke(preset, new object[] { entryId });
if (existing != null) { SetProperty(existing, "Content", NewContent); return; }  // 只更新，不重建
// 没有才 Activator.CreateInstance 新建，再按名插入到锚点之后
```

这样「首次创建」与「更新模板文本」走同一条幂等路径，重开游戏不会重复堆积条目。
若再配一个静态 `_initialized` 标志（85 号的 `Initialize()` 开头就判一次），就同时防住了
同一次启动内的重复执行。参见第 6 节的「一次性迁移 + 幂等标志」。

配套的两个反射技巧：

- **属性/字段双通道写入**：对方可能把 public 字段改成属性（或反过来），
  `SetProperty()` 先试 `GetProperty(name).CanWrite`，失败再退 `GetField(name)`——改哪个都不炸。
- **装配体靠 AppDomain 遍历发现**：`<Private>false</Private>` 引用的第三方 DLL 不随本 mod 复制，
  编译期类型引用在运行时不可用，只能
  `AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "RimTalk")`。
  详见 [cross-version-structure.md](cross-version-structure.md)。

## 10. 编译期引用第三方 DLL ≠ 编译期使用其类型（85 号反面教材）

85 号的 csproj **确实** `<Reference Include="RimTalk">` 了，但补丁层全部写成：

```csharp
[HarmonyPatch("RimTalk.Prompt.PromptManager", "BuildMessages")]      // 字符串定位
public static bool BuildMessagesPrefix(object __instance, object talkRequest, ...)  // 参数按 object
```

**好处是 RimTalk 内部改名/重构时 mod 仍能加载，只是功能静默降级**，
而不是在 `PatchAll()` 抛异常炸掉整个 mod（一个坏补丁拖垮整个列表，参见 83 号的
`[HarmonyPrepare]` 兜底）。85 号用一个总闸把降级收口：

```csharp
public static bool IsRimTalkAvailable() => _talkRequestType != null && _promptManagerType != null;
// 每个补丁最外层第一句先判启用与能力，缺目标就原样放行：
if (!StyleExpandSettings.Instance?.IsEnabled ?? true) return true;
```

**降级要降在补丁最外层，不要散落到每个功能分支里。**

⚠️ 代价同样真实：85 号的 `BuildMessagesPostfix` 用 `GetField("Item1")/GetField("Item2")` 反射读写
`ValueTuple` 元素、再经索引器写回 `List<T>`——**能改，但极脆**，RimTalk 换成 class 或改名就静默失效，
外面包着 try/catch 只打 Warning。这类「反射改泛型集合元素」的代码应视为临时手段。

## 11. 结构未知时的自适应字段发现（86 号）

目标类字段名完全不可知、但**类型**已知时，可以按类型扫出来用。86 号找 Interaction Bubbles
里「谁在说、谁在听」的两个 Pawn 字段：

```csharp
foreach (var field in AccessTools.GetDeclaredFields(targetType))
    if (field.FieldType == typeof(Pawn)) BubblePawnFields.Add(field);
// 取用约定：第一个 = 说话者，第二个 = 接收者
```

比硬编码字段名更耐改版，但**依赖字段声明顺序**——对方调换顺序就静默取错人。
配一个静态 `List<FieldInfo>` 缓存 + 幂等标志，只在首次探测时扫一次。
配套的还有 §11 之前的双通道 mod 探测（83 号范式，见
[publishing-and-release.md 第 5 节](publishing-and-release.md)）：

```csharp
bool active = ModLister.GetActiveModWithIdentifier("Jaxe.Bubbles") != null;      // 精确
if (!active) active = ModLister.AllInstalledMods
                        .Any(x => x.Active && x.Name.Contains("Interaction Bubbles"));  // 名称兜底
```

## 12. 相关文件

- 启动/装配样板：`收集/01-standalone-AlertUtility/1.4/Source/AlertUtility.cs`
- 图标补丁：`收集/01-standalone-AlertUtility/1.4/Source/TimerSetWindow.cs`
- 动态探测补丁集：`收集/55-patch-CommonModCompatibilityPatches/1.6/Source/CommonModCompatibilityPatches.cs`
- 启动期手工挂载 + 双路径降级：`收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/PromptService_OngoingEventsPatch.cs`
- 缓存失效补丁组：`收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/Quest_CacheInvalidation_Patches.cs`
- `TargetMethods()` 整族发现 + `__state` 上下文对象：`收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/ThreatCapture.cs`
- 长事件延后预热：`收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/Map_FinalizeInit_OngoingEventsDump_Patch.cs`
- 字符串定位 + `object` 签名 + 能力总闸：`收集/85-patch-RimTalkStyleExpand/Source/Patches/RimTalkPatches.cs`
- 确定性 ID + upsert 注册 + 双通道属性写入：`收集/85-patch-RimTalkStyleExpand/Source/API/RimTalkAPIIntegration.cs`
- Transpiler 换参数 + 自适应字段发现 + 帧号哨兵：`收集/86-patch-RimTalkDynamicColors/Source/RimTalkDynamicColors.cs`
- 学习笔记：`收集/01-standalone-AlertUtility/README.md`、`收集/55-patch-CommonModCompatibilityPatches/AGENTS.md`、`收集/84-patch-RimTalkEventPlus/README.md`、`收集/85-patch-RimTalkStyleExpand/README.md`、`收集/86-patch-RimTalkDynamicColors/README.md`

## 相关主题

- 组件（每 tick 逻辑）：[game-and-world-components.md](game-and-world-components.md)
- UI/窗口：[ui-and-windows.md](ui-and-windows.md)
