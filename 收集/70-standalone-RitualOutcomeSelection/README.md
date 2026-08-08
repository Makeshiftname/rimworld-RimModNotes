# Ritual Outcome Selection 选择仪式结果 Notes

## 一句话定位
仪式开始前弹窗让你直接选择想要的仪式结果，省去反复 S/L。

## 关键要点
- `[HarmonyPatch(RitualOutcomeEffectWorker_FromQuality, "GetOutcome")]` Postfix：静态 `result != null` 时覆盖 `__result`，一次性消费后清空。
- `[HarmonyPatch(LordJob_Ritual, "ApplyOutcome")]` Prefix：仪式未完成时清空 result（防串结果）。
- `[HarmonyPatch(Window, "Close")]` Postfix：`Dialog_BeginRitual` 关闭时弹出 `Dialog_RitualOutcomeSelection`。
- `Dialog_RitualOutcomeSelection : Window`：按 `outcomeDef.outcomeChances` 的 `positivityIndex` 单选，选中写入静态 result 再 Close。
- 用静态字段跨 patch 传状态（游戏单线程下可行）。
- 依赖 Harmony；需含仪式机制的 DLC（Ideology+）。

## 目录结构
```
收集/70-standalone-RitualOutcomeSelection/
├── About/About.xml
├── Common/
└── 1.6/Source/Main.cs
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

