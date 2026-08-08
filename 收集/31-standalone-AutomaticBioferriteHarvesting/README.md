# 自动弹出灵铁 Automatic Bioferrite Harvesting Notes

## 一句话定位
灵铁采集器装满后自动弹出灵铁，无需手动操作。

## 关键要点
- **ThingComp 自动弹出**：`CompBioferriteHarvester.CompTick` 每 `IsHashIntervalTick(250)` 检查 `containedBioferrite >= threshold`（默认 60），满足则 `TakeOutBioferrite()` + `GenPlace.TryPlaceThing`。
- **Gizmo 开关**：`CompGetGizmosExtra` 提供 `Command_Toggle`（图标 `UI/Commands/EjectBioferrite`）。
- **注入 comp**：`Common/Patches/Patches.xml` 用 `PatchOperationAdd`（MayRequire Anomaly）把 comp 注入 `ThingDef["BioferriteHarvester"]/comps`。
- 注意：packageId 拼写为 `Havesting`（漏 r），沿用原样。
- 依赖 Anomaly。

## 目录结构
```
收集/31-standalone-AutomaticBioferriteHarvesting/
├── About/About.xml
├── Common/Patches/         # 注入 comp
└── 1.5|1.6/Source/         # Main.cs
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

