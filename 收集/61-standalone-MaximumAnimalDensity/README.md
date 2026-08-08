# Maximum Animal Density 最大动物密度 Notes

## 一句话定位
限制地图格上生成的动物密度上限，解决老电脑因动物过多而卡顿。

## 关键要点
- `[HarmonyPatch(Tile, nameof(Tile.AnimalDensity), Getter)]` Postfix：`__result = Mathf.Min(__result, maxAllowedAnimalsDensity)`。
- `Settings`（`maxAllowedAnimalsDensity` 0–2 默认 1f、`isEnabled`）+ `SettingsUI : Mod`（滑块 + checkbox，key `MaximumAnimalDensity.*`）。
- `ApplyPatches`（StaticConstructorOnStartup）PatchAll。
- 单文件 mod；依赖 Harmony。

## 目录结构
```
收集/61-standalone-MaximumAnimalDensity/
├── About/About.xml
├── Common/
└── 1.6/Source/Main.cs
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

