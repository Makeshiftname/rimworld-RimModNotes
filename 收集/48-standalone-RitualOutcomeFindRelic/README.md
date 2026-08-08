# 仪式可以发现圣物线索 Ritual add "Find Relic Information" outcome Notes

## 一句话定位
给仪式添加「发现圣物线索」附挂结果：仪式成功后按可配置概率触发生成圣物猎寻任务/子任务。

## 关键要点
- `RitualAttachableOutcomeEffectWorker_FindRelic : RitualAttachableOutcomeEffectWorker`，重写 `Apply()`，按 `Settings.FindRelicChance` 概率触发 `GenerateAndSendQuest()`。
- **Def**：`Common/Defs/PerceptDefs/RitualOutcomeDefs.xml` 的 `RitualAttachableOutcomeEffectDef FindRelic`。
- **任务生成**：检查主意识形态 `Precept_Relic.CanGenerateRelic`，`QuestUtility.GenerateQuestAndMakeAvailable` 生成 `RelicHunt` 任务，再调 `QuestPart_SubquestGenerator_RelicHunt.TryGenerateSubquest()` 生成子任务。
- `FR_GameComp : GameComponent` 在 `FinalizeInit` 刷新效果描述（把 "xx%" 文本重写注入概率）。
- `Settings : ModSettings`（FindRelicChance 滑块 0~1）+ `[DebugAction]`。
- 依赖 **Ideology DLC**（代码 `ModLister.CheckIdeology`；About 未声明运行时依赖）。

## 目录结构
```
收集/48-standalone-RitualOutcomeFindRelic/
├── About/About.xml
├── Common/Defs/PerceptDefs/
├── Common/Languages/
└── 1.5|1.6/Source/           # Main.cs
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

