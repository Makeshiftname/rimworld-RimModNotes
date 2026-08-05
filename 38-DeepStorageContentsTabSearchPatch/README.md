# 深度存储内容页添加搜索 Deep Storage Contents Tab Search Patch Notes

## 一句话定位
给深度存储（LWM's Deep Storage）内容选项卡加搜索框。

## 关键要点
- **Prefix 整体重写** `ITab_DeepStorage_Inventory.FillTab`（`return false`），用 `QuickSearchWidget` 画搜索框。
- **大量反射**：`AccessTools.Field/Property/Method` 读写私有字段（`buildingStorage`、`size`、`scrollPosition` 等）并 Invoke 私有方法 `DrawThingRow`。
- **排序与过滤**：defName 升序 → 质量降序 → HP 比例降序；按 `Label.Contains(searchWidget.filter.Text)` 过滤。
- 引用 `LWM.DeepStorage.dll`（`ITab_DeepStorage_Inventory`、`CompDeepStorage`）。
- **注意**：About 只声明 `lwm.deepstorage`，但代码实际需要 `brrainz.harmony`（漏声明）。

## 目录结构
```
38-DeepStorageContentsTabSearchPatch/
├── About/About.xml
└── 1.5/Source/             # Main.cs + LWM.DeepStorage.dll
```

## 构建
```
cd 1.5/Source && dotnet build -c Release
```

