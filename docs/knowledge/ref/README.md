# 参考手册层（ref/）

本目录是知识库的**参考手册层**：蒸馏外部权威资料（rimworldwiki `Modding Tutorials` vetted 区），
写成短而准、可检索的参考文档。

## 与上层主题笔记的区别

| | 上层 `docs/knowledge/*.md` | 本目录 `ref/*.md` |
|---|---|---|
| 性质 | **实操笔记** | **参考手册** |
| 来源 | 从本仓库 87 个 mod 的实际代码里提炼 | rimworldwiki 官方维护的 vetted 教程区 |
| 体量 | 每篇几十行，短 | 每篇数百行，按主题成体系 |
| 写法 | 「在本仓库怎么用」 | 「这个机制/字段/流程的完整定义是什么」 |
| 是否含仓库结论 | 是 | **否** |

**本目录不写仓库特定结论、不写改造 TODO、不做影响评估**——那是上层的事。
本目录只负责把外部资料讲清楚，让上层笔记可以放心链接过来。

## 索引

| 文档 | 覆盖的 wiki 页 | 什么时候看它 |
|---|---|---|
| **[glossary.md](glossary.md)** | 自建 | ⭐ **中英术语对照**。wiki 全英文、笔记全中文，检索一切的前提 |
| **[mod-project-setup.md](mod-project-setup.md)** | About.xml / Mod Folder Structure / Recommended software / Setting up a solution / Decompiling source code | 建 mod 骨架、配工程、加引用、读原版源码 |
| **[defs-and-patches-ref.md](defs-and-patches-ref.md)** | Defs / MayRequire / PatchOperations / Apparel Tags | 写补丁、条件加载、查 PatchOperation 类型 |
| **[version-migration.md](version-migration.md)** | 1.5 / 1.6 / 1.7 Mod Updates / Testing mods / Steam Workshop 段 | ⭐ **1.7（Waylith）迁移**、版本变更清单、测试与工坊上传 |
| **[content-blueprints.md](content-blueprints.md)** | Localization / Research Projects / Basic Melee & Ranged Weapon / Basic Plant / Custom Drug | 照着写新 Def：本地化、研究项目、武器、植物、配方、药物 |
| **[assets-and-rendering.md](assets-and-rendering.md)** | Textures / Plant Rendering / Sounds / Asset Bundles | 画贴图、做遮罩、植物视觉、音效、打资源包 |
| **[csharp-basics.md](csharp-basics.md)** | Application Startup / Pawn Generation Process | ⭐ **判断 mod 代码「跑得太早/太晚」** |
| **[csharp-feature-recipes.md](csharp-feature-recipes.md)** | Example FloatMenuOptionProvider / Example Mending Job | 加右键菜单项、写 Job/JobDriver/Toil、DefOf 模式 |

## 使用约定

- **每篇开头都带来源页链接与最后核对日期**，wiki 更新后按链接复查。
- **成熟度已在各篇开头标注**：wiki 自己标了 Under Review / Outdated / Placeholder 的页面，
  不要当成定论。
- 各篇末尾的「仍未抓取的部分」是本轮抓取的诚实缺口，不要当成「wiki 里没有」。
  （截至 2026-10-15 复核，vetted 区已取全，无此类缺口。）
- 本目录内容**全部手写蒸馏**，不做整站 dump（见 [../wiki-sources.md](../wiki-sources.md) §4）。

## 数据来源与更新

- 抓取方式：内置浏览器访问 rimworldwiki，**逐页取原文**——`?action=raw` 最省事
  （纯文本、无 JSON 转义开销，单页通常一次读完）；多页批量 API 请求反而容易超读取上限。
- 首次访问会撞 Cloudflare 挑战，需要 `reload` + 等待约 15 秒；通过后同标签页连抓很快。
- 详见 [../wiki-sources.md](../wiki-sources.md)。
