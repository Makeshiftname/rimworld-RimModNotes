# 别太吵的米莉拉 Not So Loud Milira Notes

## 一句话定位
把 Milira 种族模组的所有武器/音效音量减半（纯 XML 音量替换，无 C#）。

## 关键要点
- **纯 XML**：`Common/Patches/Patches.xml` 含 13 个 `PatchOperationReplace`，全部改 `Defs/SoundDef[defName=...]/subSounds/li/volumeRange`，基本对半削（如 `Milira_Shot_PlasmaRifle` 55~75→28~38、`Milira_ExcaliburWarmup` 700→350）。
- 覆盖武器射击/爆炸/建筑/技能/信标等声音。
- 依赖 `ancot.milirarace`（Milira Race）。
- 注意：packageId 拼写 `NoSoLoud`（应为 NotSoLoud）。

## 目录结构
```
收集/46-xml-NotSoLoudMilira/
├── About/About.xml
└── Common/Patches/         # Patches.xml（13 个音量替换）
```

## 相关文件
- `Common/Patches/Patches.xml` — 音量静音补丁
- XML patch 知识：`../../docs/knowledge/xml-defs-and-patches.md`

