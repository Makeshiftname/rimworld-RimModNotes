# Dubs Mint Menus 配方右键打开信息卡 Notes

## 一句话定位
让 Dubs Mint Menus 的配方条目支持右键直接打开 InfoCard（物品信息卡）。

## 关键要点
- **IL patch**：`RightClickActionPatch` 用 `Prepare()` 精确定位目标方法，结合 `System.Reflection.Emit` 在菜单按钮绘制流程中插入右键行为。
- **状态跟踪**：`findCallToButtonInvisible`、`recipe`、`returnCodeInstruction` 记录插入点上下文。
- `PlantsViewPatch.cs` 处理植物视图相关菜单。
- 依赖：Dubs Mint Menus（`dubwise.dubsmintmenus`）+ Harmony；`Source/` 内置 `DubsMintMenus.dll` 供编译。
- 参考：`../../docs/knowledge/harmony-patching.md`

## 目录结构
```
20-DubsMenusRightClickAction/
├── About/About.xml
└── Source/                # Main.cs、PlantsViewPatch.cs、Logger.cs + DubsMintMenus.dll
```

## 构建
```
cd Source && dotnet build -c Release
```

