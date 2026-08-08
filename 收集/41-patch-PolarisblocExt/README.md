# 天轴自用扩展 Polarisbloc Extension Notes

## 一句话定位
天轴自用扩展——研究纳米修复装置后可像人格核心任务那样用无线电请求科技核心（间隔一年）。

## 关键要点
- **提醒**：`RequestTechCoreNotification : GameComponent` 每 `TicksGame % 1999`（素数）且可请求时发信提醒「可请求科技核心」。
- **无线电对话**：`TechCoreGiver : IExposable, ILoadReferenceable, ICommunicable`，`TryOpenComms` → `Dialog_Negotiation`；`RequestTechCoreQuest` 用 `QuestUtility.GenerateQuestAndMakeAvailable` 生成藏宝地任务，扣 1500 银，冷却 `GenDate.TicksPerYear`。
- **通讯台菜单**：`Patch_Building_CommsConsole_GetFloatMenuOptions` Postfix 给通讯台右键菜单加选项。
- 依赖：Polarisbloc CoreLab（`Vanya.Polarisbloc.CoreLab.tmp`）+ Harmony。

## 目录结构
```
收集/41-patch-PolarisblocExt/
├── About/About.xml
├── Common/Languages/
└── 1.5|1.6/Source/         # RequestTechCoreInformation.cs
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

