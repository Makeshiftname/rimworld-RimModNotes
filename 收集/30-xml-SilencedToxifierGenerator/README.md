# 静音排污发电机 Silenced Toxifier Generator Notes

## 一句话定位
把排污发电机（Toxifier Generator）的工作音量改为 0。

## 关键要点
- **纯 XML**：`Common/Patches/Patches.xml` 用 `PatchOperationReplace`（`MayRequire="Ludeon.Rimworld.Biotech"`）把 `SoundDef["Toxifier_Working"]/subSounds/li/volumeRange` 替换为 0。
- 无 C#、无版本目录，靠 `Common/` 跨版本。
- 参考：`../../docs/knowledge/xml-defs-and-patches.md`

## 目录结构
```
30-SilencedToxifierGenerator/
├── About/About.xml
└── Common/Patches/         # Patches.xml
```

## 相关文件
- `Common/Patches/Patches.xml` — 静音补丁

