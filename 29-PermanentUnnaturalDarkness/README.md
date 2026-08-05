# 无尽黑暗 Permanent Darkness Notes

## 一句话定位
把 Anomaly 的异常黑暗变成永久世界状态——没有夜石可打、没有异常生物袭击、无法重见光明。

## 关键要点
- **世界控制器**：`PermanentDarknessController : GameComponent` 管理两阶段（initial 0.5–0.75 天 + main），`ForceWeatherOnAllMaps` 给每张图 `RegisterCondition` 永久 `GameCondition_PermanentDarkness`，`HandleLetters` 按 tick 发三封信（初始/警告/main）。
- **自定义天气条件**：`GameCondition_PermanentDarkness : GameCondition_ForceWeather`，用 `GameCondition_NoSunlight.EclipseSkyColors` 天空、`WeatherOverlay_UnnaturalDarkness` 覆盖层、暗度可调（`GameConditionDraw` 改 `MatBases.Darkness.color`）。
- **黑暗暴露伤害**：`PDHediff_DarknessExposure : Hediff` 周期性掉血（忽略护甲），写 BattleLog，`TryMergeWith` 返回 false。
- **降雨监控**：`RainMonitorMapComponent` 每 300 tick 检查火灾/温度越界则转雨。
- **ModSettings**：`darknessLevel` 滑条（0–2）+ `shadowControl`（调试阴影）。
- 依赖 Anomaly；与 32（饥荒扩展）是系列；`PLAN.md` 为设计文档。

## 目录结构
```
29-PermanentUnnaturalDarkness/
├── About/About.xml
├── PLAN.md                 # 设计文档
├── 1.5|1.6/Source/         # 7 个 cs
├── 1.5|1.6/Defs/           # GameConditionDefs/HediffDefs/IncidentDefs/WeatherDefs
├── Languages/
└── Patches/
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

## 相关文件
- `PLAN.md` — 设计文档
- 组件/条件知识：`../../docs/knowledge/game-and-world-components.md`

