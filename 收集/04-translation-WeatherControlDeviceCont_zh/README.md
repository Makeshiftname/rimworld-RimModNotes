# WeatherControlDeviceCont_zh — 天气控制设备简中翻译

## 一句话定位
[Weather Control Device (Continued)](https://steamcommunity.com/sharedfiles/filedetails/?id=2584797720) 的简体中文翻译，纯 `Languages/`，无 C# 代码。

## 关键要点
- 翻译 mod 通常不需要 `Source/`，只需 `Languages/<语言>/` 下的 Keyed / DefInjected 翻译文件。
- 在 `About.xml` 的 `<modDependencies>` 声明被翻译的 mod（`mlie.weathercontroldevice`），保证它先加载。
- 语言目录命名：仓库内 `ChineseSimplified` 等写法并存（历史遗留），新建建议统一用 `ChineseSimplified`。

## 目录结构
```
04-WeatherControlDeviceCont_zh/
├── About/About.xml                    # packageId RunningBugs.WeatherControlDeviceContZh
└── Languages/ChineseSimplified/       # 简中翻译（Keyed / DefInjected）
```

## 部署
- 需先安装 Weather Control Device (Continued) 并排在它之后加载。

## 相关文件
- `About/About.xml` — 依赖声明 `mlie.weathercontroldevice`
- 翻译主题知识：`../../docs/knowledge/translations-localization.md`

