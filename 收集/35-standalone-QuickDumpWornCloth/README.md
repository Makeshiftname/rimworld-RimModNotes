# 远行队快速扔掉死人衣物 Caravan Quick Dump Worn Cloth Notes

## 一句话定位
选中远行队时增加 Gizmo，一键丢弃死者衣物/生物编码物品。

## 关键要点
- **WorldObjectComp**：`WorldObjectComp_DumpWornCloth.GetGizmos` 在车队含可丢物品时才 yield `Command_Action`。
- **筛选**：`Apparel.WornByCorpse`（死者身上的衣物）或 `CompBiocodable.IsBiocoded`（编码给非玩家派系/已编码无主）。
- **丢弃**：`Notify_AbandonedAtTile` + 从库存移除 + `thing.Destroy()` + 重算搬运/食物天数。
- **无 Harmony**：纯 WorldObjectComp 实现（About 残留被注释的 Harmony 依赖块）。

## 目录结构
```
收集/35-standalone-QuickDumpWornCloth/
├── About/About.xml
└── 1.5|1.6/Source/         # Main.cs
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

