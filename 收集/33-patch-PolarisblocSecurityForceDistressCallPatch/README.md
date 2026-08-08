# Polarisbloc Security Force Distress Call Patch Notes

## 一句话定位
修复 Anomaly 求救信号任务与安保部队（Security Force）派系的尸体生成不兼容问题（尸体瞬间腐烂无法搜刮）。

## 关键要点
- **Prefix 重实现** `DistressCallUtility.SpawnCorpses`（`return false` 跳过原版）。
- **保持尸体新鲜**：`HealthUtility.SimulateKilledByPawn` 模拟击杀后设 `corpse.timeOfDeath`、给 `CompRottable.RotProgress` 补上地图存在时长；随机落点 `GenSpawn.Spawn` + `DropAndForbidEverything` + 撒血渍。
- **干尸腐化**：处理 `IsFleshBeast()` 生成 `Filth_TwistedFlesh`。
- 依赖：Polarisbloc - Security Force（`vanya.polarisbloc.securityforce.tmp`）；Harmony id `com.RunningBugs.SFDistressCallFix`。

## 目录结构
```
收集/33-patch-PolarisblocSecurityForceDistressCallPatch/
├── About/About.xml
├── 1.5/Source/             # Main.cs
└── 1.5/Assemblies/         # SFDistressCallFix.dll
```

## 构建
```
cd 1.5/Source && dotnet build -c Release
```

