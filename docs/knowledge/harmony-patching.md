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

## 8. 相关文件

- 启动/装配样板：`收集/01-standalone-AlertUtility/1.4/Source/AlertUtility.cs`
- 图标补丁：`收集/01-standalone-AlertUtility/1.4/Source/TimerSetWindow.cs`
- 动态探测补丁集：`收集/55-patch-CommonModCompatibilityPatches/1.6/Source/CommonModCompatibilityPatches.cs`
- 启动期手工挂载 + 双路径降级：`收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/PromptService_OngoingEventsPatch.cs`
- 缓存失效补丁组：`收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/Quest_CacheInvalidation_Patches.cs`
- `TargetMethods()` 整族发现 + `__state` 上下文对象：`收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/ThreatCapture.cs`
- 长事件延后预热：`收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/Map_FinalizeInit_OngoingEventsDump_Patch.cs`
- 学习笔记：`收集/01-standalone-AlertUtility/README.md`、`收集/55-patch-CommonModCompatibilityPatches/AGENTS.md`、`收集/84-patch-RimTalkEventPlus/README.md`

## 相关主题

- 组件（每 tick 逻辑）：[game-and-world-components.md](game-and-world-components.md)
- UI/窗口：[ui-and-windows.md](ui-and-windows.md)
