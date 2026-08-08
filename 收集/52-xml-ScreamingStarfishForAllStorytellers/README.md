# 海星的尖叫响彻环世界 Screaming Starfish for All Storytellers Notes

## 一句话定位
把 `ZM.ScreamingStarfish` 叙事者的「海星尖叫」效果扩展到所有叙事者。

## 关键要点
- **纯 XML**：`Common/Patches/Patches.xml` 单个 `PatchOperationFindMod`（按 mod 的 **name "ScreamingStarfish"** 匹配，非 packageId）。
- 命中后 `PatchOperationAdd`：给所有缺少该 comp 的 `StorytellerDef/comps` 加 `<li Class="ScreamingStarfish.StorytellerCompProperties_PostSounds"/>`。
- 依赖 `ZM.ScreamingStarfish`（loadAfter + modDependencies）。
- 注意：若原 mod 改名会失效（FindMod 按 name 匹配）。

## 目录结构
```
收集/52-xml-ScreamingStarfishForAllStorytellers/
├── About/About.xml
└── Common/Patches/           # Patches.xml
```

## 相关文件
- `Common/Patches/Patches.xml` — 叙事者 comp 注入
- XML patch 知识：`../../docs/knowledge/xml-defs-and-patches.md`

