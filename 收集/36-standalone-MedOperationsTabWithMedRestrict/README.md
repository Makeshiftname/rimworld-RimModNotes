# 在手术页面设置医疗护理 Operations Tab With Med Restrict Notes

## 一句话定位
在健康面板的手术页直接放「用药（医疗护理等级）」选择按钮，同一页管理。

## 关键要点
- **Prefix 插入 UI**：Harmony Prefix 拦截 `HealthCardUtility.DrawMedOperationsTab`，在 `curY` 处插入一行：「AllowMedicine」标签 + `MedicalCareUtility.MedicalCareSelectButton` 按钮。
- **条件校验**：与动物/变异体护理一致（`pawn.Faction == Faction.OfPlayer`、`NonHumanlikeOrWildMan() && InBed()`、变异体需 `entitledToMedicalCare`）。
- 说明：描述写「药物方案」，实际是**医疗护理等级/用药设置**（`MedicalCareSelectButton`）。

## 目录结构
```
收集/36-standalone-MedOperationsTabWithMedRestrict/
├── About/About.xml
└── 1.5|1.6/Source/         # Main.cs
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

