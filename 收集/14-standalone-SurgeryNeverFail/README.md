# Surgery Never Fail Notes

## 一句话定位
让手术永远成功，免去 S/L 大法。patch 原版手术成功率判定。

## 关键要点
- Harmony patch 手术成功率相关方法，强制手术成功。
- 依赖 Harmony（`brrainz.harmony`）。
- 支持 1.4 / 1.5 / 1.6。
- 参考：`../../docs/knowledge/harmony-patching.md`

## 目录结构
```
14-SurgeryNeverFail/
├── About/About.xml
└── 1.4|1.5|1.6/Source/       # Main.cs + Logger.cs
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

