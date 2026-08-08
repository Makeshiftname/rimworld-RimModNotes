# [SYR] Doormats 简体中文翻译补充 Notes

## 一句话定位
[SYR] Doormats 内置简中翻译不全，本 mod 是自用汉化补充：把原 mod 的英语翻译文件翻译成简中。

## 关键要点
- 翻译 mod：只有 `Languages/`，无 C# 代码。
- 依赖 `syrchalis.doormats`（[SYR] Doormats）。
- 覆盖方式：补全原 mod 缺失的中文翻译项（Keyed / DefInjected）。
- 参考：`../../docs/knowledge/translations-localization.md`

## 目录结构
```
收集/13-translation-SYR_Doormats_zh/
├── About/About.xml                    # packageId RunningBugs.SYR.Doormats.zh
└── Languages/                         # 简中翻译补充
```

## 相关文件
- `About/About.xml` — 依赖声明 `syrchalis.doormats`

