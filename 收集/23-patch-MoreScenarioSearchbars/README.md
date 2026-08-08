# More Scenario Searchbars Notes

## 一句话定位
为原版「遭遇战/创建事件」（Create Incident）场景部件加上搜索栏，复用 Scenario Searchbars 的代码。

## 关键要点
- **动态定位目标**：`ScenPart_CreateIncident_Patch` 用 `AccessTools.TypeByName("ScenPart_CreateIncident")` 定位目标，Postfix `DoEditInterface` 调 `Tools.DrawSearchbar(...)`。
- **复用第三方代码**：直接内嵌 `ScenarioSearchbar.dll`，调用其 `Tools` 静态方法。
- 用 `TargetMethod()` 方式做 Harmony patch（非静态特性标注）。
- 依赖：Scenario Searchbars（`nimrag.scenariosearchbar`）。

## 目录结构
```
收集/23-patch-MoreScenarioSearchbars/
├── About/About.xml
└── Source/                # Main.cs + ScenarioSearchbar.dll
```

## 构建
```
cd Source && dotnet build -c Release
```

