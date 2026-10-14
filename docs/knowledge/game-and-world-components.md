# GameComponent / WorldComponent（每 tick 逻辑）

> 来源 mod：01、10、24、26、29、31、58、62、83、84
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

见 [`收集/01-standalone-AlertUtility/1.4/Source/AlertUtility.cs`](../../收集/01-standalone-AlertUtility/1.4/Source/AlertUtility.cs)
与 [`收集/10-standalone-QuestItemWatch/Source/Main.cs`](../../收集/10-standalone-QuestItemWatch/Source/Main.cs)
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

集合用 `Scribe_Collections.Look` 并指定 `LookMode`：
`LookMode.Value`（浅，只存值）/ `LookMode.Deep`（存整个对象图，元素需 `IExposable`）/ `LookMode.Reference`。

## 4. 「要不要 ExposeData」按数据性质决定，不按类决定（84 号）

同一个 mod 里的两个 `GameComponent` 给了正反对照，见
[`收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/QuestCacheComponent.cs`](../../收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/QuestCacheComponent.cs)
与
[`ThreatTrackerComponent.cs`](../../收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/ThreatTrackerComponent.cs)：

| 组件 | 数据性质 | `ExposeData()` |
|---|---|---|
| `QuestCacheComponent` | **纯运行时缓存**（反射查到的 quest→pawn 映射、quest 是否影响某地图） | **空实现** |
| `ThreatTrackerComponent` | **真存档状态**（袭击/偷窃者/通用 Lord 的记录，跨存档要续上） | 三个 `List<T>` 用 `LookMode.Deep` |

```csharp
// 缓存组件：故意不存档，读档后自然重建
public class QuestAffectsMapCacheComponent : GameComponent
{
    public override void ExposeData() { }
}

// 真数据组件
public override void ExposeData()
{
    Scribe_Collections.Look(ref raidThreats,       "raidThreats",       LookMode.Deep);
    Scribe_Collections.Look(ref sightstealerThreats, "sightstealerThreats", LookMode.Deep);
    Scribe_Collections.Look(ref genericLordThreats,   "genericLordThreats",   LookMode.Deep);
}
```

**判据**：这份数据在**读档后还能不能靠游戏数据重算出来**？能 → 空实现（省存档体积，也避免脏缓存）；
不能（记录了游戏里已消失的实体，如已结束的袭击）→ 必须存。
84 号甚至为此专门多开了一个同样空的 `QuestAffectsMapCacheComponent`，把「哪份缓存」这件事显式化。

## 5. 缓存要配失效补丁，否则读到陈旧数据

有 `GameComponent` 缓存就必须配套「什么时候过期」的 Harmony 补丁——
完整的三件套写法见 [harmony-patching.md 的第 5 节](harmony-patching.md)。

## 6. 一次性迁移 + 幂等标志（84 号范式）

数据结构升级时（Def 改名、字段类型变了），在启动期做**一次性迁移**再落盘。
84 号的做法见
[`BlacklistMigrationStartup.cs`](../../收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/BlacklistMigrationStartup.cs)
与
[`BlacklistMigrationHelper.cs`](../../收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/BlacklistMigrationHelper.cs)：

```csharp
[StaticConstructorOnStartup]
public static class BlacklistMigrationStartup
{
    static BlacklistMigrationStartup()
    {
        // 此时 DefDatabase 已就绪，可以安全读 Def
        EventFilterSettings settings = RimTalkEventPlus.Settings;
        bool changed  = BlacklistMigrationHelper.TryMigrateBlacklist(settings);
        changed       |= settings.TryMigrateLegacyTypeFilters();
        changed       |= settings.PruneUnsupportedTypeRules();
        if (changed) RimTalkEventPlus.Instance.WriteSettings();   // 只在真改动时写盘
    }
}
```

四条要点：

1. **迁移挂在 `[StaticConstructorOnStartup]`** 而不是 `Mod` 构造函数——需要 `DefDatabase` 已填充。
2. **`Try*` 返回 bool 表示「是否改动」**，多个迁移用 `|=` 串起来，只在真改动时 `WriteSettings()`，
   避免每次启动都无谓写盘。
3. **幂等靠持久化布尔标志**（`questBlacklistMigrated`），不是靠内存态——否则改档路径会重复迁移。
4. **连 `catch` 分支都把标志置 `true`**：存档损坏导致迁移抛异常时，若不置位则**每次启动都会重复炸一条错误日志**。

迁移 Def 数据时用 `DefDatabase<X>.GetNamedSilentFail(name)` 校验，
**当前整合包里不存在的 Def 直接跳过而不是报错**——这是 mods 之间共享 Def 数据的标准姿势
（玩家未必装了定义这些 Def 的前置 mod）。

## 7. 相关文件

- 组件 + 存档：`收集/01-standalone-AlertUtility/1.4/Source/AlertUtility.cs`
- GameComponent 例子：`收集/10-standalone-QuestItemWatch/Source/Main.cs`
- 缓存组件 vs 真数据组件：`收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/QuestCacheComponent.cs`、`.../ThreatTrackerComponent.cs`
- 设置持久化（`ModSettings.ExposeData` + `filterSchemaVersion`）：`收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/EventFilterSettings.cs`
- 一次性迁移：`收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/BlacklistMigrationStartup.cs`

## 相关主题

- 每 tick 逻辑配合 UI 展示：`ui-and-windows.md`
- 缓存失效补丁：`harmony-patching.md`
