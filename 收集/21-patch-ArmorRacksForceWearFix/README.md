# Armor Racks Force Wear Fix Notes

## 一句话定位
修复 Armor Racks 的「强制穿戴」功能异常（强制穿上架上装备时的行为问题）。

## 关键要点
- **Harmony patch + IL**：`ArmorRacksPatches` patch Armor Racks 的强制穿戴逻辑；仓库内保留 `TargetIL.il`（目标方法 IL 转储，用于分析插入点）。
- **引用第三方程序集**：`Source/` 内置 `ArmorRacks.dll` 供编译。
- **注意**：`namespace Template` 为模板残留命名（本应改名），后续可清理。
- 依赖：Armor Racks（`khamenman.armorracks`）+ Harmony。
- 参考：`../../docs/knowledge/harmony-patching.md`

## 目录结构
```
21-ArmorRacksForceWearFix/
├── About/About.xml
└── Source/                # Main.cs、TargetIL.il、Logger.cs + ArmorRacks.dll
```

## 构建
```
cd Source && dotnet build -c Release
```

