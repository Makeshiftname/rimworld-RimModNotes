# Limited Letter Slots 有限信件槽 Notes

## 一句话定位
把信件槽数量限制为可配置的固定值，多余信件打包成一个组按钮，防止把高优先级信息挤出屏幕。

## 关键要点
- **Prefix 完全重实现** `LetterStack.LettersOnGUI`（`return false`），通过 tracer 字段（`___letters`、`___tmpBundledLetters`、`___bundleLetterCache`、`___lastTopYInt`）访问原版私有状态。
- **槽位逻辑**：`letters.Count > maxVisibleSlots` 时 `individual = maxVisibleSlots - 1`，其余进 `BundleLetter`（`LetterDefOf.BundleLetter`）组按钮；含 Repaint 阶段的 mouse-over/tooltip 通道。
- `Settings`：`enableMod` + `maxVisibleSlots`（1~10，默认 3）。
- **附赠**：`LetterCleaner : GameComponent` 按 Delete 键清空所有信件。
- 注意：About 明说 1.5 支持未测试；Prefix 重实现依赖原版私有字段名（较脆弱）。

## 目录结构
```
55-LimitedLetterSlots/
├── About/About.xml
├── Common/Languages/          # 中英
└── 1.5|1.6/Source/            # Main.cs
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

