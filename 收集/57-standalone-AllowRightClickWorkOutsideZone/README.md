# 强制命令无视限制 Priority Order Bypass Restrictions Notes

## 一句话定位
右键工作指派点时临时无视活动区与禁用工作类型限制（生成菜单后自动还原设置）。

## 关键要点
- `FloatMenuOptionProvider_BypassRestrictions : FloatMenuOptionProvider_WorkGivers`，重写 `GetOptions`：备份优先级与 `AreaRestrictionInPawnCurrentMap` → 把所有未禁用工作类型优先级设为 3 且区域限制置 null → 调 `base.GetOptions` → **立即还原**；选项标签追加 `[BypassRestrictions]`。
- 2025-07-06 更新：从只绕过区域扩展为也绕过禁用工作类型。
- 注意：About name 与目录名/packageId 不一致；**疑点**——`Main.cs` 无 `[StaticConstructorOnStartup]`/PatchAll/注册代码，该 provider 可能未被接线，功能运行时未必生效（需验证）。

## 目录结构
```
收集/57-standalone-AllowRightClickWorkOutsideZone/
├── About/About.xml
├── _PublisherPlus.xml
└── 1.6/Source/                # Main.cs
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

