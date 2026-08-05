# RimWorld Mod 知识库

本目录把仓库 79 个 RimWorld mod（编号 01–78，55 重复）积累的 modding 经验整理成
可检索的知识文档。主要面向 **Copilot / AI 助手在后续 RimWorld modding 时调用**，
也可供人查阅。

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

- `75-SmoothDragSelect/README.md` — GUI 性能剖析（死亡螺旋/限流）
- `01-AlertUtility/README.md` — 入门四件套（patch/窗口/组件/tick）
- `34-WorkbenchZone/1.6/Source/Notes.md` — 工作台区域实现笔记
- `77-KillingReward/docs/` — 设计→实现→测试完整流程
- `72-RimLocksmith/DESIGN.md`、`73-UsefulStats/DESIGN.md` — 模块设计
