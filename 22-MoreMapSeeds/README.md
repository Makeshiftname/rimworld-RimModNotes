# More Map Seeds 更多地图随机种子 Notes

## 一句话定位
为地图生成新增大量随机种子词，并在开局时显示世界种子、可点击复制。

## 关键要点
- **替换种子生成**：Harmony Prefix 拦截 `GenText.RandomSeedString`，改用 `GrammarResolver.Resolve("r_seed", ...)` 从 `RulePackDefOf.MCS_SeedGenerator` 生成种子（Harmony id `com.runningbugs.moremapseeds`）。
- **规则包**：`RulePackDef`（`Common/Defs/RulePackDef.xml`）经 `rulesFiles` 引用 `NamesExtended/SeedGenerator`；种子词在 `Languages/ChineseSimplified/Strings/NamesExtended/SeedGenerator.txt`（中文词），English 版为空。
- **显示与复制**：`WorldSeedGameComponent : GameComponent` 在 `FinalizeInit` 发信显示 `Find.World.info.seedString`；`StandardLetterCopyOnClick : StandardLetter` 重写 `OpenLetter`，点击把种子写入系统剪贴板。
- 附 `RemoveDuplicatesTool/`（Python 去重脚本）清理种子词。

## 目录结构
```
22-MoreMapSeeds/
├── About/About.xml
├── Common/Defs/            # RulePackDef.xml、LetterDefs.xml
├── Common/Languages/       # 中英种子词
├── Common/Patches/
└── 1.4|1.5|1.6/Source/     # Main.cs、SeedLetter.cs、RemoveDuplicatesTool/
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

## 相关文件
- `1.6/Source/Main.cs` — 种子生成 patch
- `Common/Defs/RulePackDef.xml` — 种子规则包

