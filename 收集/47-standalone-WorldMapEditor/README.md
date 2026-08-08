# 世界地图编辑器 World Map Editor Notes

## 一句话定位
游戏中随时在世界地图上编辑地块（生物群落/丘陵度/定居点），通过给每个地块创建隐形世界对象实现。

## 关键要点
- **隐形世界对象**：`InvisibleWorldObject : WorldObject`（每地块一个），`InvisibleObjectWorldGenStep` 在 `GenerateFresh` 遍历 `WorldGrid.TilesCount` 创建。
- **编辑 Gizmo**：`WorldMapEditorComp.GetGizmos` 提供 4 个：DestroySettlement（直接移除，不触发事件/任务信号）、GenerateSettlement（FloatMenu 选非玩家派系）、SetBiome、SetHilliness。
- **切换图标**：Harmony patch `PlaySettings.DoPlaySettingsGlobalControls` 加编辑模式图标，开关时动态增删隐形对象。
- **存档优化**：`WorldObjectsHolder_ExposeData_Patch.Prefix` 保存前自动移除所有隐形对象。
- 性能已知较差（每地块一个世界对象，拖拽最多 80 块），About 公开征集优化/PR。

## 目录结构
```
收集/47-standalone-WorldMapEditor/
├── About/About.xml
├── Common/Defs/UsefulDefs/   # WorldObjectDef + WorldGenStepDef
├── Common/Textures/
└── 1.5|1.6/Source/           # Main.cs（单文件）
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

