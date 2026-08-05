# Highlight Enemies Notes

## 一句话定位
在游戏中添加一个开关，开启后用「?」高亮敌人（含雾中/隐身目标），让玩家更容易发现敌对单位。

## 关键要点
- **每 tick 高亮逻辑**：`EnemyHighlighter : MapComponent`，在 `MapComponentTick()` 里根据开关调用 Highlight / DeHighlight。
- **用 Designation 标记**：`DefDatabase<DesignationDef>.GetNamed("HE_Mark")` 拿自建设计图，把敌人加入标记集合。
- **ModSettings**：`HE_ModSettings` 控制默认开启、是否标记雾中/隐身敌人（`markEnemiesByDefault` / `markEnemiesInFog` / `markEnemiesInvisible`）。
- **MapComponent 生命周期**：构造 `(Map map)` + `FinalizeInit()`（游戏加载完成后初始化）。
- 参考：MapComponent 每 tick + Designation 渲染高亮，见 `../../docs/knowledge/game-and-world-components.md`。

## 目录结构
```
05-HighlightEnemies/
├── About/About.xml
├── 1.4|1.5|1.6/Source/      # Main.cs（EnemyHighlighter）、ModSettings.cs、LogUtility.cs
└── Languages/               # 本地化文本
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

## 相关文件
- `1.6/Source/Main.cs` — 高亮逻辑
- `1.6/Source/ModSettings.cs` — 设置项

