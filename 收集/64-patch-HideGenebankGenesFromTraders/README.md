# Hide Genebank Genes From Traders 基因库里的基因不卖！Notes

## 一句话定位
交易界面隐藏基因库（`CompGenepackContainer`）里的基因，方便基因贸易。

## 关键要点
- `[HarmonyPatch(Pawn_TraderTracker, "ColonyThingsWillingToBuy")]` Postfix：过滤 `Genepack.ParentHolder is CompGenepackContainer`（地面商队）。
- `[HarmonyPatch(TradeUtility, "AllLaunchableThingsForTrade")]` Postfix 同样过滤（轨道贸易船）。
- 均以 `ModsConfig.BiotechActive` 守卫。
- 依赖 Biotech DLC + Harmony。

## 目录结构
```
63-HideGenebankGenesFromTraders/
├── About/About.xml
├── Common/
└── 1.6/Source/Main.cs
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

