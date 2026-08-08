# 知识库维护指南（CONTRIBUTING）

本文档说明如何让 `docs/knowledge/` 与仓库中的 mod 保持同步。脚本详见
[`tools/kb/README.md`](../../tools/kb/README.md)。**验收门槛**：任何改动后
`python tools/kb/check_links.py` 与 `python tools/kb/check_repo_hygiene.py`
必须都退出码为 0。

## 知识库结构

```
docs/knowledge/
├── README.md               # 总索引：主题文档清单 + 检索建议
├── mod-index.md            # Mod 索引（AUTO 段脚本生成 + MANUAL 段人工维护）
├── kb-index.json           # 机器可读元数据（scan_mods.py 生成）
├── CONTRIBUTING.md         # 本文档
└── <topic>.md              # 主题聚合文档（手写）
tools/kb/                   # 工具链（Python 标准库）
```

## 规则

1. **生成/手写分离**：`mod-index.md` 中 `<!-- AUTO-GENERATED -->` 区间由
   `scan_mods.py` 重写，勿手改；`<!-- MANUAL -->` 区间（类型权威清单、笔记分布）
   人工维护，脚本永不覆盖。
2. **主题文档是手写的**：脚本只扫描/生成元数据/校验，不生成任何正文。
3. **链接相对所在 md 文件解析**：如 `docs/knowledge/mod-index.md` 里指向
   `收集/01-standalone-AlertUtility/README.md` 需写 `../../收集/01-standalone-AlertUtility/README.md`。

## 更新场景 checklist

### A. 新增一个 mod
0. 放入正确大类目录：`自建/`（仓库主本人原创可发布）或 `收集/`（收集的他人 mod）；
   目录名 `NN-功能-名称`，功能前缀 = standalone/patch/xml/translation/lib/empty/special（按类型定，编号在大类内唯一）
1. `python tools/kb/scan_mods.py` —— 刷新索引元数据段 + `kb-index.json`
2. `python tools/kb/scan_mods.py --check` —— 确认新增项进入变更报告
3. 若新 mod 引入未覆盖的知识点：在 `docs/knowledge/` 新增主题文档或补充
   `MANUAL` 段的「真实笔记分布」；否则在 mod-index 的 MANUAL 段补一行要点
4. 若仍为模板占位 README，按「README 重写」场景处理
5. `python tools/kb/check_links.py` —— 必须通过
6. `python tools/kb/check_repo_hygiene.py` —— 必须通过（无 `.bak`/跟踪 PDB/目录命名问题）

### B. 修改已有 mod（改版本 / 加功能 / 加 docs / 换类型）
1. `python tools/kb/scan_mods.py --check` —— 查看变更报告
2. 只更新受影响的主题文档与 `MANUAL` 段要点（类型变化时同步「类型权威清单」）
3. `python tools/kb/check_links.py` 与 `python tools/kb/check_repo_hygiene.py` —— 必须通过

### C. 重写占位 README（阶段 F 批次任务）
1. 用 `python tools/kb/validate_readme.py --todo` 查看待重写清单
2. 按批次（每批 8–12 个 mod）重写，遵循 README 模板（见下）
3. 每批完成：`python tools/kb/validate_readme.py --verify <mod>` 验收每个 mod
4. `python tools/kb/scan_mods.py` —— 刷新索引（README 状态列会变为 `ok`）
5. 若重写中提炼出新知识点，同步补充对应主题文档
6. `python tools/kb/check_links.py` 与 `python tools/kb/check_repo_hygiene.py` —— 必须通过

## README 重写模板

参考 `收集/01-standalone-AlertUtility/README.md`（学习要点风格）与 `收集/79-xml-RimFlixAnimeShows/AGENTS.md`
（操作手册风格），建议结构：

```markdown
# <Mod 名称> Notes

## 一句话定位
（这个 mod 解决什么问题）

## 解决的问题 / 关键要点
- How to ...（关键技术点，逐条列出）

## 目录结构
（About / Common / 版本目录 / Source / Languages / docs / Tests 等）

## 构建与部署
（dotnet build 命令、软链接、加载顺序）

## 相关文件
（链接到源码、docs、测试）
```

内容必须与 `About/About.xml` 一致（名称、描述、依赖），不得为模板占位。
