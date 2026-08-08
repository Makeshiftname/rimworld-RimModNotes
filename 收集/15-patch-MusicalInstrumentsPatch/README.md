# Musical Instruments (Continued) Patch Notes

## 一句话定位
让乐器演奏状态与自定义 Comp 同步：patch Musical Instruments 的播放/停止，联动 `Comp_PlayingMusic`。

## 关键要点
- **Postfix StartPlaying**：`MusicalInstruments.PerformanceManager.StartPlaying` 后，让乐器上的 `Comp_PlayingMusic` 开始播放。
- **Prefix StopPlaying**：`PerformanceManager.StopPlaying` 前，从 `Comp_PlayingMusic.notebook` 取出对应 comp 停止播放。
- **自定义 ThingComp**：`Comp_PlayingMusic`（含 `CompProperties`：`CompProp_PlayingMusic`）挂在乐器 Thing 上。
- 依赖：Musical Instruments (Continued)（`mlie.musicalinstruments`）+ Harmony。
- 参考：`../../docs/knowledge/harmony-patching.md`

## 目录结构
```
15-MusicalInstrumentsPatch/
├── About/About.xml
└── Source/                # Main.cs、Comp_PlayingMusic.cs、CompProp_PlayingMusic.cs + MusicalInstruments.dll
```

## 构建
```
cd Source && dotnet build -c Release
```

