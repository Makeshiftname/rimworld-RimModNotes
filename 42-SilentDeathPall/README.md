# 安静的死亡迷雾 Silent DeathPall Notes

## 一句话定位
把死亡迷雾批量复活蹒跚怪的吵闹提示改成静默消息。

## 关键要点
- **Prefix 重写** `GameCondition_DeathPall.GameConditionTick`：仅当 `TicksGame % 60 == 0` 且已过 `nextResurrectTick` 才继续，避免高频扫描。
- **复活逻辑**：遍历 `AffectedMaps` 的 `ThingRequestGroup.Corpse`，找 `MutantUtility.CanResurrectAsShambler(corpse) && corpse.Age >= 15000` 的尸体，反射调私有 `ResurrectPawn`。
- **静音关键**：复活提示改用 `Messages.Message(..., MessageTypeDefOf.SilentInput, historical: false)`，且仅在未迷雾时发消息。
- 反射辅助扩展方法（`GetFieldValue<T>` 等）访问非 public 字段（`nextResurrectTick`、`ResurrectIntervalRange`）。
- 依赖 Anomaly + Harmony。

## 目录结构
```
42-SilentDeathPall/
├── About/About.xml
└── 1.5|1.6/Source/         # Main.cs
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

