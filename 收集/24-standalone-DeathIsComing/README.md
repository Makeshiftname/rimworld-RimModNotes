# 死神来了 Death Is Coming Notes

## 一句话定位
每天随机点名一名殖民者（孕妇豁免），被点名者死亡。

## 关键要点
- **自定义 IncidentWorker**：`DIC_IncidentWorker_DeathComing.TryExecuteWorker` 遍历玩家地图，收集玩家派系且非孕妇的人形殖民者/奴隶，随机选一个 `pawn.Kill()`。
- **强制触发**：Harmony Prefix 拦截 `CanFireNow` 强制 `__result = true`，配合 IncidentDef 的 `baseChance 0` 实现「每天必触发」。
- **存档**：`DIC_GameComponent : GameComponent` 存 `DeathMercy` 布尔，用 `ExposeData` 存档。
- 命名空间 `DIC`；依赖 Harmony。

## 目录结构
```
24-DeathIsComing/
├── About/About.xml
├── Common/Defs/            # IncidentDef.xml
└── 1.4|1.5/Source/         # Main.cs
```

## 构建
```
cd 1.5/Source && dotnet build -c Release
```

