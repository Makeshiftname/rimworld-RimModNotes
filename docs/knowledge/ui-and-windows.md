# UI 与窗口绘制

> 来源 mod：01、26、27、44、45、75
> 适用：自定义窗口、顶部切换栏图标、选项卡、列表布局。

## 1. 自定义窗口生命周期

见 [`收集/01-standalone-AlertUtility/1.4/Source/TimerSetWindow.cs`](../../收集/01-standalone-AlertUtility/1.4/Source/TimerSetWindow.cs) 与
[`收集/01-standalone-AlertUtility/README.md`](../../收集/01-standalone-AlertUtility/README.md)。

- 继承 `Verse.Window`，按需覆写：
  - `SetInitialSizeAndPosition` — 初始大小/位置
  - `PreOpen` — 打开前初始化
  - `DoWindowContents(Rect inRect)` — 主绘制逻辑（必须实现）
  - `PostClose` — 关闭后清理
- 打开：`Find.WindowStack.Add(new XxxWindow(...))`
- 关闭：`Find.WindowStack.TryRemove(typeof(XxxWindow))`

## 2. 常用绘制基元

- `Listing_Standard` — 垂直列表：`Begin(inRect)` → `Label`/`ButtonText`/`Checkbox` → `End()`
- `Widgets` — 低层控件：`Widgets.Label`、`Widgets.ButtonText`、`Widgets.DrawTextureFitted`
- `WidgetRow.ToggleableIcon` — 顶部切换栏的开关图标按钮（见 harmony-patching.md 的 01 例子）
- 文本截断工具类（01 的 `StringExt.Truncate` 扩展）

## 3. GUI 性能红线（重要）

**切勿在 `OnGUI` 里做重活**。详见 [performance-and-gui.md](performance-and-gui.md)：

- RimWorld 用 Unity IMGUI，每次鼠标事件（Move/Drag）都会**全量重跑整个 OnGUI 树**，
  实测约 20ms/次。
- GUI 死亡螺旋：帧率下降 → 排队事件变多 → 遍历次数变多 → 更卡。
- 挂在 OnGUI 里的 FPS 计数器会**虚高**（实测 900+，因为 OnGUI 触发次数≠帧数）。
- 参考解法：在 `UIRoot_Play.UIRootOnGUI` 入口做自适应事件限流（间隔=平滑帧时间÷2，
  范围 60Hz–10Hz），Repaint 事件不拦截。完整设计与数据见
  [`收集/76-standalone-SmoothDragSelect/docs/superpowers/specs/2026-07-27-mouse-event-throttle-design.md`](../../收集/76-standalone-SmoothDragSelect/docs/superpowers/specs/2026-07-27-mouse-event-throttle-design.md)。

## 4. 相关文件

- 窗口与图标：`收集/01-standalone-AlertUtility/1.4/Source/TimerSetWindow.cs`
- GUI 性能笔记：`收集/76-standalone-SmoothDragSelect/README.md`

## 相关主题

- 图标补丁接入：`harmony-patching.md`
- 性能剖析：`performance-and-gui.md`
