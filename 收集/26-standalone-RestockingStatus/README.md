# 补货状态 Restocking Status Notes

## 一句话定位
在定居点图标上叠加「刷新」角标，直观显示是否正在补货中。

## 关键要点
- **图标叠加**：Harmony Postfix 拦截 `Settlement.ExpandingIcon`（getter），非敌对派系且 `NextRestockTick > TicksGame` 时替换为叠加图标。
- **纹理合成**：`TextureUtilities` 用 `RenderTexture` + `Graphics.Blit` + `ReadPixels` 把派系图标与角标合成，按 faction 缓存。
- **敌对判定**：`faction.RelationWith(Faction.OfPlayer, allowNull: true)` 规避未建交派系报错。
- 图标来自 flaticon（sync 图标）。

## 目录结构
```
26-RestockingStatus/
├── About/About.xml
├── Common/Textures/
└── 1.4|1.5|1.6/Source/     # Main.cs
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

