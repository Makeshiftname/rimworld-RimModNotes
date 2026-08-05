# Simple GUI Library and Gallery Notes

## 一句话定位
自用的简易 GUI 库 + 演示画廊：封装常用 UI 组件与顶部切换栏图标的注册，供后续 mod 复用（packageId `RunningBugs.GUI`）。

## 关键要点
- **切换栏图标注册**：`ToggleIconData.setupToggleIcon(typeof(SampleWindow), icon, tooltip, sound, openAction)` 一行注册顶部图标，点击打开窗口。
- **窗口演示**：`SampleWindow : Window` 展示库内组件用法（Gallery）。
- **工具类**：`Utility.cs` 放共享 GUI 辅助；`Log.cs` 是日志封装（`Log.prefix = "SimpleGUI"`）。
- **装配**：`[StaticConstructorOnStartup]` 初始化图标 + `Harmony.PatchAll`。
- 支持 1.4 / 1.5（无 1.6 目录）。

## 目录结构
```
09-GUILib/
├── About/About.xml
├── 1.4|1.5/Source/        # Main.cs、SampleWindow.cs、Utility.cs、Log.cs
└── Textures/              # 图标资源
```

## 构建
```
cd 1.5/Source && dotnet build -c Release
```

## 相关文件
- `1.5/Source/Main.cs` — 图标注册与启动
- `1.5/Source/SampleWindow.cs` — GUI 组件演示
- UI 知识：`../../docs/knowledge/ui-and-windows.md`

