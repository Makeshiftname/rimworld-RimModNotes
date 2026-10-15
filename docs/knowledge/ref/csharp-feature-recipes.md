# C# 功能配方：FloatMenuOptionProvider / Job + JobDriver + Toil

> 参考手册层。蒸馏自 rimworldwiki `Modding Tutorials` vetted 区，**不含仓库特定结论**。
> 写新 mod 的骨架与日志模板见 [project-templates.md](../project-templates.md)。
>
> 来源页（最后核对 2026-10-15）：
> [Example FloatMenuOptionProvider](https://rimworldwiki.com/wiki/Modding_Tutorials/Code_FloatMenuOptionProvider) ·
> [Example Mending Job](https://rimworldwiki.com/wiki/Modding_Tutorials/Code_MendingJob)
>
> 所属 wiki 章节：C# Guides
>
> 官方样例仓库：
> [RWDevathon/ExampleFloatMenuOptionProvider](https://github.com/RWDevathon/ExampleFloatMenuOptionProvider) ·
> [RWDevathon/ExampleMending](https://github.com/RWDevathon/ExampleMending)

---

## 1. FloatMenuOptionProvider

### 1.1 为什么 1.6 才有这个类

1.6 之前，浮动菜单选项由**一个大一统的方法**处理，导致 mod 很难扩展且容易冲突。
1.6 起拆成 `FloatMenuOptionProvider`，让每个选项独立成类。

1.6 原版用例：命令玩家 pawn 装备/丢弃武器服装、逮捕/救援/拾取/调情、
与遗物终端等任务物件交互。

### 1.2 骨架

```csharp
using RimWorld;
using Verse;

namespace ExampleFloatMenuOptionProvider
{
    public class FloatMenuOptionProvider_Example : FloatMenuOptionProvider
    {
        // 这三个是抽象属性，没有默认值，必须覆写
        protected override bool Drafted => true;      // 已征召时也提供
        protected override bool Undrafted => true;    // 未征召时也提供
        protected override bool Multiselect => false; // 不支持多选

        protected override FloatMenuOption GetSingleOptionFor(
            Pawn clickedPawn, FloatMenuContext context)
        {
            return new FloatMenuOption("Example_KillPawn".Translate(clickedPawn), () =>
            {
                clickedPawn.Kill(null);
                Messages.Message(
                    "Example_KilledWithTheirMind".Translate(context.FirstSelectedPawn, clickedPawn),
                    MessageTypeDefOf.PositiveEvent,
                    historical: false);
            });
        }
    }
}
```

- 🔑 **游戏会自动发现 `FloatMenuOptionProvider` 的所有子类**，
  定义类即可用，不需要注册。
- 这三个只是**唯一抽象的三个属性**。控制「选项是否出现」还有别的属性和方法，
  去反编译 `FloatMenuOptionProvider` 看完整列表和默认值。
- **action 传 `null` ⇒ 选项显示但置灰不可选**——这是区分「可用/不可用」的手法。
- **`GetSingleOptionFor` 返回 `null` ⇒ 完全不显示这个选项。**
- `FloatMenuContext` 里有当前选中目标，`context.FirstSelectedPawn` 最常用。
- `Messages.Message` 是屏幕左上角的小字提示，用于**不值得发完整 Letter 的临时信息**。
  `historical: false` 表示**不写进历史标签页**。

### 1.3 代码里的字符串必须走 `Translate`

代码里写死的字符串没法翻译。改用 `.Translate()`：

```csharp
new FloatMenuOption("Example_KillPawn".Translate(clickedPawn), () => { … })
```

游戏会到 `Languages/<语言>/Keyed/` 下找这些键：

```xml
<!-- Languages/English/Keyed/FloatMenuOptions.xml -->
<LanguageData>
  <Example_KillPawn>让 {0} 死</Example_KillPawn>
</LanguageData>

<!-- Languages/English/Keyed/Messages.xml -->
<LanguageData>
  <Example_KilledWithTheirMind>{0} 用心智杀死了 {1}</Example_KilledWithTheirMind>
</LanguageData>
```

- **`{0}`、`{1}` 是传给 `Translate` 的第 0、1 个参数**，顺序对应。
- `Keyed` 下的**文件名无所谓**，但建议按用途分文件——
  因为从 XML 里看不出某个键在 C# 里被用在哪。
- ⚠️ **键名必须与代码里的字符串完全一致**，否则游戏里会直接显示键名本身
  （开 dev mode 会看到所谓 "zalgo text" 来提醒你这多半不是有意为之）。

### 1.4 用不同结果表达「可用 / 不可用」

```csharp
protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
{
    if (clickedPawn.Faction != Faction.OfPlayer)
    {
        return new FloatMenuOption("Example_KillPawn".Translate(clickedPawn), () => { … });
    }
    return new FloatMenuOption("Example_CannotKillPlayerPawns".Translate(clickedPawn), null);
}
```

前者可点，后者置灰——比「不显示」对玩家更友好，因为能看到「为什么不行」。

---

## 2. Job / JobDriver / Toil 三者关系

| 概念 | 职责 |
|---|---|
| **JobDef**（XML） | 声明式数据：字符串、`driverClass` 指向哪个 JobDriver 等 |
| **Job**（运行时对象） | 一次具体任务 |
| **JobDriver**（C# worker） | 决定**怎么完成**、何时中止、能否合法启动、结果是什么 |
| **Toil** | Job 的**单个子步骤**。JobDriver 把 Job 拆成一串 Toil |

### 2.1 完整生命周期

1. `FloatMenuOptionProvider` 给玩家一个选项 → 从 JobDef 造出 Job。
2. JobDef（XML）声明数据和 JobDriver，RimWorld 据此创建 Job。
3. JobDriver 启动，**先预约（reserve）目标**，防止别人抢。
4. 预约成功 → 生成一系列 Toil，pawn 依次完成。
5. 全部 Toil 完成，或 Job 被取消/完成。
6. Job 销毁，pawn 按常态拿新 Job（征召中＝等玩家指令；未征召＝回工作分配）。

> 🔑 **「一个 pawn 永远有 Job」——倒地、已卸载（despawned）、甚至死亡的 pawn 都有 Job。**

---

## 3. 完整配方：修补（mend）工作

### 3.1 FloatMenuOptionProvider（Thing 重载）

⚠️ 上面的教程覆写的是 `GetSingleOptionFor(Pawn, …)`；要作用于物件要用
**`GetSingleOptionFor(Thing, …)` 重载**。

```csharp
using RimWorld;
using Verse;
using Verse.AI;

public class FloatMenuOptionProvider_MendingExample : FloatMenuOptionProvider
{
    protected override bool Drafted => true;
    protected override bool Undrafted => true;
    protected override bool Multiselect => false;

    protected override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
    {
        // ⚠️ 便宜的判断必须放前面——float menu provider 会在大多数右键输入上运行
        if (!clickedThing.def.useHitPoints
            || clickedThing.MaxHitPoints <= clickedThing.HitPoints
            || !context.FirstSelectedPawn.CanReach(clickedThing, PathEndMode.Touch, Danger.Deadly)
            || clickedThing.IsBurning())
        {
            return null;
        }

        if (clickedThing is Apparel || clickedThing.HasComp<CompEquippable>())
        {
            return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption("Example_MendItem".Translate(clickedThing.Label), () =>
                {
                    Job job = JobMaker.MakeJob(
                        Example_JobDefOf.Example_MendingJob,
                        new LocalTargetInfo(clickedThing));
                    context.FirstSelectedPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }),
                context.FirstSelectedPawn,
                clickedThing);
        }
        return null;
    }
}
```

### 3.2 四种必须隐藏选项的情况

| 判断 | 原因 |
|---|---|
| `!def.useHitPoints` | 不可破坏（钢锭等）。这条**计算开销极低**，放最前面能省下大量无谓检查 |
| `MaxHitPoints <= HitPoints` | 已满血，修它没意义 |
| `!CanReach(thing, PathEndMode.Touch, Danger.Deadly)` | 够不着。**不查的话下一步创建的 Job 可能直接报错** |
| `IsBurning()` | 着火的东西不该修 |

> 💡 这四条能显著减少「创建了 Job 却启动失败」的情况——
> 那类失败会打出「Job was created but failed to start」警告，
> 通常说明上游（provider）判断有问题。

### 3.3 JobDef

```xml
<!-- Defs/Example_JobDefs.xml -->
<Defs>
  <JobDef>
    <defName>Example_MendingJob</defName>
    <driverClass>ExampleMendingJob.JobDriver_Mend</driverClass>
    <reportString>mending TargetA.</reportString>
    <suspendable>false</suspendable>
  </JobDef>
</Defs>
```

- `reportString` 显示在 pawn 检查面板上表示它在干嘛；
  **`TargetA` 会自动翻译成它正在交互物件的标签**。
- `suspendable=false` 让这个优先任务不会被随机打断。

### 3.4 DefOf 模式

```csharp
using RimWorld;
using Verse;

namespace ExampleMendingJob
{
    [DefOf]
    public class Example_JobDefOf
    {
        static Example_JobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(Example_JobDefOf));
        }

        public static JobDef Example_MendingJob;
    }
}
```

- `[DefOf]` 告诉 RimWorld：Def 加载完后扫描这个类，
  用**变量名匹配同名 `defName` 的 Def**填值。
- DefOf **可以引用任何 mod 甚至原版的任意类型 Def**——
  Ludeon 并没有把所有 Def 都放进自己的 DefOf，你可以自己建。
- 静态构造里的 `EnsureInitializedInCtor` 唯一目的是**在 Def 未加载完就被使用时报警告**。
  此时值还是 null，会引发错误——它只报警告，**不阻止错误发生**。
- 也可以用 `DefDatabase<JobDef>.GetNamed(...)` 等方式，各有取舍。
  ⚠️ **DefOf 名字写错而 Def 又真不存在，运行时会报错且后果可能很严重。**

### 3.5 JobDriver

```csharp
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace ExampleMendingJob
{
    public class JobDriver_Mend : JobDriver
    {
        public int ticksToNextMend = 0;
        public const int ticksBetweenMending = GenTicks.TicksPerRealSecond; // 60 ticks

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(TargetIndex.A).Thing, job);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            Toil mend = ToilMaker.MakeToil("JobDriver_Mend_Toil");

            mend.initAction = () => { ticksToNextMend = ticksBetweenMending; };

            mend.tickAction = () =>
            {
                Pawn actor = mend.actor;
                actor.rotationTracker.FaceTarget(TargetThingA);

                ticksToNextMend--;
                if (ticksToNextMend <= 0f)
                {
                    ticksToNextMend += ticksBetweenMending;
                    if (TargetThingA.HitPoints < TargetThingA.MaxHitPoints)
                    {
                        TargetThingA.HitPoints++;
                    }
                    else
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                    }
                }
            };

            mend.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            mend.defaultCompleteMode = ToilCompleteMode.Never;
            mend.handlingFacing = true;

            yield return mend;
        }
    }
}
```

#### `TryMakePreToilReservations`

- 负责**预约**即将操作的东西，返回是否成功。
- 返回 false ⇒ **Job 在开始前就被中止**，通常伴随「Job was created but failed to start」警告。
- ⚠️ **不要在这里跑任意 C#**——此时 Job 还没正式开始，
  这里只该做预约和最后的临门检查。

#### `MakeNewToils`

- 返回的 Toil 全部完成 = Job 完成。每个 Toil 具体干什么由 JobDriver 自己决定。
- **Toils 是一次性全部创建并返回的**（Job 创建时），
  **不是**轮到某个 Toil 时才创建。
  → **写每个 Toil 时要当作彼此独立**，用 `initAction` 之类初始化各自的状态。
- 现成工具类值得挖：寻路直接用 `Toils_Goto.GotoThing`。
  成功后可假定 pawn 已触碰到目标；失败（如途中目标被毁）则 Job 取消。

#### Toil 常用字段

| 成员 | 作用 |
|---|---|
| `initAction` | 该 Toil **开始时**执行一次 |
| `tickAction` | **每 tick** 执行，主体逻辑放这 |
| `actor` | 干活的 pawn |
| `FailOnCannotTouch(index, pathEnd)` | 摸不到目标就失败 |
| `defaultCompleteMode` | 默认完成时机。`Never` = 必须自己显式结束 |
| `handlingFacing` | 设为 true 则朝向由 Toil 自己处理（配合 `FaceTarget`） |

- Toil 的名字只是调试用，**不需要翻译**。
- Job 结束方式不止一种：有的靠延时器，有的靠 Toil 全部完成，有的靠被打断。
  范例里由 mend Toil 自己调 `EndCurrentJob(JobCondition.Succeeded)` 收尾。

---

## 4. 调试 Job 的顺序

按这个链路自上而下查：

1. **provider 判断得对不对**——该出现时出现、不该出现时不出现。
2. **Job 造对了吗**——JobDef 对不对？`driverClass` 指向的 JobDriver 对不对？
3. **JobDriver 内部**——再逐个 Toil 查为什么行为异常。

**务必也测「该不出现时确实不出现」**，否则会遇到很难查的怪问题。

---

## 5. 官方留下的改进空间（很好的练手清单）

- 木头、酒这类「因为是武器所以可修」的物件该不该被排除掉？
- mend Toil 在物品满血后还多花一秒才结束——能否改 `tickAction` 省下这些 tick？
- 没检查动手能力：没手的 pawn 不该能修枪。
- 没关联技能：原版多数 Job 会给技能经验并受技能影响工作速度。
  修补是否该吃 crafting 技能？
- 没有音效与视觉效果。
- 修补目前免费——是否该收费、影响品质、或产出副产品？

> 💡 **强烈建议动手前先反编译几个原版 JobDriver**——
> 里面有大量能省你时间的工具类和方法。
