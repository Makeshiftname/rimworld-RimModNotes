# Ghoul Auto Serum Commands 食尸鬼自动服用血清命令按钮 Notes

## 一句话定位
给食尸鬼添加「自动服用强力血清/钢血血清」的命令切换按钮与对应 Job。

## 关键要点
- `CompGhoulCommands : ThingComp`，`CompGetGizmosExtra` 生成 `Command_Toggle` 开关（仅 `IsColonySubhumanPlayerControlled && IsGhoul`），状态 `PostExposeData` 随存档。
- `GhoulCommandsSettings`：`enableJuggernautSerumToggle`（默认 true）/ `enableMetalbloodSerumToggle`（默认 false）。
- `JobGiver_TakeJuggernautSerum/TakeMetalbloodSerum : ThinkNode_JobGiver`（`ClosestThingReachable` 找血清、无对应 Hediff 才工作）+ `JobDriver_Take*Serum`（`Toils_Goto`→`Toils_Ingest.ChewIngestible`→`FinalizeIngest`）。
- JobDefs 在 `Common/Defs/JobDefs/Jobs_Ghoul.xml`；血清/hediff 用 `GetNamed("JuggernautSerum"/"Metalblood", true)` 获取。
- 依赖 Anomaly DLC（食尸鬼/血清；About 未声明）。

## 目录结构
```
59-GhoulCommands/
├── About/About.xml
├── Common/Defs/JobDefs/     # Jobs_Ghoul.xml
├── Common/Languages/
└── 1.6/Source/Main.cs
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

