# UI 与窗口绘制

> 来源 mod：01、26、27、44、45、75、84
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

## 4. Mod 自带设置页的最小骨架（84 号）

不想单开一个 `Window`、只想挂在游戏选项里的 mod 设置，覆写 `Mod` 的两个方法即可，
见 [`收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/RimTalkEventPlus.cs`](../../收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/RimTalkEventPlus.cs)：

```csharp
public class RimTalkEventPlus : Mod
{
    public static EventFilterSettings Settings;

    public RimTalkEventPlus(ModContentPack content) : base(content)
    {
        Settings = GetSettings<EventFilterSettings>();   // 加载 ModSettings
    }

    // 设置页归到哪个分类；必须 Translate() 出可读名并写进 Languages/
    public override string SettingsCategory() => "RimTalkEventPlus_SettingsCategory".Translate();

    // 把 inRect 转发给自己的 UI 模块
    public override void DoSettingsWindowContents(Rect inRect) => EventFilterUI.DoFilteringUI(inRect, Settings);
}
```

要点：**分类名走 `Translate()` 键**（翻译文件里是 `<Lang>/Keyed/RimTalkEventPlus_Settings.xml`），
不要硬编码字符串；`Mod` 主类只做「加载设置 + 转发」，真正的绘制逻辑拆到独立静态类里。

## 5. 过滤/分类能力必须按类别声明（84 号反面教材）

带分类的过滤 UI，最容易犯的错是「所有类别画一样的控件」。84 号给了正解——
用能力查询函数驱动 UI：

```csharp
public static bool SupportsTypeFiltering(EventCategory category)
    => category == EventCategory.Quest
    || category == EventCategory.MapCondition
    || category == EventCategory.SitePart;      // ⚠️ Threat 不在其中

public static bool SupportsInstanceFiltering(EventCategory category)
    => category == EventCategory.Quest;          // 只有任务支持实例级
```

**为什么 Threat 不支持类型过滤**：威胁是运行时生成的 `Lord` 实例，**没有对应的 Def**，
按类型屏蔽无从谈起，只能按实例走。**能力差异来自数据模型，不是 UI 偷懒。**

配套的第二个坑：**界面值与生效值要分离**。

```csharp
public bool showQuests = true;                                   // 用户勾选值
public bool IsEnhancedPromptLockActive => /* 第三方 mod 冲突锁 */;
public bool ShowQuestsEffective => showQuests && !IsEnhancedPromptLockActive;  // 实际生效值
```

UI 读 `Effective`（并在顶部画冲突横幅提示已被锁定），业务代码各读各的并自行判断，边界清楚。
**别让「用户想要」和「系统允许」共用一个字段。**

## 6. 大 UI 文件的拆分思路与规模红线

84 号的 `EventFilterUI.cs` 是 **1099 行 / 53.7 KB**，本仓库目前最大的单个 UI 类
（仅 83 号的 `DirectorUtils.cs` 95 KB 可比）。它的内部结构值得学、规模不值得学：

- 按小节切静态方法：`RenderOptimizationSection` / `RenderAdvancedModeSection` /
  `RenderCategoryFiltersSection` / `DoTypeBasedFilteringSection` / `DoInstanceBasedFilteringSection`
- 私有 `struct TwoColumnLayout` 承载分栏布局，把坐标计算从绘制代码里抽出来
- 纯绘制辅助独立成 `Draw*`：`DrawHorizontalDivider` / `DrawSectionHeader` / `DrawResetButtons`

**反面点**：单文件超过 ~600 行 / 30 KB 就该拆了；这类文件在原版 API 大改时几乎无法合并冲突。

## 7. 设置入口的两种放法：`Mod` 设置页 vs `MainButtonDef`（86 号）

第 4 节的骨架是「塞进 Mod 设置列表」。当功能有自己的复杂窗口（如历史查看器）时，
更合适的做法是**在主界面按钮栏插一个独立图标页**，86 号的 `Defs/MainButtonDef.xml` 只有 13 行：

```xml
<MainButtonDef>
  <defName>RTDC_HistoryTab</defName>
  <label>言出多彩</label>
  <description>左键：查看历史记录\n右键：打开 Mod 设置</description>
  <tabWindowClass>RimTalkDynamicColors.RimTalkHistoryWindow</tabWindowClass>
  <workerClass>RimTalkDynamicColors.MainButtonWorker_RimTalkHistory</workerClass>
  <order>79</order>              <!-- 决定排序位置 -->
  <minimized>True</minimized>    <!-- 默认折叠，不抢主界面空间 -->
  <iconPath>UI/Icons/RTDC_Icon</iconPath>
</MainButtonDef>
```

对比两种放法：

| | `Mod` 设置页（第 4 节） | `MainButtonDef` |
|---|---|---|
| 入口位置 | Mod 设置列表里的一行 | 主界面顶部按钮栏 |
| 是否需 C# | 需要（覆写 `SettingsCategory` 等） | 纯 XML 即可挂窗口，图标/提示用 `workerClass` |
| 适合 | 开关、参数、过滤器 | 有独立窗口的重功能 |
| 空间 | 受限，需折叠分区 | 一个图标位，`minimized` 可默认收起 |

`MainButtonWorker` 决定图标与「未读/高亮」提示的绘制；`tabWindowClass` 挂的窗口就是点开后的内容。
86 号的 worker 还把右键分给了 Mod 设置，形成「左键主功能 / 右键设置」的紧凑交互。

⚠️ 同一份 `About.xml` 的 `<description>` 与 README 的功能列表必须同步——86 号 README 列了 5 项功能，
`About.xml` 只写了 4 项（漏掉「关键词高亮」），工坊页面就少一个卖点。

## 8. 相关文件

- 窗口与图标：`收集/01-standalone-AlertUtility/1.4/Source/TimerSetWindow.cs`
- GUI 性能笔记：`收集/76-standalone-SmoothDragSelect/README.md`
- 设置页骨架 + 过滤 UI：`收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/RimTalkEventPlus.cs`、`.../EventFilterUI.cs`
- 过滤能力声明与设置持久化：`收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/EventFilterSettings.cs`
- `MainButtonDef` 独立设置页 + 颜色选择器/历史窗口：`收集/86-patch-RimTalkDynamicColors/Defs/MainButtonDef.xml`、`.../Source/RimTalkDynamicColors.cs`

## 相关主题

- 图标补丁接入：`harmony-patching.md`
- 性能剖析：`performance-and-gui.md`
