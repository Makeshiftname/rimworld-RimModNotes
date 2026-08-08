# HakuroXenohuman_zh — 简中翻译 Notes

## 一句话定位
[Hakuro Xenohuman](https://steamcommunity.com/sharedfiles/filedetails/?id=2911715839) 的简体中文翻译。

## 关键要点
- 翻译 mod：以 `Languages/` 为主，无实际功能代码。
- 依赖 `hakuro.xenohuman.septentrionrace`，写入 `<loadAfter>` 保证加载顺序。
- **已知问题**：`Source/Main.cs` 残留模板 `namespace Template` 代码（翻译 mod 本不需要 C#），后续应清理。
- 参考：`../../docs/knowledge/translations-localization.md`

## 目录结构
```
收集/18-translation-HakuroXenohumanZh/
├── About/About.xml
├── Languages/             # 简中翻译
└── Source/                # Main.cs（残留模板，待清理）
```

## 相关文件
- `About/About.xml` — 依赖与 loadAfter
- `Source/Main.cs` — 残留模板代码（清理项，见 CONTRIBUTING）

