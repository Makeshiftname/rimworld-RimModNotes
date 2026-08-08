# Mass Info for World View Caravan with Shuttle 世界地图穿梭机重量信息显示 Notes

## 一句话定位
在世界地图远行队检查面板加一行显示穿梭机当前载重/容量。

## 关键要点
- `CaravanShuttleMassInfoComp : WorldObjectComp` 覆写 `CompInspectStringExtra()`：`parent is Caravan && caravan.Shuttle is Building_PassengerShuttle` → `CaravanShuttleUtility.GetCaravanShuttleMass(caravan)`，显示 `"Mass": x.x / y.y kg`。
- 通过 `Common/Patches/Patches.xml` 给 `WorldObjectDef[defName="Caravan"]/comps` 追加该 comp。
- **无 Harmony**（纯 XML + C# comp）；未选中穿梭机时返回 null（不显示）。

## 目录结构
```
66-CaravanShuttleMassInfoInspectString/
├── About/About.xml
├── Common/Patches/          # 注入 comp
├── Common/Languages/
└── 1.6/Source/Main.cs
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

