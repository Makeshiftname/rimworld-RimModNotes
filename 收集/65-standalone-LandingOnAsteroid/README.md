# Landing On Asteroid 着陆小行星 Notes

## 一句话定位
让玩家在生成世界时（Odyssey 轨道层）把小行星作为起始地格。

## 关键要点
- `Page_SelectStartingSite.ExtraOnGUI` Postfix：左上角手绘「查看轨道/表面」层切换 gizmo（`PlanetLayer.Selected`）。
- `DoWindowContents` Postfix：选中 `SpaceMapParent` 子类世界对象时设 `GameInitData.startingTile` + `mapGeneratorDef`，并 `AsteroidMineralConfig.SetPreciousResource`。
- `DoNext` Prefix：按设置配置地图生成器（`AsteroidBasic` / `Base_Player`）。
- `TileFinder.IsValidTileForNewSettlement` Postfix：允许轨道层小行星格（清空 reason）。
- **关键坑**（`Notes.md`）：每个 tile 只能有一个 MapParent，`BasicAsteroidMapParent` 先注册导致 `mapParent?.Map` 恒 null。
- 依赖 Odyssey DLC + Harmony。

## 目录结构
```
收集/65-standalone-LandingOnAsteroid/
├── About/About.xml
├── Common/Patches/          # 给 SpaceMapGenerator 插入 FindPlayerStartSpot
├── Common/Textures/
├── Notes.md                 # 关键坑记录
└── 1.6/Source/Main.cs
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

## 相关文件
- `Notes.md` — 关键技术坑（MapParent 唯一性）

