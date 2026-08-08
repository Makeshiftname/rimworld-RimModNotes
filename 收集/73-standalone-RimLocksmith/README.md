# RimLocksmith 边缘锁匠 Notes

## 一句话定位
参考 Locks2 的门禁控制 mod：门权限分类、默认配置、多门批量编辑、内置汉化、白盒测试核心策略。

## 关键要点
- **纯逻辑 `Core/`**：`AccessCategory` 枚举（10 类，前 6 可配置）、`LockPolicy`（`IsConfigurable` + `Allows`；`MaxPetBodySize=0.86`、AnimalAccess/MechAccess 三态 `OnlyPets`/`OnlyOverseen`）、`LockConfigData`（v2 schema，`UserConfigured`、`LinkedPresetId` 预留）、`PawnAccessFactsFactory`（按派系敌对性分流，商队/访客动物归 `Guest`，不误当野生动物）。
- `CompRimLocksmithDoor : ThingComp`：懒加载默认配置（`EnsureConfig`）、`NotifyChanged` 清 `map.reachability` 缓存、`LockConfigScribe` 存读。
- **patch 语义**：`Patch_BuildingDoor_PawnCanOpen` Postfix **只收窄**（原版 true 时按 `ShouldDeny` 改 false；`CanOpenAnyDoor` 放行）；`KnownCompatDoor_PawnCanOpen` 兼容 Doors Expanded 的 `Building_DoorExpanded`。
- **多选编辑**：`ITab_RimLocksmith` + `Dialog_EditLockConfig` + `InspectPanePatches`（多门批量、非殖民地门忽略、mixed 状态）。
- **XML Patches**：`1.6/Patches/Core/`（Building_Door、Building_MultiTileDoor）+ `Mods/`（DoorsExpanded、LinkableDoors、ArchitectExpandedFences、SaveOurShip）显式挂 comp/ITab（不自动猜测）。
- **测试**：`tests/RimLocksmith.Tests`（C# 白盒单测覆盖 LockPolicy/分类/三态）+ `source_invariant_tests.py` + `run_tests.sh`。
- **设计文档**：`DESIGN.md` 详述关键语义——postfix 只收窄、非殖民地门完全旁路、`FenceBlocked` 畜栏动物保留原版限制（避免 Locks2 式动物越狱 bug）。
- 依赖 Harmony；可选兼容 4 个第三方门 mod。

## 目录结构
```
收集/73-standalone-RimLocksmith/
├── About/About.xml
├── DESIGN.md                 # 设计文档
├── docs/                     # vanilla-behavior-audit.md、rebuild-design.md
├── 1.6/Source/               # Core/ 5 文件 + Comp/ITab/Dialog/Patches 等
├── 1.6/Patches/              # Core 2 + Mods 4 xml
├── tests/                    # RimLocksmith.Tests + source_invariant_tests.py
└── Common/Languages/
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

## 相关文件
- `DESIGN.md` — 核心语义设计
- `tests/RimLocksmith.Tests/` — C# 白盒单测
- 测试知识：`../../docs/knowledge/testing-and-validation.md`
