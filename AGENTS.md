# AGENTS.md — RimWorld Modding 仓库

本仓库是个人 RimWorld modding 学习作品集：**83 个 mod 目录**——`收集/` 82 个（收集的他人 mod，重编 `01–82`，目录名带功能前缀）+ `自建/` 1 个（**仓库主本人原创**，编号从 `01` 起）。
仓库以 [RunningBugs/rimworld-RimModNotes](https://github.com/RunningBugs/rimworld-RimModNotes) 为基础 fork 搭建；除 `自建/` 外均为**收集/学习的他人 mod，不宜改动 mod 本体**（含 About.xml 内容/C#/Defs）；`packageId` 前缀 `RunningBugs.*`，MIT 许可。

## 仓库结构

- 两级分类：`自建/`（仓库主本人原创可发布）、`收集/`（收集的他人 mod）；目录名 `NN-功能-名称`，
  功能前缀 = standalone/patch/xml/translation/lib/empty/special（与 mod-index Type 一致）。
  每个 mod 内含 `About/About.xml`、版本目录（`1.4/` `1.5/` `1.6/`）、`Common/`（跨版本共享）、
  `Source/`（C#）、`Languages/`、`docs/`、`Tests/` 等。
- 类型分布（人工核实，即目录功能前缀）：patch 26 / standalone 44 / xml 5（30,37,46,52,79）/ 翻译 4
  （04,13,18,25；06 空）/ lib 1（09）/ special 2（06,32）。
- **既有事实勿改**：06 空目录、32 内容在 `backup/`；55 重复编号已于 2026-08-08 修正
  （现为 `55-patch-CommonModCompatibilityPatches` 与 `56-standalone-LimitedLetterSlots`）。

## 知识库（重要）

完整的 modding 经验提炼在 **`docs/knowledge/`**，开始任何 RimWorld 任务前先看
[总索引](docs/knowledge/README.md)：

- 想给原版/第三方功能打补丁 → `docs/knowledge/harmony-patching.md`
- 画窗口/加切换栏图标 → `docs/knowledge/ui-and-windows.md`
- 每 tick 逻辑/存档 → `docs/knowledge/game-and-world-components.md`
- 改 Defs / PatchOperation / 纯 XML → `docs/knowledge/xml-defs-and-patches.md`
- 翻译 → `docs/knowledge/translations-localization.md`
- 跨版本目录/Common/ → `docs/knowledge/cross-version-structure.md`
- 测试 → `docs/knowledge/testing-and-validation.md`
- 卡顿/GUI 性能 → `docs/knowledge/performance-and-gui.md`
- 新建 C# mod 骨架/日志模板 → `docs/knowledge/project-templates.md`
- 发布/软链接/工坊红线 → `docs/knowledge/publishing-and-release.md`

全部 mod 的索引表 + 人工要点/笔记分布：`docs/knowledge/mod-index.md`。
**注意**：多数 mod 的根 `README.md` 是模板占位；真实学习笔记在
`Notes.md` / `docs/` / `DESIGN.md` 等文件（分布见 mod-index 的 MANUAL 段）。

## 关键约定

- **跨版本**：共享资源放 `Common/`；老 mod 单 1.4，新收集 mod（编号 ≥54）基本仅 1.6；避免用 LoadFolders.xml。
- **测试**：RimWorld 运行时对象无法在游戏外实例化，用 Python 静态白盒测试
  （`Tests/whitebox/test_*_static.py` + `run_whitebox.sh`）；纯逻辑可加 C# 单测。
- **发布**：13 个 mod 带 `_PublisherPlus.xml`（16,26,57,60,66,67,69,70,71,73,75,76,78；发布时排除 `obj/`）；patch 第三方 mod 时其
  packageId 写入 `<loadAfter>` 而非 `<modDependencies>`；改动后 `About.xml` 描述与代码保持同步。
- **构建**：`cd <版本>/Source && dotnet build -c Release`，产物 DLL 随源码提交。
- **日志**：复用 `Logger.cs` 骨架（`Log.Message/Warning/Error` 自动带调用位置）。

## 维护知识库（当新增/修改 mod 时）

工具链在 `tools/kb/`（纯 Python 标准库），完整流程见
[docs/knowledge/CONTRIBUTING.md](docs/knowledge/CONTRIBUTING.md)：

```bash
python tools/kb/scan_mods.py            # 重新生成 mod-index 元数据段（保留 MANUAL 段）
python tools/kb/scan_mods.py --check    # 查看变更报告
python tools/kb/check_links.py          # 验收门槛：必须退出码 0
python tools/kb/check_repo_hygiene.py   # 结构卫生门槛（.bak/PDB/目录命名）：必须退出码 0
python tools/kb/validate_readme.py --todo   # 查看占位 README 待重写清单
```

**规则**：`mod-index.md` 的 `AUTO` 段由脚本生成勿手改；`MANUAL` 段人工维护；
主题文档全部手写（脚本不做内容生成）。
