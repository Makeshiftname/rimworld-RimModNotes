# 游戏机制参考手册（mech 层）

> **这一层讲「游戏系统本身长什么样」**——系统由什么构成、数据模型长什么样、
> 有哪些内容、有哪些层级关系。
> 目的：**写/改 mod 时快速查「这个系统是什么、字段什么意思、能怎么扩展」**。

---

## 这个目录解决什么问题

RimWorld modding 时经常需要回答三类问题，本知识库把它们分给了三层：

| 你想问的 | 去哪一层 |
|---|---|
| **这个系统是什么？由什么构成？有哪些内容？** | 👉 **本目录（mech 层）** |
| 这个功能怎么实现？流程怎么走？直接抄一个能用的 Def？ | [../ref/](../ref/README.md)（参考手册层，来自 Modding Tutorials） |
| 改某个具体 mod 的代码怎么改？调优技巧？ | 上层主题笔记（[../harmony-patching.md](../harmony-patching.md) 等） |

**举例**：
- 「Precept（戒律）是什么？有哪些字段？」→ 本目录 [ideology.md](ideology.md)
- 「pawn 生成时第 17 步会怎么设 Ideo？」→ [../ref/csharp-basics.md](../ref/csharp-basics.md)
- 「给我一个能用的 HediffDef 模板」→ [../ref/content-blueprints.md](../ref/content-blueprints.md)

---

## 本批文档（2026-10-16）

| 文档 | 系统 | 核心 Def | 规模 |
|---|---|---|---|
| **[ideology.md](ideology.md)** | 文化 Ideology（DLC） | MemeDef · PreceptDef · IssueDef · StyleCategoryDef | 模因 36 · 戒律 215 · 议题 72 |
| **[gear.md](gear.md)** | 装备（武器/护甲/品质/词条） | ThingDef · WeaponTraitDef · ApparelLayerDef | 护甲 115 · 武器 106 · 词条 61 |
| **[hediff.md](hediff.md)** | 健康（疾病/伤口/部位） | HediffDef · BodyPartDef · DamageDef | Hediff 324 · 部位 141 · 伤害 52 |
| **[needs-mood.md](needs-mood.md)** | 需求与心情 | NeedDef · ThoughtDef · MentalStateDef | 需求 24 · 想法 923 · 精神状态 46 |

> 这是**样板批次**——先做 4 篇验证体例与深度。剩余系统（派系、任务、工作排班、
> 基因、技能特质、世界地图、生产…）待体例验收后再铺开。

---

## 数据源与可复现性

**权威源是本地游戏数据**，不是 wiki：

```
F:\Games\GBL\rimworld\Data\
├── <DLC>/Defs/**/*.xml              ← Def 结构：defName、父类、字段
└── <DLC>/Languages/ChineseSimplified (简体中文).tar
       └── DefInjected/<DefType>/*.xml   ← 官方中文标签与描述
              <!-- 每条都带 EN: 英文原文注释 -->
```

本批所有数字、字段统计、中文名都来自**本地 RimWorld 1.6（全 6 DLC）**。
中文用**官方简中译文**（零翻译风险）。机制解读部分补自
[灰机 wiki](https://rimworld.huijiwiki.com/)。

**可复现**：底稿用脚本重新生成（纯标准库，无需依赖）：

```bash
# 看看有哪些 Def 类型
python tools/game_defs/extract_defs.py --list

# 抽某个系统的底稿（字段概览 + 内容清单 + 实例详情）
python tools/game_defs/extract_defs.py --def MemeDef PreceptDef IssueDef

# ThingDef 有 1800+，用过滤条件切子集
python tools/game_defs/extract_defs.py --def ThingDef --source apparel
python tools/game_defs/extract_defs.py --def ThingDef --source misc/weapons

# 限定 DLC / 限定字段
python tools/game_defs/extract_defs.py --def PreceptDef --dlc Ideology --fields comps.li.thought
```

脚本输出落在 `tools/game_defs/.cache/`（**已 gitignore**，可再生产物不入会话）。
换游戏版本后重跑脚本即可刷新本层数据。

> 游戏路径默认探测本机安装位置（`F:\Games\GBL\rimworld` 及常见 Steam 路径），
> 其他机器用 `--game "路径"` 覆盖。

---

## 体例约定

每篇按四段组织，**这是 mech 层的统一结构**：

1. **框架**——系统由哪些 Def 构成、数据模型长什么样、核心字段含义
2. **层级**——包含/继承/分期/分组关系（Meme→Precept、Hediff stages、装备层…）
3. **内容**——实例清单与分类归纳（代表性 + 可复现的完整清单查询方式）
4. **解读**——怎么运作、设计意图、跨系统联动（补自 wiki）
5. **modder 常见坑**——实际开发中最容易踩的 N 个雷

### 与现有层的边界

- **不重复**「怎么写一个能用的 Def 蓝本」→ 那是 [../ref/content-blueprints.md](../ref/content-blueprints.md) 的事。
- **不重复**「这个系统在什么流程节点被调用」→ 那是 [../ref/csharp-basics.md](../ref/csharp-basics.md)（pawn 生成流程等）。
- **不重复**「怎么打补丁 / 怎么存档」→ 那是上层主题笔记的事。
- 本层也**不含仓库特定结论**（不引用 `收集/` 里的具体 mod），保持可移植。
  与本地 mod 的关联留在上层。

### 数字的可信度

所有规模数字（实例数、字段占比）都由脚本实测得出，**不是估算**，
可随时用上面的命令复现。若以后换游戏版本，数字会变——以脚本输出为准。
