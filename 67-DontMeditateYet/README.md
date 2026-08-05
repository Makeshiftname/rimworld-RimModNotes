# Don't Meditate Yet 先别冥想 Notes

## 一句话定位
给有 psylink 的小人加「别冥想」开关 gizmo，开关状态按地图记忆，避免战斗后小人停下冥想。

## 关键要点
- `DontMeditateYetMapComponent : MapComponent`：`Dictionary<int,bool>`（thingIDNumber→state）随存档，默认值读设置。
- `MeditationToggleComp : ThingComp`：`CompGetGizmosExtra` 仅对 psylink+玩家+单选+`NeedToShowGizmo()` 时 yield `Command_ToggleMeditation`（图标 `MeditationAllowed/Blocked`，Order=-99 贴近 psyfocus gizmo）。
- `JobGiver_MeditateConditional : ThinkNode_JobGiver`：状态关闭时返回 0/null（复刻原版优先级 9 / 7.1 / 6）。
- `DontMeditateYetSettings.defaultToggleState`（默认 false=允许冥想）。
- 依赖 Royalty DLC（psylink/psyfocus）。

## ⚠️ 当前状态（如实标注）
**功能未接线 / 仅实现组件未生效**：`IMPLEMENTATION_NOTES.md` 声称会 patch `Pawn.GetGizmos`，但实际**没有 HarmonyPatches.cs**，`MeditationToggleComp` 也未挂到任何 pawn def；纹理是 `.txt` 占位（图标会是 BadTex）；`Main.cs` 只打日志不 PatchAll。

## 目录结构
```
67-DontMeditateYet/
├── About/About.xml
├── README_MOD.md            # mod 说明
├── IMPLEMENTATION_NOTES.md  # 实现笔记（含未完成标注）
├── 1.6/Source/              # 9 个 cs
└── 1.6/Textures/UI/Commands/  # .txt 占位
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

## 相关文件
- `IMPLEMENTATION_NOTES.md` — 实现与未接线说明

