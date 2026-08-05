# 翻译 / Languages（本地化）

> 来源 mod：04、13、18、25（另有 06 空目录待补）
> 适用：为第三方 mod 补充中文翻译，或自建 mod 的多语言支持。

## 1. Languages 目录结构

RimWorld 翻译放在 mod 的 `Languages/<语言>/` 下，常用语言目录：
`ChineseSimplified`（简体）、`ChineseTraditional`（繁体）、`English` 等。

典型内容：
- `Keyed/*.xml` — 按键名翻译（`<KeyName>` 对应代码里 `.Translate()` 的键）。
- `DefInjected/*.xml` — 注入到 Def 的字段（`<DefName.FieldName>`）。
- `Strings/*.xml` — 其他字符串资源。

## 2. 仓库现状与注意点

- 翻译 mod 通常**仅依赖被翻译的 mod**：`04-WeatherControlDeviceCont_zh` 在
  `About.xml` 的 `<modDependencies>` 声明了 `mlie.weathercontroldevice`
  （Weather Control Device (Continued)）。详见
  [`04-WeatherControlDeviceCont_zh/About/About.xml`](../../04-WeatherControlDeviceCont_zh/About/About.xml)。
- **目录位置不统一**：有的在顶层 `Languages/`（04、13），有的在 `Common/Languages/`（25）。
  `scan_mods.py` 已同时探测这两种位置。
- **命名不统一**：仓库中 `ChineseSimplified`、`ChineseSimplified (简体中文)`、
  `ChineseTraditional (繁體中文)` 三种写法并存（历史遗留，见 CONTRIBUTING 备注）。
- **教训**：`18-HakuroXenohumanZh` 的 `Source/Main.cs` 残留了模板 `namespace Template`
  代码——翻译 mod 通常不需要 C# 源码，复制模板时记得清理。

## 3. 翻译 mod 清单

| Mod | 翻译对象 | 目录位置 |
|---|---|---|
| 04-WeatherControlDeviceCont_zh | Weather Control Device (Continued) | 顶层 `Languages/` |
| 13-SYR_Doormats_zh | [SYR] Doormats | 顶层 `Languages/` |
| 18-HakuroXenohumanZh | Hakuro Xenohuman | 顶层 `Languages/` |
| 25-AMonkeyExpansion_zh | A Monkey Expansion | `Common/Languages/` |

## 4. 相关文件

- 依赖声明：`04-WeatherControlDeviceCont_zh/About/About.xml`
- 跨版本共享翻译：`25-AMonkeyExpansion_zh/Common/Languages/`

## 相关主题

- 跨版本目录：`cross-version-structure.md`
