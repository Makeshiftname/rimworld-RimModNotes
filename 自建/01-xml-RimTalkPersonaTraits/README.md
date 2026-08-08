# ⭐ 自建 mod · Makeshiftname

> **三重自建标注**：位于 `自建/` 大类目录（`NN-功能-名称`）/ 本 README 顶部徽标 / `mod-index.md` MANUAL 段「自建 mod」小节。
> 与 `收集/80-standalone-RimTalk`、`收集/81-patch-RimTalkExpandMemory`、`收集/82-patch-HautsAddedTraits` 等**第三方收录**（克隆收藏）明确区分。

## 一句话定位

轻量级**角色扮演向特质包**：10 个「对话人设」特质（性格/说话风格/嗜好），
**0 数值改动、纯描述、纯 XML（无 C#）**，每个特质的描述专为 **RimTalk 提示词注入**优化。

## 为什么做这个（RimTalk 提示词优化）

RimTalk（`收集/80-standalone-RimTalk`）在 `Full` 信息级别下，会把小人每个特质的
`degreeDatas/li/label` + `description` 拼进提示词：

```text
Traits: gossip:{PAWN_nameDef} is always the first to hear...
        dramatic:Everything is a performance for {PAWN_nameDef}...
```

- 描述里的 `{PAWN_nameDef}` / `{PAWN_pronoun}` / `{PAWN_possessive}` / `{PAWN_objective}`
  会被 RimTalk 自动解析成真实小人信息（对应 `CommonUtil.Sanitize` → `Formatted(pawn.Named("PAWN"))`）。
- 富文本标签会被剥离、换行会被去掉 —— 所以描述一律**不用** `<b>`/`<color>` 等标签。
- 因此「喂给 LLM 的提示词质量」≈ **特质描述文字本身**：本包的描述聚焦
  **怎么说话、聊什么**，给 LLM 明确的角色扮演钩子。

本包纯 XML 即可达成上述目标，无需 C#。

## 特质清单（10 个，初始定稿，可细调）

| defName / label                         | 中文名     | 类型     | 对话钩子                   |
| --------------------------------------- | ---------- | -------- | -------------------------- |
| `RB_Gossip` / gossip                  | 八卦精     | 性格     | 爱打听、主动聊他人近况     |
| `RB_Dramatic` / dramatic              | 戏精       | 性格     | 戏剧化独白、夸张措辞       |
| `RB_Raconteur` / raconteur            | 说书人     | 说话风格 | 长篇叙事、把日常讲成传奇   |
| `RB_Taciturn` / taciturn              | 寡言       | 说话风格 | 极简短句、开口即点睛       |
| `RB_Pedant` / pedant                  | 字面人     | 说话风格 | 抠定义、纠正措辞、字面理解 |
| `RB_TeaConnoisseur` / tea connoisseur | 茶道爱好者 | 嗜好     | 以茶会友、品评生活细节     |
| `RB_Angler` / angler                  | 钓鱼佬     | 嗜好     | 「跑掉的那条鱼」式吹牛     |
| `RB_Stargazer` / stargazer            | 观星者     | 嗜好     | 星空哲思、深夜有感         |
| `RB_Daydreamer` / daydreamer          | 白日梦者   | 性格     | 幻想跑偏、话题神游         |
| `RB_Curmudgeon` / curmudgeon          | 老牢骚     | 性格     | 刀子嘴豆腐心、毒舌吐槽     |

- 全部 `commonality` = 1.0、degree 0、**无数值/thought/mentalState**。
- 内部冲突：`Taciturn` 与 `Gossip` / `Raconteur` / `Dramatic` 互斥（说话风格矛盾）。

## 目录结构

```
自建/01-xml-RimTalkPersonaTraits/
├── About/About.xml                                  # packageId RunningBugs.RimTalkPersonaTraits, 1.6
├── 1.6/Defs/Traits.xml                              # 10 个 TraitDef（纯 XML）
├── Languages/ChineseSimplified/DefInjected/TraitDef/Traits.xml   # 中文 label + description
└── README.md
```

## 构建 / 验证

纯 XML mod，无编译步骤。验证：

```bash
python tools/kb/scan_mods.py --check        # 确认 82 被正确识别
python tools/kb/check_links.py              # 验收门槛：必须退出码 0
```

游戏内验证（可选，需 RimWorld 环境）：软链到 `Mods/` → 开档观察特质随机出现 →
RimTalk 对话（Full 信息级别）确认 `Traits:` 行包含本包特质描述。

## 待细调（后调项）

- `conflictingTraits`：若安装了 Vanilla Traits Expanded 等特质 mod，同名特质
  （Angler / Gossip / Stargazer 等）需按实际安装补写 `MayRequire` 冲突。
- `commonality`：按实际出现频率微调。
- 特质清单：随时增删，改时同步本表 + 中文翻译 + `mod-index.md` MANUAL 段。

## 相关参考

- RimTalk 提示词注入机制：`../../收集/80-standalone-RimTalk/Source/Service/ContextBuilder.cs`（`GetTraitsContext`）
- TraitDef 结构参考：`../../收集/82-patch-HautsAddedTraits/1.6/Defs/Traits.xml`
- 仓库 mod 索引（含本包自建标注）：`../docs/knowledge/mod-index.md`
