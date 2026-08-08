# More Responsive Planet 世界视角UI响应改进 Notes

## 一句话定位
用独立拖拽框 + 后台线程 + Unity MonoBehaviour 重做世界视图选择逻辑，改善 1.6 世界视角卡顿。

## 关键要点
- `[HarmonyPatch(WorldSelector, "WorldSelectorOnGUI")]` Prefix 全量接管：自绘 `ImmediateDragBox`（禁用原版 `selector.dragBox`）。
- 单击同步处理 `ProcessSingleClickImmediate`（colonist bar 命中 → 连选/循环切换/选 tile）；拖拽 → `ThreadSafeSelectionProcessor.ProcessDragSelectionAsync` 后台线程计算，`dragId` 使旧任务失效（`IsCancelled`）。
- `UnityMainThreadDispatcher : MonoBehaviour`（`DontDestroyOnLoad` + `ConcurrentQueue<Action>`）把后台结果投递回主线程。
- `ScreenshotCache` 拖拽期间截图缓存；右键菜单/商队移动 `AutoOrderToTile` 复刻原版逻辑（`CaravanExitMapUtility` 等）。
- 依赖 Harmony；纯 1.6。

## 目录结构
```
收集/62-standalone-MoreResponsivePlanet/
├── About/About.xml
├── Common/
└── 1.6/Source/              # Main.cs、ImmediateDragBox.cs、ThreadSafeSelectionProcessor.cs、UnityMainThreadDispatcher.cs、ScreenshotCache.cs
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

