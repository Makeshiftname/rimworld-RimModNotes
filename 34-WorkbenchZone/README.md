# 工作台存储区 Workbench Zone Notes

## 一句话定位
点工作台一键创建与其配方所需半径匹配的（空）存储区。

## 关键要点
- **核心洞察**（来自 `1.6/Source/Notes.md`）：原版不允许在工作台所在格直接建库，所以从工作台交互格（InteractionCell）用 FloodFill 扩展。
- **Gizmo**：`CanCreateZone : ThingComp` 提供 `Command_Action`（`WzCreateWorkbenchZone`，图标 `UI/Designators/ZoneCreate_Stockpile`）。
- **半径计算**：取所有 bill 的 `ingredientSearchRadius` 最小值（再减 0.5），`GenRadial.RadialCellsAround` 圈范围，`map.floodFiller.FloodFill` 以交互格为中心扩散，判据含 `Designator_ZoneAdd.IsZoneableCell`。
- **过滤合并**：新建 `Zone_Stockpile` 用 `ThingFilterExtension.MergeAll` 合并各 bill 的 `ingredientFilter`；交互格已有库则并入，非库区则发消息 `WorkbenchHasZoneNonStockpile`。
- **ModSettings**：`ZoneSettings`/`ZoneSettingsUI : Mod`，`maxRadius` 滑条（1–40，`Scribe_Values` 存档）。
- `Common/Patches/Patches.xml` 把 `CanCreateZone` comp 注入所有 `ThingDef/comps`。

## 目录结构
```
34-WorkbenchZone/
├── About/About.xml
├── Common/Patches/         # 注入 comp
├── 1.5|1.6/Source/         # Main.cs、Logger.cs、Notes.md
└── 1.5|1.6/Assemblies/     # WorkbenchZone.dll
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

## 相关文件
- `1.6/Source/Notes.md` — 学习笔记
- 组件/Gizmo 知识：`../../docs/knowledge/game-and-world-components.md`

