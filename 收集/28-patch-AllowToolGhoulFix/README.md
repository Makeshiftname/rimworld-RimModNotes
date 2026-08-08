# AllowToolGhoulFix Notes

## 一句话定位
修复 Allow Tool 导致食尸鬼近战「补刀」被禁用的 bug。

## 关键要点
- **真正的修复是 XML**：`Common/Patches/Patches.xml` 用 `PatchOperationReplace`（`MayRequire="unlimitedhugs.allowtool"`）把 `WorkTypeDef[defName="FinishingOff"]/relevantSkills` 替换为空，移除技能限制。
- `Source/Main.cs` 是纯模板（无实际 C# 逻辑）——「假 C# 壳 + 真 XML patch」的示例。
- 依赖：Allow Tool（`unlimitedhugs.allowtool`）。

## 目录结构
```
收集/28-patch-AllowToolGhoulFix/
├── About/About.xml
├── Common/Patches/         # 真正的补丁
└── 1.4|1.5/Source/         # 模板占位
```

## 相关文件
- `Common/Patches/Patches.xml` — 实际修复
- XML patch 知识：`../../docs/knowledge/xml-defs-and-patches.md`

