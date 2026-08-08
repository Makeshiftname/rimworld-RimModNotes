# Better Outfit Stand (Vanilla Improvement) 更好用的服装架 (原版改进) Notes

## 一句话定位
改进原版服装架：右键弹双栏窗口一键换装 + 服装/武器「放到目标服装架」命令。

## 关键要点
- `Building_BetterOutfitStand : Building_OutfitStand`（+ `Building_BetterKidOutfitStand` 子类）：覆写 `GetFloatMenuOptions` 打开 `Dialog_ChooseApparel`；`EnsureHeldItemsAllowed`（PostLoadInit 允许架上物品）。
- `Dialog_ChooseApparel : Window`：左右双栏「放到衣架上/从衣架上穿」复选框；确认后置转移列表 + `SetAllowHauling(false)` + 下单 `UseOutfitStand_Better` Job。
- `JobDriver_UseOutfitStand_Extension`：分两条分支处理双向转移。
- `OutfitStandHaulGizmoUtility`：`ThingWithComps.GetGizmos` Postfix 给已生成服装/武器追加「放到目标服装架」命令（validator 限玩家服装架，自动 `AllowDefOnStand`）。
- 需 XML 把原版服装架替换为 `Building_BetterOutfitStand`（Common/Patches）。
- 过滤自动禁用功能已暂停（`EnableAutomaticFilterDisable=false`）；有静态白盒测试。
- 依赖 Harmony。

## 目录结构
```
68-BetterOutfitStand/
├── About/About.xml
├── Common/
├── Tests/whitebox/test_better_outfit_stand_static.py
└── 1.6/Source/              # BetterOutfitStands.cs、Dialog_ChooseOutfit.cs、HarmonyPatches.cs、OutfitStandHaulGizmoUtility.cs 等
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

## 相关文件
- `Tests/whitebox/` — 白盒测试

