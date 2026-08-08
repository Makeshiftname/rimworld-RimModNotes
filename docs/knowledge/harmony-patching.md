# Harmony Patching（C# 补丁模式）

> 来源 mod：01、07、08、11、12、15、19、20、21、23、28、33、38、39、40、41、49、51、53、55、59、64、71、72、77
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

## 4. 相关文件

- 启动/装配样板：`收集/01-standalone-AlertUtility/1.4/Source/AlertUtility.cs`
- 图标补丁：`收集/01-standalone-AlertUtility/1.4/Source/TimerSetWindow.cs`
- 动态探测补丁集：`收集/55-patch-CommonModCompatibilityPatches/1.6/Source/CommonModCompatibilityPatches.cs`
- 学习笔记：`收集/01-standalone-AlertUtility/README.md`、`收集/55-patch-CommonModCompatibilityPatches/AGENTS.md`

## 相关主题

- 组件（每 tick 逻辑）：[game-and-world-components.md](game-and-world-components.md)
- UI/窗口：[ui-and-windows.md](ui-and-windows.md)
