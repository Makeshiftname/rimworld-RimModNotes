# GameComponent / WorldComponent（每 tick 逻辑）

> 来源 mod：01、10、24、26、29、31、57、61
> 适用：需要在游戏每 tick 持续运行、跨存档/跨地图维护状态的逻辑。

## 1. 两种组件怎么选

| 组件 | 生命周期 | 适用 |
|---|---|---|
| `GameComponent` | 每个存档一个，随游戏加载/保存 | 存档相关的全局状态 |
| `WorldComponent` | 星球世界层，长存 | 与地图无关的全局逻辑（如 01 的倒计时、24 的机制） |

两者都会被游戏**自动创建**，无需手动实例化——继承并写构造函数即可（构造函数带
`Game` / `World` 参数，由框架注入）。

## 2. 每 tick 回调

```csharp
public class AlertUtility : WorldComponent
{
    public AlertUtility(World world) : base(world) { }

    public override void WorldComponentTick()
    {
        base.WorldComponentTick();
        // 每 tick 执行的逻辑
    }
}
```

- `GameComponentTick()` 同理。
- 想按时间/频率触发，可在内部计数 tick（01 用 `defaultInterval = 60` 即每"秒"检查一次，
  慢速模式下一个游戏 tick = 1/60 秒）。

见 [`01-AlertUtility/1.4/Source/AlertUtility.cs`](../../01-AlertUtility/1.4/Source/AlertUtility.cs)
与 [`10-QuestItemWatch/Source/Main.cs`](../../10-QuestItemWatch/Source/Main.cs)
（`QuestItemWatcher : GameComponent` + `GameComponentTick`）。

## 3. 存档兼容（IExposable + Scribe）

组件内的自定义数据若要随存档保存，实现 `IExposable` 并用 `Scribe_Values.Look`：

```csharp
public class Event : IExposable
{
    public int presetGameTicksToAlert;
    public string message;

    public void ExposeData()
    {
        Scribe_Values.Look(ref presetGameTicksToAlert, "RunningBugs.AlertUtility.Event.ticks");
        Scribe_Values.Look(ref message, "RunningBugs.AlertUtility.Event.message");
    }
}
```

键名建议用 `packageId.类名.字段名` 避免跨 mod 冲突（见 01 的写法）。

## 4. 相关文件

- 组件 + 存档：`01-AlertUtility/1.4/Source/AlertUtility.cs`
- GameComponent 例子：`10-QuestItemWatch/Source/Main.cs`

## 相关主题

- 每 tick 逻辑配合 UI 展示：`ui-and-windows.md`
