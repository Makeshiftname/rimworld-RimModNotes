# Free Go To 自由的移动目标点 Notes

## 一句话定位
允许把已征召小人/机械体的移动目标点拖动到被其他小人占用的格子上。

## 关键要点
- **自研 `SinglePawnGotoController`**（镜像原版 `MultiPawnGotoController` 简化为单小人）：`StartInteraction`/`ProcessInputEvents`/`FinalizeInteraction`（`RCellFinder.BestOrderedGotoDestNear` 找合法落点）/`Draw`/`OnGUI`。
- **5 个 Harmony patch**（单文件 Main.cs，针对 Selector/MapInterface）：`Selector_HandleMapClicks_Prefix`（return false 整体重写输入）、`Selector_HandleMultiselectGoto_Prefix`（单个征召时用新控制器）、`SelectorOnGUI`/`MapInterfaceUpdate` Postfix（绘制）、`ClearSelection` Postfix（取消激活）。
- 背景：1.6 原版禁止拖到占用格（但允许右键点格子移动）。
- packageId `RunningBugs.FreeGoTo` 与目录名不同。

## 目录结构
```
58-DraftPawnCanGoToOccupiedCell/
├── About/About.xml
└── 1.6/Source/                # Main.cs
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

