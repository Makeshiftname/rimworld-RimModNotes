# RimWorld Modding 学习笔记仓库

个人 RimWorld modding 学习作品集：收集与自建 RimWorld mod，并把 modding 经验沉淀成可检索的知识库。作者 RunningBugs。

## 仓库结构

```
自建/    RunningBugs 原创 mod（可实际使用，编号从 01 起）
收集/    收集的他人 mod（学习用途，不改动 mod 本体），重编 01–82
docs/knowledge/  知识库：主题文档 + 全部 mod 索引（mod-index.md）
tools/kb/        知识库维护工具链（纯 Python 标准库，无依赖）
```

目录名 `NN-功能-名称`，功能前缀 = `standalone` / `patch` / `xml` / `translation` / `lib` / `empty` / `special`（与 mod-index Type 一致）。除 `自建/` 外均为收集的他人 mod，不宜改动 mod 本体。

## 快速开始

- 全部 mod 索引 + 人工学习要点/笔记分布：[`docs/knowledge/mod-index.md`](docs/knowledge/mod-index.md)
- 按主题查 modding 经验（补丁/窗口/tick/XML/翻译/测试/性能等）：[`docs/knowledge/README.md`](docs/knowledge/README.md)
- 维护知识库（新增/修改 mod 的流程与验收门槛）：[`docs/knowledge/CONTRIBUTING.md`](docs/knowledge/CONTRIBUTING.md)

## 学习资源

- RimWorld Modding 教程（官方 wiki）：<https://rimworldwiki.com/wiki/Modding_Tutorials>
- RimWorldModGuide：<https://github.com/roxxploxx/RimWorldModGuide/wiki>
- RWModdingResources（社区聚合）：<https://spdskatr.github.io/RWModdingResources/>

## 许可

本仓库内容作者 RunningBugs，[MIT 许可](LICENSE)。`收集/` 下第三方 mod 保留原作者署名与各自许可，仅供本地学习、勿擅自发布。
