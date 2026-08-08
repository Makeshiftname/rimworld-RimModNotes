# 解锁区域数量上限排序修复 AreaUnlocker Reorder Fix Notes

## 一句话定位
修复 Area Unlocker 在 1.5 中无法排序活动区的问题。

## 关键要点
- **Prefix 禁用** `AreaManager.SortAreas`（`Log.Warning` + `return false`），阻止原版排序逻辑覆盖 Area Unlocker 扩容后的区域列表。
- 极简补丁：一个 Prefix 禁用一个方法。
- 依赖：Harmony + Area Unlocker（`fluffy.areaunlocker`）。

## 目录结构
```
收集/40-patch-AreaUnlockerReorderFix/
├── About/About.xml
└── 1.5|1.6/Source/         # Main.cs
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

