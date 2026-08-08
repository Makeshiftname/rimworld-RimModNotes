# QuestItemWatch Notes

## 一句话定位
监控任务物品：通过 GameComponent 每 tick 检查任务系统，对关键任务物品做提醒（字母类型 LetterDef）。

## 关键要点
- **GameComponent**：`QuestItemWatcher : GameComponent`，构造 `(Game game)` 由游戏自动创建。
- **每 tick 回调**：覆写 `GameComponentTick()`，访问 `Find.QuestManager` 检查任务状态。
- **DefOf 静态引用**：`[DefOf]` + `public static LetterDef success_letter;` 在启动时绑定 Def。
- **日志封装**：`Logger.cs` 提供 `Log.Message/Warning/Error`（自动带调用位置）。
- 此 mod 不依赖 Harmony（源码中 Harmony 引用已注释掉）。

## 目录结构
```
收集/10-standalone-QuestItemWatch/
├── About/About.xml
└── Source/                # Main.cs（QuestItemWatcher）、Logger.cs
```

## 构建
```
cd Source && dotnet build -c Release
```

## 相关文件
- `Source/Main.cs` — GameComponent 实现 + DefOf
- `Source/Logger.cs` — 日志模板
- 组件知识：`../../docs/knowledge/game-and-world-components.md`、`../../docs/knowledge/project-templates.md`

