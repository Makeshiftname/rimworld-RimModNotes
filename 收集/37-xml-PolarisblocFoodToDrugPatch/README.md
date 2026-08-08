# 天轴食物加入成瘾品列表 Polarisbloc Food Add To Drug Patch Notes

## 一句话定位
把天轴的两款应急食物加入成瘾品体系，使药物方案可让小人随身携带、战斗时直接食用。

## 关键要点
- **纯 XML**：`Common/Patches/FoodPatches.xml` 两组 `PatchOperationConditional`（匹配 `Polarisbloc_EmergencyFood` 与 `Vanya_CombatRations`），各做三处 `PatchOperationAdd`：
  - ThingDef 根加 `<orderedTakeGroup>Drug</orderedTakeGroup>`；
  - `ingestible` 加 `<drugCategory>Medical</drugCategory>`；
  - `comps` 加 `CompProperties_Drug`（`listOrder` 1200、`teetotalerCanConsume` true）。
- loadAfter 两个天轴派系 mod。
- 备注：`drugCategory=Medical`，作者不确定对文化成瘾品/仅医疗的影响。

## 目录结构
```
收集/37-xml-PolarisblocFoodToDrugPatch/
├── About/About.xml
└── Common/Patches/         # FoodPatches.xml
```

## 相关文件
- `Common/Patches/FoodPatches.xml` — 成瘾品注入
- XML patch 知识：`../../docs/knowledge/xml-defs-and-patches.md`

