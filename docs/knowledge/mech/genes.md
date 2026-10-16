# 基因（Biotech / Gene / Xenotype）

> **mech 层**：本文只讲**基因系统由什么构成、字段含义、内容清单**。
> 「基因怎么被继承、怎么给后代」参考基因改造/孵化流程；
> 「Ideaology 的推崇异种」见 [ideology.md](ideology.md)（推崇异种会拒绝对非推崇异种做基因改造）。
>
> **数据源与核对日期（2026-10-16）**：
> 本地 `F:\Games\GBL\rimworld\Data\`（RimWorld 1.6），中文取官方简中 DefInjected。
> 复现：`python tools/game_defs/extract_defs.py --def GeneDef GeneCategoryDef XenotypeDef GeneTemplateDef`。
>
> ⚠️ **基因系统几乎全部来自 Biotech DLC**：169 个 GeneDef 中 **Biotech 139 · Core 27 · Odyssey 3**。
> Biotech 未加载时，异种人与基因功能整体不存在。
>
> 规模：**GeneDef 169 · GeneCategoryDef 21 · XenotypeDef 12 · GeneTemplateDef 7**。

---

## 1. 框架

### 1.1 四层结构

```
XenotypeDef（异种人，12 个）──「基因蓝图」
  └─ 基因组（Genes）── 一组 GeneDef 组成「异种人的完整基因」
       ├─ 外观基因（Hair/Skin/Eye/Body）── 渲染外观
       ├─ 能力基因（Abilities）── 授予特殊能力
       ├─ 属性基因（Hediff/Stats）── 生理特征
       └─ XenoGene（X 染色体，来自父/母）── 性别特异基因

GeneDef（基因，169 个）── 单个基因定义
  ├─ displayCategory ──── 归类（能力/体液/建筑术…）
  ├─ biostatMet / biostatCpx ──── 基因统计（Met 代谢 / Cpx 复杂度）
  ├─ abilities / class ──── 授予能力或特殊行为
  └─ renderNodeProperties ──── 影响外观渲染

GeneCategoryDef（基因类别，21 个）── UI 分组
GeneTemplateDef（基因模板，7 个）── 可插拔的基因组合模板
```

### 1.2 基因的三类

169 个 GeneDef 按用途分三类：

**① 外观基因（Hair / Skin / Eye / 身体）**
- 发色 `Hair_SnowWhite`（雪白发）· `Hair_InkBlack`（墨黑）· `Hair_Gray`（灰）
- 肤色 `SkinColor*` 系列
- 这些基因用 `hairColorOverride` / `skinColorOverride` / `renderNodeProperties` 改外观，
  而非给能力。18 个有 `hairColorOverride`，12 个有 `skinColorOverride`。

**② 能力基因（Abilities）**
- `abilities` 字段授予特殊能力（9 个基因有，如 `FireSpew` 喷火 · `FoamSpray` 喷泡沫 · `Longjump` 跳跃）
- `geneClass` 给基因特殊行为类（如 `Gene_Bloodfeeder` 吸血者 · `Gene_Clotting` 快速凝血 ·
  `Gene_PsychicBonding` 灵能链接）
- `customEffectDescriptions` 描述特异效果（13 个，如「伤口极快愈合」）

**③ 属性基因（Hediff / Stats）**
- 影响生理（如对毒气免疫、快速愈合）
- `exclusionTags` 控制互斥（如「近战伤害」类基因互斥）

### 1.3 基因的关键字段

| 字段 | 出现率 | 说明 |
|---|---:|---|
| `displayCategory` | 25% | 归入哪个基因类别（`Ability` 能力 · `Hemogen` 体液 · `Archite` 建筑术…） |
| `biostatMet` | 52% | **代谢水平**——越高消耗体力越多（−1 到 −3） |
| `biostatCpx` | 22% | **基因复杂度**——影响改造难度/繁殖稳定性 |
| `selectionWeight` | 12% | 随机生成时的选取权重 |
| `exclusionTags` | 7% | 互斥标签（如「HairStyle」头发类互斥、「MeleeDamage」近战伤害类） |
| `abilities` / `geneClass` | 5% | 授予能力 / 特殊行为类 |
| `renderNodeProperties` | 7%+ | 外观渲染（毛发、皮肤、附件） |
| `ParentName` | 75% | 126/169 有父类（继承外观基类等） |

🔑 **`biostatMet`（代谢）是基因的核心平衡点**：
每个基因消耗体力，`biostatMet` 越高越贵。这解释了为什么强力基因（如多喷火）
不能无脑堆叠——**会累垮 pawn**。

### 1.4 基因的命名生成（symbolPack）

一个显著的设计：**异种人名字是按基因自动拼出来的**。
`GeneDef.symbolPack` 定义了前缀/后缀符号（如「fire」「foam」「jet」），
Xenotype 的名字由所带基因的符号组合生成（如「Pyrofoam Jet」）。

这是 Biotech 特色——玩家能造出千奇百怪的自定义异种人，名字也自动生成。

### 1.5 异种人 XenotypeDef

12 个官方异种人，全部 Biotech（Odyssey 加 `Starjack` 星旅种）：

| defName | 中文 | 特色（示意） |
|---|---|---|
| `Baseliner` | 智人种 | 无基因特效的「普通人」，基因改造基准 |
| `Neanderthal` | 尼人种 | 强壮（更多体力/近战） |
| `Pigskin` | 猪猡种 | 能吃（食量相关） |
| `Yttakin` | 毛绒种 | 毛发（保温） |
| `Impid` | 炎魔种 | 喷火类（`FireSpew` 等） |
| `Dirtmole` | 土鼹种 | 挖掘/地下 |
| `Genie` | 智灵种 | 灵能（`PsychicBonding`） |
| `Highmate` | 优侣种 | 社交/繁殖 |
| `Hussar` | 骠骑种 | 骑术/移动 |
| `Sanguophage` | 赫血种 | 吸血（`Gene_Bloodfeeder`） |
| `Waster` | 污骸种 | 辐射适应 |
| `Starjack` | 星旅种 | 星旅（Odyssey） |

🔑 **`Baseliner`（智人种）是所有基因改造的「参照系」**——
见 [ideology.md](ideology.md) §4.7：若文化推崇智人种，信徒会**拒绝一切基因改造手术**。

---

## 2. 层级

### 2.1 基因的层级（X 染色体 vs 常染色体）

```
Xenotype（异种人）
  └─ 染色体
       ├─ 常染色体 ── 两份，每人的
       │    └─ GeneDef（含 Xeno 标记的除外）
       └─ X 染色体 ── 父（母给女儿）/ 母（父给女儿）
            └─ GeneDef（带 xeno 标记，只走一条 X）
```

🔑 **X 染色体遗传是「一父一母传女」的**——这是 1.6 基因系统的遗传学核心，
决定了很多杂交繁育的特性。

### 2.2 基因组合的互斥（exclusionTags）

```
GeneDef
  └─ exclusionTags（如 MeleeDamage）
       └─ 另一个带同标签的基因 ──▶ 互斥，不能共存
```

例：`MeleeDamage` 类基因互斥（一个超近战、一个远程，不能都要）。
`HairStyle` 类基因互斥（只能一种发型）。

11/169 有 `exclusionTags`——多数基因自由叠加，只有少数互斥。

### 2.3 基因 → 能力/属性

```
GeneDef
  ├─ abilities ──▶ AbilityDef（喷火、跳跃等，直接给能力）
  ├─ geneClass ──▶ C# 类（吸血、凝血等特殊行为）
  ├─ statFactors/statOffsets ──▶ 改属性
  └─ customEffectDescriptions ──▶ UI 上显示的效果说明
```

与 [skills-traits.md](skills-traits.md) 的特质一样，基因通过
`statFactors` / `statOffsets` 改数值，也通过专属类获得独特行为。

### 2.4 基因影响外观

```
GeneDef
  └─ renderNodeProperties
       ├─ parentTagDef（挂 Body/Head）
       ├─ nodeClass（PawnRenderNode_Fur 等）
       ├─ texPath（贴图，如红眼附件）
       ├─ colorType（Hair/Skin）
       └─ workerClass（渲染 worker）
```

外观基因通过渲染节点（render node）叠加到 pawn 身上——
这就是「红眼」「毛绒」等外观的实现方式（对应贴图约定见
[../ref/assets-and-rendering.md](../ref/assets-and-rendering.md)）。

---

## 3. 内容

### 3.1 基因全表（169 个）

规模：**Biotech 139 · Core 27 · Odyssey 3**。官方中文标签 169/169 全有。

> 完整 169 行清单：`python tools/game_defs/extract_defs.py --def GeneDef`
> 字段级（每个基因的代谢/能力/互斥）：
> `python tools/game_defs/extract_defs.py --def GeneDef --fields biostatMet abilities displayCategory exclusionTags`

按用途分组（代表性）：

| 类别 | 数量级 | 代表 |
|---|---:|---|
| **发色基因** | ~18 | `Hair_SnowWhite` 雪白发 · `Hair_InkBlack` 墨黑 · `Hair_Gray` 灰 |
| **肤色基因** | ~12 | `SkinColorMelanin`（黑色素）等 |
| **眼/附件基因** | ~14 | 红眼、浓眉等（`renderNodeProperties`） |
| **喷火类** | 多 | `FireSpew` 喷火 · `FoamSpray` 灭火泡沫 · `Longjump` 跳跃 |
| **能力类** | 9 有 abilities | 各种特异能力 |
| **属性类** | 多数 | 代谢、免疫、愈合等 |
| **Core 的 27 个** | 27 | 基础/遗传相关（Biotech 未加载也在） |

### 3.2 基因类别（21 个）

`GeneCategoryDef` 是 UI 分组。20 个 `displayCategory` 值可见的实际分组包括：
`Ability` 能力 · `Hemogen` 体液 · `Archite` 建筑术 等 10 个可见类别。

> 复现：`python tools/game_defs/extract_defs.py --def GeneCategoryDef`

### 3.3 基因模板（7 个）

`GeneTemplateDef`（7 个）是**可插拔的基因组合模板**——供快速生成特定类型的异种人。

---

## 4. 解读

### 4.1 基因 = 母系继承 + X 染色体

玩家可以给 pawn 插基因（做基因手术），也可以让基因自然遗传。
- **基因手术**：在培养舱/基因台做，消耗基因复杂度（`biostatCpx`）
- **自然遗传**：孩子从父母继承基因，X 染色体特殊（父传女）

### 4.2 Ideaology 与基因的冲突

**这是两个 DLC 最戏剧性的交汇点**（见 [ideology.md](ideology.md) §4.7）：

- 若文化**推崇「智人种」**（`Baseliner`），信徒**拒绝一切基因改造手术**
- 若文化**推崇非智人种**，信徒**拒绝让自己变成非推崇的异种**
- 推崇异种的**意见影响很大**（推崇 +10 / 反感 −30）

所以基因 mod 和 Ideology mod 可能互相冲突——改基因系统要顾及文化系统。

### 4.3 基因的代谢平衡

每个基因消耗体力（`biostatMet`）。这防止了「无脑堆叠强力基因」：
- 一个喷火基因 + 一个跳跃基因 = 双倍代谢负担
- 高代谢基因多了，pawn 体力不够，战斗力反而下降

做基因 mod 时，`biostatMet` 是核心平衡旋钮——强力能力必须配高代谢代价。

### 4.4 基因与 XenoGeneTemplate

`GeneTemplateDef` 允许 mod 打包一组基因成一个「模板」，快速造异种人。
这是基因 mod 的便捷入口——不用手搓几十个基因，直接引用一个模板。

---

## 5. modder 常见坑

1. **基因几乎全在 Biotech**（139/169）。不加 `MayRequire="Ludeon.RimWorld.Biotech"`
   引用基因会导致 Biotech 未加载时出问题。
2. **`biostatMet` 是平衡核心**。新增强力基因必须配高 `biostatMet`，
   否则玩家无脑堆叠就没难度了。
3. **`exclusionTags` 控互斥**（不是直接列基因名）。新增互斥基因要用标签机制，
   和已有的同标签基因自动互斥。
4. **外观基因走 `renderNodeProperties`**，要配 `nodeClass` / `texPath` / `parentTagDef`，
   贴图路径遵循 [../ref/assets-and-rendering.md](../ref/assets-and-rendering.md) 的规则。
5. **`Baseliner`（智人种）特殊**——它是基因改造的参照系，且与 Ideology 推崇异种联动，
   改它的语义要小心。
6. **X 染色体遗传是单亲的**（父传女）——做繁殖/遗传 mod 要理解这个不对称。
7. **基因名由 `symbolPack` 自动拼**——改 symbol 会影响所有含该基因的异种人名字。
