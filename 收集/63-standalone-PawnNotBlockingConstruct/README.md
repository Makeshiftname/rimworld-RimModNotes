# Pawn Not Blocking Construction 小人不会阻碍建造 Notes

## 一句话定位
小人踩上施工中建筑不再打断建造（`GenConstruct.BlocksConstruction` 对 Pawn 恒返回 false）。

## 关键要点
- `[HarmonyPatch(GenConstruct, "BlocksConstruction")]` Postfix：`t is Pawn` 时 `__result = false`。
- `Start`（StaticConstructorOnStartup）+ `harmony.PatchAll()`。
- **注意**：namespace 是 `Template`、Harmony id 是 `com.RunningBugs.Test`（模板残留未清理，建议改名）。

## 目录结构
```
收集/63-standalone-PawnNotBlockingConstruct/
├── About/About.xml
├── Common/
└── 1.6/Source/Main.cs
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

