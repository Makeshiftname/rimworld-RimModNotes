# RecipeBook 图鉴/配方书 Notes

## 一句话定位
图鉴/配方书 mod（播放设置栏加切换图标打开搜索窗口）——但当前基本是**未完成的骨架**。

## ⚠️ 当前状态（如实标注）
**未完成 / 不完整**：
- `RecipeBookLoading` 标注 `[StaticConstructorOnStartup]` 但**只有实例构造器、无静态构造器**，`harmony.PatchAll` 实际不会触发 → 补丁很可能从未应用。
- `RecipeBookWindow.cs` 是从 `02-ItemPolicy` 的 `Dialog_ItemPolicy` 复制而来（残留 `_ItemPolicy` 引用、未定义变量 `pawn`）→ 无法编译/未接线。
- `RecipeBookDatabase : GameComponent` 是空壳；所有 XML def/patch 均被注释。
- 已完成的部分：`ToggleIconPatcher`（patch `PlaySettings.DoPlaySettingsGlobalControls` 加 toggle 图标，取 `UI/recipe_book`）+ 搜索 + 双滚动列 UI 骨架。

## 目录结构
```
03-RecipeBook/
├── About/About.xml
├── 1.4/                       # Defs/Misc（全注释）、Patches（全注释）
├── Source/                    # RecipeBook.cs、RecipeBookWindow.cs、LogUtility.cs
└── Textures/UI/recipe_book.*
```

## 构建
```
cd Source && dotnet build -c Release
```