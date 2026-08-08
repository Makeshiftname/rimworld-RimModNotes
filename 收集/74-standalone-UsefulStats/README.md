# Useful Stats 实用数据表 Notes

## 一句话定位
主标签数据表比较可制作物品效率：市场价值/工作量、单一材料数量/工作量（含当前与未来解锁）。

## 关键要点
- `Core/CraftableEfficiencyMetrics`：`Ratio`/`PerDisplayedWork`（rawWork/60）/`FormatRatio`（自适应小数位）。
- `Core/CraftableStatRow` + `MaterialVariantStat`：`AvailableNow`/`FutureAvailable`、`ValuePerWork`、`IngredientPerWorkMin/Max`、`DefaultValuePerMaterial`、`MaterialVariants`。
- `UI/CraftableRowBuilder.BuildRows(map)`：三来源——RecipeDef（排除 `IsSurgery`/无单产物）、recipeMaker ThingDef、`BuildableByPlayer` Building；单一材料变体聚合（`MaxMaterialVariantsForSummary=256`，摘要 `25-250 x stuff (24)` + 可展开具体材料）。
- `UI/MainTabWindow_UsefulStats : MainTabWindow`：Current/Future/All-only 过滤、动态 Kind 分类 picker、搜索（物品/defName/配方/原料）、**虚拟化滚动**（只画可见行，过滤/排序结果缓存非每帧重建）。
- `DESIGN.md`：硬编码英文标签待本地化、无 CSV 导出、第一版隐藏 Recipe/User 与 Status 列；`tools/generate_ui_mockup.py` 可生成 UI mockup PNG。
- 无第三方依赖（纯 Verse/RimWorld）。

## 目录结构
```
收集/74-standalone-UsefulStats/
├── About/About.xml
├── DESIGN.md                 # 设计文档
├── 1.6/Source/Core/          # CraftableEfficiencyMetrics.cs、CraftableStatRow.cs
├── 1.6/Source/UI/            # CraftableRowBuilder.cs、MainTabWindow_UsefulStats.cs
├── 1.6/Defs/Misc/            # 主按钮 def
├── tests/UsefulStats.Tests/  # C# 单测
└── tools/generate_ui_mockup.py
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

## 相关文件
- `DESIGN.md` — 设计文档
- `tests/UsefulStats.Tests/` — C# 单测
