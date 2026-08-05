# RimSaves + MoreAutosaveSlots 兼容补丁 Notes

## 一句话定位
让 RimSaves 与 MoreAutosaveSlots 两个存档 mod 共存：补丁统一处理「虚拟文件夹」前缀，使存档命名检查互通。

## 关键要点
- **兼容补丁**：同时引用 `aRandomKiwi.ARS`（RimSaves）与 `Revolus.MoreAutosaveSlots` 程序集，`Source/` 内置 `RimSaves.dll`、`Revolus.MoreAutosaveSlots.dll` 供编译。
- **虚拟文件夹前缀**：`PatchHelper.Prefix` 用 `Settings.curFolder` + 分隔符 `#§#` 构造存档前缀；`SavedGameNamedExists` 检查存档时带上前缀。
- **装配**：`[StaticConstructorOnStartup]` + `new Harmony("com.RunningBugs.RimSavesMoreAutosaveSlotsPatch").PatchAll()`。
- **依赖与排序**：`<loadAfter>` 声明 `revolus.moreautosaveslots`、`mlie.moreautosaveslots`、`arandomkiwi.rimsaves`（依赖 RimSaves）。

## 目录结构
```
12-RimSavesMoreAutoaveSlotsPatch/
├── About/About.xml
├── 1.4|1.6/Source/        # Main.cs、PatchHelper、Logger.cs + 两个第三方 dll
└── 1.4|1.6/Assemblies/    # 编译产物
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

## 相关文件
- `1.4/Source/Main.cs` — 兼容补丁与虚拟文件夹前缀
- Harmony 补丁模式：`../../docs/knowledge/harmony-patching.md`

