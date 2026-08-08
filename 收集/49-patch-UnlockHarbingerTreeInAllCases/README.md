# 在任何情况下解锁厄兆噬树图鉴 Unlock Harbinger Tree In All Cases Notes

## 一句话定位
修复极端生态（如冰盖）下厄兆噬树不生成导致图鉴无法解锁的问题：事件一触发就强制解锁图鉴。

## 关键要点
- 两个 Harmony patch（单文件 `Main.cs`）：
  1. `Patch_IncidentWorker_HarbingerTreeSpawn_TryExecuteWorker.Prefix`：事件触发时若未发现则 `Find.EntityCodex.SetDiscovered(HarbingerTree)` + 发消息。
  2. `Patch_IncidentWorker_SpecialTreeSpawn_CanFireNowSub.Postfix`：`__instance is IncidentWorker_HarbingerTreeSpawn && parms.target is Map m && m.Biome.isExtremeBiome` 时强制 `__result = true`。
- `MyEntityCodexEntryDefOf`（DefOf）：`HarbingerTree`。
- 依赖 Anomaly + Harmony。

## 目录结构
```
收集/49-patch-UnlockHarbingerTreeInAllCases/
├── About/About.xml
└── 1.5|1.6/Source/           # Main.cs
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

