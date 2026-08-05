# Misc. Robots++ Clean Before Work Patch Notes

## 一句话定位
让 Common Sense 的「做饭前清理」等清洁判定对 Misc. Robots++ 机器人（ToolUser 智能、无精神状态）也生效。

## 关键要点
- **补丁目标**：Harmony patch `CommonSense.Utility.IncapableOfCleaning`，postfix 放宽判定。
- **放宽点**：智能等级从 `Humanlike` 降为 `ToolUser`、移除精神状态（mental state）检查，使 RPP 机器人可被派去清洁。
- **引用第三方程序集**：`Source/` 直接 `using CommonSense;` 并放置 `CommonSense.dll` 供编译。
- **装配**：`[StaticConstructorOnStartup]` + `new Harmony("com.RunningBugs.RppPatch.CommonSense").PatchAll()`。
- 依赖：Common Sense（`avilmask.commonsense`）+ Misc. Robots++ + Harmony。

## 目录结构
```
08-RppCommonSenseCleanBeforeCookingPatch/
├── About/About.xml
└── Source/                # Main.cs + CommonSense.dll（第三方引用）
```

## 构建
```
cd Source && dotnet build -c Release
```

## 相关文件
- `Source/Main.cs` — 补丁实现
- Harmony 补丁模式：`../../docs/knowledge/harmony-patching.md`

