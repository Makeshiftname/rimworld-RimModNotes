# 开局方案删不完 Not So Many Policies Notes

## 一句话定位
阻止开局生成海量预置方案——只留「任意」服装方案与「无限制/不提供」食物方案。

## 关键要点
- **服装**：Postfix 拦截 `OutfitDatabase.GenerateStartingOutfits`，`RemoveAll(o => o.label != "OutfitAnything")` 只留「任意」。
- **食物**：Postfix 拦截 `FoodRestrictionDatabase.GenerateStartingFoodRestrictions`，只保留 `FoodRestrictionNothing` 与 `FoodRestrictionLavish`（无限制/不提供）。
- 用药/阅读方案未动（描述明确说明）。
- 注意：命名空间仍是模板 `namespace Template`（残留）。

## 目录结构
```
收集/44-standalone-NotSoManyPolicies/
├── About/About.xml
└── 1.5|1.6/Source/         # Main.cs
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

