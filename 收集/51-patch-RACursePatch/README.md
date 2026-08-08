# 鼠异诅咒补丁 RACursePatch Notes

## 一句话定位
对 Ratkin Anomaly+ 原模组的诅咒机制再打补丁：可配置使诅咒只对殖民者生效。

## 关键要点
- **「补丁的补丁」**：`TargetMethod()` 用 `AccessTools` 定位原模组补丁类的方法（`AteWithoutTable_Patch.Postfix`、`KillAnimals_Patch.Prefix`、`Wounded_Patch.Prefix`），再挂 Prefix 拦截。
- **拦截逻辑**：`settings.enablePatch && !pawn.IsColonist` 时 `return false` 跳过原补丁。
- `Settings : ModSettings`（`enablePatch` 勾选）；只覆盖吃没桌子/杀动物/受伤 3 个诅咒。
- 依赖 `fxz.ratkinanomaly.update`（Ratkin Anomaly+）+ Harmony。
- 注意：`RASL.dll` 已提交但未被 csproj 引用（残留）。
- 与 50 的关系：50 是独立重实现（不需要 Ratkin 种族），51 是给 Ratkin Anomaly 原诅咒打拦截补丁。

## 目录结构
```
51-RACursePatch/
├── About/About.xml
└── 1.5/Source/               # Main.cs + RatkinAnomaly.dll（+RASL.dll 残留）
```

## 构建
```
cd 1.5/Source && dotnet build -c Release
```

