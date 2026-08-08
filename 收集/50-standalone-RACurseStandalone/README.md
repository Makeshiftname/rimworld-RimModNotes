# 鼠异诅咒独立模组 RACurseStandalone Notes

## 一句话定位
从 Ratkin 模组抽出「鼠异诅咒」机制并独立重实现（不依赖 Ratkin 种族），5 种诅咒随机降临殖民地。

## 关键要点
- **自实现诅咒系统**：`IncidentWorker_Curse` 随机选 5 诅咒之一写入 `RACurseSAComponent : WorldComponent.Curse`；`GameCondition_Curse.End()` 重置并发结束信。
- **5 个诅咒补丁**（`CursePatches.cs`）：
  - EatingWithoutTable：吃没桌子→FoodPoisoning 升 3 阶段
  - Research：研究中自由殖民者 -10 智力经验
  - KillAnimals：杀动物→生成奇美拉（Chimera）空降
  - Wounded：受伤→音效 + 血污 + 击倒
  - CutTree：砍树→强制寒潮
- **Defs**：`RACurseSA_CurseCondition`（GrayPall 天气）+ `IncidentDef RA_Curse_SA`（baseChance 0.5、minRefireDays 30、durationDays 1~3）。
- 注意：About 声称「可配置只对殖民者生效」但 `OnlyApplyToColonists` 设置未接入 UI（部分补丁写死 `pawn.IsColonist`）。
- 与 51 的关系：50 是独立重实现（不需要 Ratkin 种族），51 是给 Ratkin Anomaly 原诅咒打拦截补丁。

## 目录结构
```
收集/50-standalone-RACurseStandalone/
├── About/About.xml
├── Common/Defs/              # GameConditionDef + IncidentDef + LetterDef
├── Common/Languages/
└── 1.5/Source/               # Main.cs + CursePatches.cs
```

## 构建
```
cd 1.5/Source && dotnet build -c Release
```

