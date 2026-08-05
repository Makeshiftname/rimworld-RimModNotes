# 无尽黑暗扩展 —— 饥荒 Permanent Darkness Extended -- DontStarve Notes

## 一句话定位
无尽黑暗的「饥荒」扩展——添加可种植的萤火虫（发光植物）与可携带/放置的燃油提灯，让永夜下能靠自身光源生存。

## 关键要点
- **萤火虫**：`GenStep_FireFlies`（地图生成撒萤火虫）+ `CompPlantableFireFly`（种植 Gizmo + 瞄准预览 + 邻近红圈冲突提示）+ `CompGlowerFireFly`（闪烁发光）+ `ScattererValidator_AvoidSpecialThings`（避开喷泉/远古舱/灵树/极光核心等）。
- **提灯**：`LanternWeapon : MinifiedThing`（可装备近战提灯，消耗 1 萤火虫种子）+ `CompHasLightBulb`（在持有者位置生成光点）+ `CompLightBulb`（寿命到期自毁）+ `CompLanternFeulTracker`（油量条 Gizmo）。
- **注入地图生成**：`Common/Patches/Patches.xml` 用 `PatchOperationAdd` 给 `MapGeneratorDef["Base_Player"]/genSteps` 加 `PDE_FireFlies`。
- `[DebugAction]` 一键变暗地图调试。
- 依赖 `runningbugs.permanentdarkness`（无尽黑暗，29）。

## 重要状态
**整个 mod 内容当前在 `backup/` 子目录**（1.5/1.6 源码 + Assemblies + Common + Textures），
仓库根目录为空壳（仅 `.roo/`、`.gitignore`、`backup/`）。`backup/About/About.xml` 的
description 仍是占位 `Template`——属于「移动端未定稿」状态，重构完成前从 `backup/` 读取。

## 目录结构
```
32-PermanentDarknessExtendedDontStarve/
├── .roo/                   # agent 规则
└── backup/                 # 全部内容在此（1.5|1.6/Source、Common/、About/、Textures/）
```

## 相关文件
- `backup/1.6/Source/Fireflies.cs`、`Latern.cs`、`DebugOptions.cs`
- `backup/Common/Defs/`（FireFlies.xml、Lantern.xml）
- `backup/Common/Patches/Patches.xml`
