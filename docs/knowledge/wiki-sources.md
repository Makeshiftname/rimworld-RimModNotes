# 外部资料：RimWorld Wiki 与游戏数据源（wiki-sources）

> 适用：需要环世界游戏机制/数值资料时，「去哪找、怎么拿、怎么进知识库」。
> 定位是**外部资料网关**：wiki 内容以「蒸馏要点 + 精选索引 + 来源链接」进入知识库，
> 而非整站复制正文。

结论先行：

- **数值类内容优先读游戏本地 Defs XML**（权威、最新、无抓取与版权问题）。
- 要中文百科时用**灰机 wiki 官方 API**（可编程、本仓库已验证可用）。
- 英文官方 wiki（rimworldwiki.com）有 Cloudflare 反爬，自动化环境拿不到，
  靠真人浏览器 / Wayback 存档读取。

## 1. 数据源优先级

| 优先级 | 来源 | 说明 |
|---|---|---|
| 1 | 游戏本地 Defs XML | 最终权威：`<游戏目录>/RimWorld/Data/<Core\|DLC>/Defs/`。wiki 上物品/建筑/生物数值基本都整理自这些 XML |
| 2 | 灰机 wiki（中文，`rimworld.huijiwiki.com`） | 标准 MediaWiki，**官方 API 可编程访问**，适合按需检索中文词条 |
| 3 | rimworldwiki.com（英文官方） | 内容最全，但被 Cloudflare JS 挑战拦截：自动化一律 403，只能真人浏览器或 Wayback 存档 |

## 2. 获取方式速查（2026-08-08 实测）

### 灰机 wiki（推荐，可编程）

| 目的 | 端点 |
|---|---|
| 枚举全部页面 | `api.php?action=query&list=allpages&aplimit=20`（`continue` 分页） |
| 取页面原文 wikitext | `api.php?action=parse&page=<标题>&prop=wikitext` |
| 取渲染后 HTML | `api.php?action=parse&page=<标题>&prop=text` |
| 全文搜索 | `api.php?action=query&list=search&srsearch=<词>` |
| 结构化查询（Semantic MediaWiki） | `api.php?action=ask&query=[[Category:+]]\|limit=5` |
| 批量导出 XML | `wiki/Special:导出` |

- ⚠️ **限流**：连番请求会 403；实测**间隔 ≥10 秒 + 浏览器 UA** 即稳定 200，勿并发轰炸。
- 页面标题需先经 allpages / search 确认，直接猜标题会 404。

### rimworldwiki.com（英文，受限）

- 自动化全部 403（Cloudflare）：curl（含浏览器 UA）、抓取工具、无头浏览器全被挡。
- 可用途径：
  1. **真人浏览器**手动过验证后阅读/复制（集成浏览器需用户先过挑战再共享页面）。
  2. **Wayback 存档**：`https://web.archive.org/web/<日期>/https://rimworldwiki.com/wiki/<页名>`
     （自动化环境可能连不通 archive.org，用户网络通常可用）。
- 页面 URL 用下划线：`/wiki/Pawns`、`/wiki/Character_Types`。

## 3. 核心游戏概念 → 资料页索引

### Pawns（从 rimworldwiki 蒸馏，2026-08-08）

- **Pawn 是游戏代码术语**：指任何**使用寻路的可移动实体**（ambulatory entity that uses
  pathing）。不只是人——机械体、动物、部分 DLC 实体都算 Pawn。
- 类型树（rimworldwiki `Pawns` 页）：
  - **Mechanoids**（机械体）
  - **Animals**（动物）
    - **Humans**（人类）：Colonists（殖民者）、Raiders（袭击者）、Prisoners（囚犯）、
      Visitors（访客，见 Factions）等
    - **Insectoids**（虫族）
    - **Dryads**（树精，Ideology DLC）
    - 其他动物
  - **Entities**（实体，Anomaly DLC）
  - **Drones**（无人机，Odyssey DLC）
- 资料页：rimworldwiki `/wiki/Pawns`（`Pawn` 重定向至此），子页 `/wiki/Human`、
  `/wiki/Colonist`、`/wiki/Raiders`、`/wiki/Prisoners`、`/wiki/Mechanoid`、`/wiki/Animals`、
  `/wiki/Insectoids`、`/wiki/Dryads`、`/wiki/Entities`、`/wiki/Drones`。
- 与本地 mod 知识衔接：打 Pawn 行为补丁 → [harmony-patching.md](harmony-patching.md)；
  每 tick 的 Pawn 组件逻辑 → [game-and-world-components.md](game-and-world-components.md)。

### 中英对照：Pawns ↔ 生物（灰机，2026-08-08）

中文 wiki 没有直接对应「Pawn 术语」的页面，而是用**「生物」**页（`/wiki/生物`）按角色
类型组织，并逐条挂英文链接到 rimworldwiki（等价于 `Pawns` 概念分区）：

| 中文词条（灰机） | 灰机页 | 英文对应（rimworldwiki） |
|---|---|---|
| 生物（总览） | `/wiki/生物` | `Character Types` / `Pawns` |
| 殖民者 | `/wiki/殖民者` | `Colonist` |
| 囚犯 | `/wiki/囚犯` | `Prisoner` |
| 袭击者（页内亦称掠夺者） | `/wiki/袭击者` | `Raider` |
| 访客 | `/wiki/事件#访客` | Visitor（见 `Factions`） |
| 流浪者 | `/wiki/流浪者` | `Drifter` |
| 机械体 | `/wiki/机械体` | `Mechanoid` |
| 动物 | `/wiki/动物` | `Animals` |
| 角色属性 | `/wiki/角色属性` | Character Properties（`Characters`） |

- 角色属性下的子条目（灰机首页）：背景故事 / 健康 / 身体部位 / 心情 / 需求 / 能力 /
  技能 / 社交 / 想法 / 特性 / 属性。
- ⚠️ 灰机「生物」页带 **Wiki 翻新计划** 标记（2022-12 起全站页面无差别标记），
  部分内容可能过时，数值以英文页或游戏 Defs 为准。
- 语义差异：rimworldwiki 的 Pawn 是「任何使用寻路的可移动实体」（含机械体/动物/
  实体/无人机）；灰机「生物」更接近「遇到的生物/角色类型」集合，**不含**
  Entities（Anomaly）、Drones（Odyssey）等 DLC 新类型——查 DLC 内容以英文页为准。

### 机械体 / Mechanoids（跨 wiki 蒸馏，2026-08-08）

**定义**：被开发用作打理家务、工业生产或军事用途的人工智能机器人，只有先进文明能使用
（复杂 AI 需严格控制）。灰机 `/wiki/机械体` 最近 2 个月有更新，比「生物」页新。

**机制要点**（灰机 ↔ rimworldwiki `Mechanoids` 互证）：

- 对火焰 / 烧伤 / 高温伤害免疫；100% 毒素抗性与有毒环境抗性（中毒、腐臭、毒素无效）；
  极端温度对其无威胁（即便有舒适温度设定）。
- 不需要进食、休息，无情绪；**敌对机械体没有任何需求**（enemy mechanoids have no needs）。
- **EMP**：攻击将其暂时击晕（时间取决于 EMP 伤害值），并获得「适应」效果，接下来
  **2,200 ticks（36.67 秒）内免疫后续一切 EMP**（rimworldwiki：regardless of source）。
- 敌对机械体在战斗中倒地（downed）或**操作能力（Manipulation）降至 0%** 时立即死亡。
- 出现方式：机械族巢穴派系（远古遗迹 / 袭击 / 心灵飞船 / 枯萎飞船 / 机械集群[Royalty] /
  远古遗迹[Ideology]）；若开局派系设定删掉「机械族巢穴」则不敌对出现。
- 友方机械体：制造见 `机械体制造`（Biotech DLC）；对应 rimworldwiki `Mechanoid`。
- 相关本地 mod：`收集/53-patch-AllowTunnelersToDrillFork`（机械体可钻矿）、`收集/07-patch-RPP_Bill_AllMech_Patch`
  等（打机械体行为补丁时可作示例）。

### Modding 教程（rimworldwiki ↔ 灰机，与本地知识库映射）

- **rimworldwiki `Modding_Tutorials`**（modding 中枢页，约 1 周前更新）：
  - 官方无正式 modding API，内容由 modding 社区收集维护。
  - 分区：About RimWorld / Game Systems Guides / XML Tutorials / C# Guides /
    Updates and Migrations / Testing and Troubleshooting / Performance and Optimization /
    Slightly Outdated / Uploading to Steam Workshop。
  - 关键子页：`Recommended software`、`Mod Folder Structure`、`About.xml`、`Defs`
    （含 `MayRequire`）、`Localization`、`PatchOperations`、`Sounds`、`Textures`、
    `Research Projects`、`Basic Melee/Ranged Weapon`；另有 **`RimWorld 1.6 Mod Updates`**
    （社区 data-mine 的 1.6 变更清单，含奥德赛 DLC 剧透）。
  - 与本地知识库主题文档对应：Defs/MayRequire/PatchOperations →
    [xml-defs-and-patches.md](xml-defs-and-patches.md)；Localization →
    [translations-localization.md](translations-localization.md)；Mod Folder/About.xml →
    [project-templates.md](project-templates.md) 与 [cross-version-structure.md](cross-version-structure.md)；
    Testing/Troubleshooting → [testing-and-validation.md](testing-and-validation.md)；
    Performance → [performance-and-gui.md](performance-and-gui.md)；Steam Workshop →
    [publishing-and-release.md](publishing-and-release.md)。
- **灰机 `MOD教程`**（`/wiki/MOD教程`，首页「其他」板块）：中文 modding 教程入口，
  与 rimworldwiki `Modding_Tutorials` 对应；具体子页用灰机 API 检索。

### 版本与规模速记（2026-08-08）

| wiki | 当前稳定版 | 词条/文章数 | 备注 |
|---|---|---|---|
| rimworldwiki | PC **1.6.4633** / Console **1.23** | 2,745 | 英文；自动化被 Cloudflare 挡 |
| 灰机 | **1.6.4850** | 2,178 | 中文；API 可编程；部分页带翻新计划标记 |

> 两侧版本号不一致（灰机更新到 1.6.4850）；查具体数值时留意页面最后更新时间。

### 其他导航页（rimworldwiki ↔ 灰机首页）

| 英文导航（rimworldwiki） | 灰机首页对应板块 |
|---|---|
| Basics / Menus / Game Creation / Gameplay | 概念：游戏基础/操作控制/菜单选项/用户界面；上手指南/远征/环境/事件/研究/任务/时间/贸易/命令 |
| Plants / Resources / Gear | 植物；资源（材质/织物/药物/成瘾品/食物）；装备（武器/服装/护甲/实用物品） |
| Pawns / Characters / Character Types | 生物（殖民者/囚犯/袭击者/访客/机械体/动物）+ 角色属性 |
| Mods | 其他：MOD教程/模组/开发者模式/存档/音乐/历史版本 |

> 中文具体词条用灰机 API 搜索获取（见上表），勿直接猜标题。

## 4. 融入知识库的原则

- 知识库主题文档**全部手写**（CONTRIBUTING 规定脚本不生成正文），wiki 内容以
  「蒸馏要点 + 精选索引 + 来源链接」进入，不做整站 dump。
- 复制正文需留意版权与维护负担：数值优先「取真值自己写」，叙述优先链接原文。
- 批量离线缓存放 `docs/knowledge/` 之外（如 `tools/wiki-cache/`），不进知识库正文本体。
