# ItemPolicy 物品策略 Notes

## 一句话定位
主标签页「物品策略」mod：为每个小人设定随身携带的指定物品与数量，功能类似 Carry More 但实现完全不同（无需改 XML）。

## 关键要点
- `ItemPolicyUtility : GameComponent`，静态 `Dictionary<Pawn, ItemPolicy> policies`；`FinalizeInit()` 预建 label→ThingDef 索引（`DefDict`/`Search`）供搜索。
- `ItemPolicy : IExposable`（`Dictionary<ThingDef,int> data`），`MergePolicy` 实现粘贴合并。
- **主标签** `MainTabWindow_Items : MainTabWindow_PawnTable` + `PawnTableDef`（列含 CopyPasteItemPolicy 复制粘贴列、ItemPolicyPawnColumn 打开设置）；`Dialog_ItemPolicy : Window`（QuickSearchWidget 搜索、数量编辑、删除）。
- **自定义 ThinkNode** `JobGiver_TakeItemForInventoryStock`（6000–9000 tick 间隔、`MassUtility` 容量限制、long max_to_hold 防溢出），通过 `Common/Patches/Patches.xml` 挂入 Humanlike 思考树 `TakeForInventoryStock` 节点。
- Harmony patch `JobGiver_DropUnusedInventory.ShouldKeepDrugInInventory`（Postfix 强制保留策略内药品）。
- `ItemPolicyController.notes` 记录了已解决问题（FinalizeInit NRE、药品 pickup 死循环）。

## 目录结构
```
收集/02-standalone-ItemPolicy/
├── About/About.xml
├── CHANGELOG.md
├── Common/Defs/Misc/ItemPolicy.xml
├── Common/Patches/           # 挂入思考树
└── 1.4|1.5|1.6/Source/       # ItemPolicy.cs、ItemPolicyController.cs、ItemPolicyView.cs、LogUtility.cs
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```