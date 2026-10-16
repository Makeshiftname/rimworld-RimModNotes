# RimWorld Modding 学习笔记仓库

个人 RimWorld modding 学习作品集：收集与自建 RimWorld mod，并把 modding 经验沉淀成可检索的知识库。
基于 [RunningBugs/rimworld-RimModNotes](https://github.com/RunningBugs/rimworld-RimModNotes) fork 搭建；`自建/` 下的 mod 为**仓库主本人原创**，`收集/` 为收集的他人 mod。

## 仓库结构

```
自建/    仓库主本人原创 mod（可实际使用，编号从 01 起）
收集/    收集的他人 mod（学习用途，不改动 mod 本体），重编 01–86
docs/knowledge/  知识库：主题笔记 + 参考手册层(ref/) + 游戏机制层(mech/) + mod 索引
tools/kb/        知识库维护工具链（纯 Python 标准库，无依赖）
tools/game_defs/ 游戏 Def 底稿提取脚本（读本地游戏数据，供 mech/ 层核对）
```

目录名 `NN-功能-名称`，功能前缀 = `standalone` / `patch` / `xml` / `translation` / `lib` / `empty` / `special`（与 mod-index Type 一致）。除 `自建/` 外均为收集的他人 mod，不宜改动 mod 本体。

## 快速开始

- 全部 mod 索引 + 人工学习要点/笔记分布：[`docs/knowledge/mod-index.md`](docs/knowledge/mod-index.md)
- 按主题查 modding 经验（补丁/窗口/tick/XML/翻译/测试/性能等）：[`docs/knowledge/README.md`](docs/knowledge/README.md)
- **写 mod 前查实现**（功能怎么实现、怎么写）：[`docs/knowledge/ref/README.md`](docs/knowledge/ref/README.md)
- **查游戏系统本身**（文化/装备/健康/心情等由什么构成）：[`docs/knowledge/mech/README.md`](docs/knowledge/mech/README.md)
- 维护知识库（新增/修改 mod 的流程与验收门槛）：[`docs/knowledge/CONTRIBUTING.md`](docs/knowledge/CONTRIBUTING.md)

## 学习资源

- RimWorld Modding 教程（官方 wiki）：<https://rimworldwiki.com/wiki/Modding_Tutorials>
- RimWorld中文维基百科：<https://rimworld.huijiwiki.com/wiki/%E9%A6%96%E9%A1%B5>
- RimWorldModGuide：<https://github.com/roxxploxx/RimWorldModGuide/wiki>
- RWModdingResources（社区聚合）：<https://spdskatr.github.io/RWModdingResources/>

## 许可

`自建/` mod 为**仓库主本人原创**，[MIT 许可](LICENSE)。`收集/` 下第三方 mod（含继承自 RunningBugs 上游仓库的）保留原作者署名与各自许可，仅供本地学习、勿擅自发布。
