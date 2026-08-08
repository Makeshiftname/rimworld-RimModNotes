# 目标线 Target Line Notes

## 一句话定位
从每个小人向其目标画线，敌我颜色区分，方便观察敌方意图。

## 关键要点
- **绘制 hook**：Harmony Postfix 拦截 `Pawn_StanceTracker.StanceTrackerDraw` → `DrawTargetLine`。
- **两种线**：`Stance_Warmup` 时画 小人→`focusTarg` 单条线；否则画 小人→`CurJob.targetA`→`targetB`→`targetC` 三段线。
- **颜色**：敌对 `Red/Orange/Magenta`，非敌对 `Green/Blue/Cyan`；用 `GenDraw.DrawLineBetween`；目标 Thing 用 `DrawPos`、否则用 `Cell.ToVector3Shifted()`。
- **ModSettings**：`showTargetLine` 总开关 + 敌/非敌分开关（`Scribe_Values` 存档）。
- **警告**：packageId 仍是模板 `RunningBugs.modname`，发布前必须改。

## 目录结构
```
收集/45-standalone-TargetLine/
├── About/About.xml
├── Common/Languages/       # 中英
└── 1.5|1.6/Source/         # Main.cs
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

