# Facial Animation Statue Snapshot Fix Notes

## 一句话定位
修复 Facial Animation 雕像快照因重复 `HeadControllerComp` 向 `Dictionary.Add` 写同一 key 抛异常导致 trader 库存/领袖雕像生成崩溃。

## 关键要点
- **手动（非 PatchAll）patch**：`AccessTools.TypeByName("FacialAnimation.HarmonyPatches")` + 定位 `PrefixCreateSnapshotOfPawn_HookForMods`（参数 `[Pawn, Dictionary<string,object>&]`），找不到则报错返回。
- **Transpiler**：把目标方法内**唯一**的 `Dictionary<string,object>.Add` 调用替换为 `SafeAddIgnoreDuplicate`（保留 labels/blocks）；替换数 ≠1 时 `Log.Error` 告警（自检）。
- `SafeAddIgnoreDuplicate`：key 已存在时 `Log.WarningOnce` 保留第一个值，不删除 pawn comp。
- 依赖 `Nals.FacialAnimation` + Harmony（loadAfter）。
- 针对第三方 mod 私有方法的 transpiler 补丁，易随 Facial Animation 版本失效（代码已做计数校验自检）。

## 目录结构
```
收集/72-patch-FacialAnimationStatueSnapshotFix/
├── About/About.xml
└── 1.6/Source/Main.cs
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```
