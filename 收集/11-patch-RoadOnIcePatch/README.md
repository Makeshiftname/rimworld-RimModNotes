# RoadOnIcePatch Notes

## 一句话定位
让 Roads of the Rim 可以在冰盖/冰海等**所有**生物群系修路：启动时把所有 `BiomeDef.allowRoads` 置为 true。

## 关键要点
- **MapComponent + FinalizeInit**：`MyMapComponent.FinalizeInit()` 在加载完成后遍历 `DefDatabase<BiomeDef>.AllDefs`，逐个 `biome.allowRoads = true`。
- **mod 检测**：`ModLister.GetActiveModWithIdentifier(packageId)` 判断第三方 mod 是否激活。
- **依赖**：`mlie.roadsoftherim`（Roads of the Rim (Continued)），写入 `<loadAfter>` 保证加载顺序。
- 注释掉的备选：只开冰原/海冰（`BiomeDefOf.IceSheet/SeaIce`），现改为全部生物群系。
- 日志：复用 `Logger.cs`（`Log.prefix = "RoadOnIce"`）。

## 目录结构
```
收集/11-patch-RoadOnIcePatch/
├── About/About.xml
└── Source/                # Main.cs（MyMapComponent）、Logger.cs
```

## 构建
```
cd Source && dotnet build -c Release
```

## 相关文件
- `Source/Main.cs` — 生物群系 allowRoads 修改
- Defs 修改知识：`../../docs/knowledge/xml-defs-and-patches.md`

