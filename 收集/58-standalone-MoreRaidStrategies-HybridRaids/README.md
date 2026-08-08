# More Raid Strategies: Hybrid Raids 更多的袭击类型：复合袭击 Notes

## 一句话定位
「More Raid Strategies」系列第一个：把原版袭击类型混合成复合袭击（围攻+破墙、空投+破墙同时来袭）。

## 关键要点
- **采用 XML Def + 自定义 worker**：`RaidStrategyWorker_Hybrid.SpawnThreats` 按 `extension.subStrategies` 逐子策略生成，分层算点数（`points × pointsFactor × pointsFactorCurve`），最后按到达方式分组 `Arrive`；`MakeLords` 用反射调子策略 worker 造 LordJob。
- **扩展**：`RaidStrategyDefExtension : DefModExtension` + `SubStrategy`（`def`/`pointsFactor`/`arriveModes`）。
- **Defs**（`Common/Defs/RaidStrategyDefs.xml`）：`HybridRaid` 基类 + `HybridArrival` + 两个具体（`MRS_SiegeAndBreach`：Siege 0.5 + Breach 0.7；`MRS_CenterDropPodAndBreach`）。
- 注意：`Main.cs` 是未清理的模板（只 Log 模板文案）；被注释的 `MRS_Test` 残留。

## 目录结构
```
收集/58-standalone-MoreRaidStrategies-HybridRaids/
├── About/About.xml
├── Common/Defs/RaidStrategyDefs.xml
├── Languages/
└── 1.6/Source/                # Main.cs + RaidStrategyWorker_Hybrid.cs + RaidStrategyDefExtension.cs + PawnsArrivalModeWorker_Hybrid.cs
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

