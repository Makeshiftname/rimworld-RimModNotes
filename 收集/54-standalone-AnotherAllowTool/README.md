# Another Allow Tool (1.6) Notes

## 一句话定位
老版 Allow Tool 的精选功能子集重实现，目标之一是去掉 HugsLib 依赖（1.6 专用）。

## 关键要点（5 大功能）
- **Haul Urgently**：`Designator_HaulUrgent` + `WorkGiver_HaulUrgently` + `HaulUrgentlyCache : MapComponent`（60 tick 缓存 + dirty 标记 + 与 `listerHaulables` 求交集）；patch 搬运完成自动清标记；运行时可选兼容 Pick Up And Haul。
- **Allow / Forbid / AllowAll**：`Designator_ForbidStateBase` + `AATThingFilters`（`IsForbiddable` 判 `CompForbiddable`）。
- **Select Similar**：patch `Thing.GetGizmos` 按 `def + Stuff` 判断同质，`Designator_SelectSimilar` 框选加入选择器。
- **Harvest Fully Grown**：只标 `HarvestableNow && LifeStage==Mature` 的植物。
- **性能架构**（`docs/architecture.md`）：RimWorld/Unity 对象视为主线程对象；后台只做主线程快照纯计算，storage/path/reservation 留主线程；当前实现主线程缓存 + 60tick 分片（预算 250 Thing / 1–2ms）。
- 设计器经 `ReverseDesignatorDatabase.InitDesignators` 反射注入。

## 目录结构
```
收集/54-standalone-AnotherAllowTool/
├── About/About.xml
├── Common/Defs/AllowFunctionDefs/   # 4 个 XML
├── Common/Patches/                  # 给 Mech_Lifter 加 HaulingUrgent
├── docs/architecture.md             # 架构笔记
├── Tests/whitebox/                  # test_aat_static.py + run_whitebox.sh
└── 1.6/Source/                      # 6 个 cs
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

## 相关文件
- `docs/architecture.md` — 高质量架构笔记（线程边界/缓存/分片）
- `Tests/whitebox/test_aat_static.py` — 白盒测试
- 测试知识：`../../docs/knowledge/testing-and-validation.md`

