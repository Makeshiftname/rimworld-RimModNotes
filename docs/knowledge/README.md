# RimWorld Mod 知识库

本目录把仓库 83 个 RimWorld mod 目录（`收集/` 82 + `自建/` 1，`NN-功能-名称`）积累的 modding 经验
整理成可检索的知识文档。主要面向 **Copilot / AI 助手在后续 RimWorld modding 时调用**，也可供人查阅。

## 怎么用（AI 检索指南）

按主题找文档，再顺着文档里的链接去读对应 mod 的源码/笔记：

| 想做什么 / 遇到什么问题 | 去读 |
|---|---|
| 给第三方/原版功能打补丁 | [harmony-patching.md](harmony-patching.md) |
| 给切换栏加图标、画窗口、用 Widgets | [ui-and-windows.md](ui-and-windows.md) |
| 每 tick 逻辑、存档状态 | [game-and-world-components.md](game-and-world-components.md) |
| 改 Defs、PatchOperation、纯 XML 内容包 | [xml-defs-and-patches.md](xml-defs-and-patches.md) |
| 翻译 / Languages 结构 | [translations-localization.md](translations-localization.md) |
| 多版本目录、Common/、LoadFolders | [cross-version-structure.md](cross-version-structure.md) |
| 写测试（白盒 / 单测） | [testing-and-validation.md](testing-and-validation.md) |
| 卡顿 / GUI 性能剖析 | [performance-and-gui.md](performance-and-gui.md) |
| 新建 C# mod 的骨架与日志模板 | [project-templates.md](project-templates.md) |
| 发布 / 软链接 / 工坊红线 | [publishing-and-release.md](publishing-and-release.md) |
| 查游戏机制/数值、找 wiki 外部资料 | [wiki-sources.md](wiki-sources.md) |

## 参考手册层（`ref/`）

上面 10 篇是**实操笔记**——从本仓库 mod 的实际代码里提炼，讲「在本仓库怎么用」。

`ref/` 是另一层：**参考手册**——蒸馏 rimworldwiki 官方 vetted 教程区，
讲「这个机制/字段/流程的完整定义是什么」，**不含仓库特定结论**。
面向「这个字段到底是什么意思」「这个流程一共几步」这类查询。

| 想查什么 | 去读 |
|---|---|
| **中英术语对照**（wiki 全英文、笔记全中文） | [ref/glossary.md](ref/glossary.md) |
| About.xml 字段表 / 目录结构 / 大小写敏感路径 | [ref/mod-project-setup.md](ref/mod-project-setup.md) |
| PatchOperation 全类型 / MayRequire 语义与陷阱 | [ref/defs-and-patches-ref.md](ref/defs-and-patches-ref.md) |
| **1.7 Waylith 迁移** / 1.5、1.6 变更 / 测试与工坊上传 | [ref/version-migration.md](ref/version-migration.md) |
| 本地化 / 研究项目 / 武器·植物·药物蓝本 | [ref/content-blueprints.md](ref/content-blueprints.md) |
| 贴图遮罩 / 植物渲染 / 音效 / Asset Bundle | [ref/assets-and-rendering.md](ref/assets-and-rendering.md) |
| **应用启动 38 步 / pawn 生成 34 步**（判断代码跑早跑晚） | [ref/csharp-basics.md](ref/csharp-basics.md) |
| 右键菜单项 / Job + JobDriver + Toil / DefOf | [ref/csharp-feature-recipes.md](ref/csharp-feature-recipes.md) |

索引与体例说明见 [ref/README.md](ref/README.md)。

`mech/` 是第三层：**游戏机制参考手册**——讲「这个游戏系统本身由什么构成、
字段什么意思、有哪些内容、层级怎么组织」，数据来自**本地游戏 Defs + 官方简中译文**
（权威、可脚本复现），解读补自灰机 wiki。为「这个系统是什么」这类查询服务。
样板批次（2026-10-16）覆盖文化、装备、健康、需求与心情四个系统。

| 想查什么 | 去读 |
|---|---|
| **文化 Ideology**：模因/戒律/议题/风格层级、认可度公式、圣物与职位 | [mech/ideology.md](mech/ideology.md) |
| **装备 Gear**：护甲属性字段、装备层、品质、武器结构与词条 | [mech/gear.md](mech/gear.md) |
| **健康 Hediff**：分期机制、伤害类型树、身体部位与器官、感染路径 | [mech/hediff.md](mech/hediff.md) |
| **需求与心情**：需求-想法-精神状态三层叠加、成瘾闭环 | [mech/needs-mood.md](mech/needs-mood.md) |

分工、体例与底稿复现方式见 [mech/README.md](mech/README.md)；
提取脚本在 [`tools/game_defs/`](../../tools/game_defs/extract_defs.py)。

## 索引与数据

- **[mod-index.md](mod-index.md)** — 全部 mod 的索引表（自动生成）+ 人工维护的
  要点/笔记分布（`MANUAL` 段）。
- **[kb-index.json](kb-index.json)** — 机器可读元数据（`tools/kb/scan_mods.py` 生成），
  供脚本与未来工具使用。

## 维护

- 更新流程与验收门槛：[CONTRIBUTING.md](CONTRIBUTING.md)
- 工具链脚本：[`tools/kb/README.md`](../../tools/kb/README.md)

## 高质量笔记入口（学习主载体）

真实学习笔记大多不在各 mod 的根 README（多为模板占位），而在
`Notes.md` / `docs/` / `DESIGN.md` 等文件。完整分布见
[mod-index.md 的 MANUAL 段](mod-index.md#3-真实笔记分布非根-readme学习主载体)。几个重点：

- `收集/76-standalone-SmoothDragSelect/README.md` — GUI 性能剖析（死亡螺旋/限流）
- `收集/01-standalone-AlertUtility/README.md` — 入门四件套（patch/窗口/组件/tick）
- `收集/34-standalone-WorkbenchZone/1.6/Source/Notes.md` — 工作台区域实现笔记
- `收集/78-standalone-KillingReward/docs/` — 设计→实现→测试完整流程
- `收集/73-standalone-RimLocksmith/DESIGN.md`、`收集/74-standalone-UsefulStats/DESIGN.md` — 模块设计
