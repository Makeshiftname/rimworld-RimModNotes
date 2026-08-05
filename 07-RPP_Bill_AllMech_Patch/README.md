# RPP_Bill_AllMech_Patch Notes

## 一句话定位
修复 Misc. Robots++（RPP）机器人在工作清单「所有机械体」设置下不生效的问题。

## 关键要点
- **补丁目标**：Harmony patch 原版 `Bill.PawnAllowedToStartAnew`，让它正确识别 RPP 机器人。
- **机械体判定**：`p.IsColonyMech` + `p.RaceProps.mechFixedSkillLevel`（RPP 机器人用固定技能等级，不走 `p.skills`）。
- **技能范围检查**：按 `mechFixedSkillLevel` 与 `b.allowedSkillRange` 比较，超出范围给 `JobFailReason` 拒绝开工。
- **启动装配**：`[StaticConstructorOnStartup]` + `new Harmony("com.RunningBugs.RPP_Bill_AllMech_Patch")` + `PatchAll`。
- 依赖：Misc. Robots++（`alaestor.miscrobots.plusplus`）+ Harmony。

## 目录结构
```
07-RPP_Bill_AllMech_Patch/
├── About/About.xml
└── Source/                # Main.cs（Bill.PawnAllowedToStartAnew 补丁）
```

## 构建
```
cd Source && dotnet build -c Release
```

## 相关文件
- `Source/Main.cs` — 补丁实现
- Harmony 补丁模式：`../../docs/knowledge/harmony-patching.md`

