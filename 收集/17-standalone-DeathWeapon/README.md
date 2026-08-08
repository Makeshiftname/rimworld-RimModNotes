# Death Weapon 死神之刃 Notes

## 一句话定位
提供一把「斩断不死」的武器：受击单位直接死亡，手工加工点 1 块钢铁制作，无科技需求。

## 关键要点
- **致死伤害**：`DeathDamageWorker`（自定义 `DamageWorker`）在命中时直接击杀目标。
- 武器为新增 Def（ThingDef/Weapon），在版本目录 `Defs/` 中定义。
- 依赖 Harmony；支持 1.4/1.5/1.6。
- 参考：Harmony 启动装配见 `../../docs/knowledge/harmony-patching.md`。

## 目录结构
```
17-DeathWeapon/
├── About/About.xml
├── 1.4|1.5|1.6/Source/    # Main.cs、DeathDamageWorker.cs、Logger.cs
└── 1.4|1.5|1.6/Defs/      # 武器 Def
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

