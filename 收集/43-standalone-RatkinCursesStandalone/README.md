# 鼠鼠异常诅咒特辑 Ratkin Curses Standalone Notes

## 一句话定位
把「鼠鼠异常+」的诅咒做成可独立设置的永久游戏状态（不再随机）。

## 关键要点
- **持久化诅咒**：`RAComponent : WorldComponent` 用 `string curse`（存档 key "curse"，默认 "Null"）记录当前诅咒。
- **条件驱动**：`GameCondition_Curse : GameCondition_ForceWeather`，`Init` 里设置诅咒 + 用 `DefModExtension`（`GameConditionModExtension`）/`LetterMaker` 发诅咒信；`End` 时置回 "Null" + 发结束信。
- **受伤诅咒**：`Wounded_Patch` Prefix 拦截 `Pawn.PostApplyDamage`，当 `RAUtility.IfCurseActive("Wounded")` 且殖民者/奴隶受伤时：播音效 + 撒 16 格血渍 + `HealthUtility.DamageUntilDowned`。
- `Common/Defs/GameConditionDefs/GameConditions.xml`：抽象 `RA_CurseCondition` + 具体 `RA_CurseCondition_Wounded`（`canBePermanent=true`）。
- 目前只有 "Wounded" 一种，架构上可扩展；与 50/51（RACurse 系列）同体系。

## 目录结构
```
43-RatkinCursesStandalone/
├── About/About.xml
├── Common/Defs/GameConditionDefs/
├── Common/Languages/
└── 1.5|1.6/Source/         # Main.cs + RASL/GameCondition_Curse.cs
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

