# Letter Stack Cleaner 2 Notes

## 一句话定位
按 Delete 键一键清空当前全部信件（原版 Letter Stack Cleaner 的重实现，补上世界视图支持）。

## 关键要点
- **按键监听**：`LetterStackCleaner : GameComponent` 在 `GameComponentOnGUI` 检测 `KeyUp + KeyCode.Delete`，遍历 `Find.LetterStack.LettersListForReading` 逐一 `RemoveLetter`，播 `SoundDefOf.Click`。
- 无 Harmony 业务 patch，纯 GameComponent OnGUI 监听。
- 注意：启动日志仍是模板默认文案；根目录与 1.4/1.5 有多份 Source 副本（同步问题）。

## 目录结构
```
27-LetterStackCleaner2/
├── About/About.xml
├── Source/                 # 根目录副本
└── 1.4|1.5/Source/         # Main.cs
```

## 构建
```
cd 1.5/Source && dotnet build -c Release
```

