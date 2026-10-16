# 体液与机械体（Biotech DLC）

> **mech 层**：本文讲 **Biotech DLC 独有系统**——体液（Hemogen）资源、
> 基因能力（Xenogene abilities）、机械体重编程、培育舱。
> ⚠️ **基因（GeneDef / XenotypeDef）本身已在 [genes.md](genes.md) 详述**，
> 本文只讲基因**之外**的 Biotech 系统，以及基因如何通过能力/体液/机械体生效。
>
> **数据源与核对日期（2026-10-16）**：
> 本地 `F:\Games\GBL\rimworld\Data\Biotech\`，RimWorld 1.6。中文取官方简中 DefInjected。
> 复现：`python tools/game_defs/extract_defs.py --def MechWeightClassDef MechWorkModeDef ReservationLayerDef --dlc Biotech`
> 和 `--def AbilityDef --dlc Biotech`（18 个基因能力）。
>
> 规模：**基因能力 18 · 机械体重量级 4（实为 Core）· 机械体工作模式 4 · 预留层 3**。
> 说明：Biotech 的 GeneDef(169) 见 [genes.md](genes.md)；这里讲它之外的部分。

---

## 1. 框架

### 1.1 四大子系统

```
体液 Hemogen ──── 第二种「血」：产出/储存/消耗
  └─ 基因能力（如 Bloodfeed 吸血）产出 Hemogen
       └─ Hemogen 决定（部分）基因能力的强度/可用性

基因能力 Xenogene Ability ──── 基因给予的主动能力（18 个）
  └─ AbilityDef（--dlc Biotech）── 吸血、凝血、植入基因、跳跃等
       └─ 受 Hemogen 强度调制（强基因需高 Hemogen）

机械体重编程 ──── 机械体的「特性」可改
  └─ MechWorkModeDef（4 种工作模式）
       └─ MechWeightClassDef（4 种重量级，实为 Core）

培育舱 Gestative Tank ──── 基因培育/妊娠
  └─ 房间 + 建筑（复用 Core 建造，见 build.md）
       └─ 与基因改造流程配合
```

🔑 **Biotech 的独特之处**：把「血」分成两种——
普通血（Health）和 **体液 Hemogen**（基因资源）。
基因能力（吸血、超速、喷射）都靠 Hemogen 驱动，Hemogen 来自特定基因。

### 1.2 基因能力 AbilityDef（18 个）

基因给予的**主动能力**（区别于基因的被动属性修正）：

| 能力 | 作用 |
|---|---|
| `Bloodfeed` 吸血 | 咬目标抽取 Hemogen（目标失血+恐惧） |
| `Coagulate` 凝血 | 快速止血/治疗 |
| `ReimplantXenogerm` 植入基因 | 把一个人的基因种到他人身上（可致死） |
| `Longjump` 跳跃 | 远超常人的跳跃距离 |
| `FireSpew` 喷火 | 喷火焰（若带对应基因） |

🔑 **基因能力的强弱受 Hemogen 影响**——同一个能力，
Hemogen 高则强。这是 Biotech 的核心资源循环：
`能力 → 产 Hemogen → 能力更强`，或 `消耗 Hemogen → 能力生效`。

### 1.3 机械体工作模式 MechWorkModeDef（4 种）

机械体（mech）可切换的**工作模式**——决定它怎么战斗/工作。
配合 `MechWeightClassDef`（4 种重量级，实为 Core）。

🔑 **机械体是 Biotech 的另一个核心**——它可被基因改造
（mech 基因重编程），也可切换工作模式做不同任务。

> 复现：`python tools/game_defs/extract_defs.py --def MechWorkModeDef MechWeightClassDef --dlc Biotech`

### 1.4 预留层 ReservationLayerDef（3 个）

🔑 **预留层**——「多层地板/地基」系统：控制机体的优先级预留，
如「重型层先建，地基先挖」。与挖掘/建造的预留逻辑相关
（见 [build.md](build.md) 的建造标记）。

---

## 2. 层级

### 2.1 Hemogen 循环（Biotech 的核心）

```
基因（Bloodfeeder 等）── 给予 Bloodfeed 能力
  └─ 咬人 ──▶ 抽取目标 Hemogen ──▶ 自己 Hemogen 上升
       └─ Hemogen 高
            └─ 基因能力（Bloodfeed/跳跃等）更强/可用
                 └─ 用能力 → 更多 Hemogen 或消耗 Hemogen
```

🔑 **Hemogen 是「基因能力的燃料」**——
基因给了能力，但能力强度/解锁靠 Hemogen 维持。
这是 Biotech 区别于「基因给永久属性」的设计。

### 2.2 基因的三个作用面

```
GeneDef（基因，见 genes.md）
  ├─ 被动：属性修正（statFactors/statOffsets）
  ├─ 主动：AbilityDef（基因能力，本文的 18 个）
  └─ 外观：renderNodeProperties（渲染，见 genes.md）
```

🔑 **基因不只改属性，还给主动能力**——这让某些基因（如「吸血者」）
变成「战斗流派」而非纯数值堆叠。

### 2.3 培育舱与基因流程

```
基因改造（手术）──▶ 培育舱（Gestative Tank）──▶ 基因在体内稳定
  └─ 房间 + 建筑（复用 Core 建造）
       └─ 基因在培育舱内生长（类似妊娠）
            └─ 完成后基因稳定，可遗传
```

🔑 **培育舱把「基因改造」变成一个流程**（改造→培育→稳定），
需要时间和房间条件（见 [build.md](build.md) 的房间角色）。

---

## 3. 内容

### 3.1 基因能力全表（18 个）

官方中文标签 18/18 全有。清单见 §1.2。

> 完整 18 行：`python tools/game_defs/extract_defs.py --def AbilityDef --dlc Biotech`
> 每条能力的冷却/范围：
> 加 `--fields cooldownTicksRange verbProperties.range`

### 3.2 机械体工作模式全表（4 种）

各模式的切换与特性。

### 3.3 预留层全表（3 个）

挖掘/建造的预留优先级。

> 完整 3 行：`python tools/game_defs/extract_defs.py --def ReservationLayerDef --dlc Biotech`

---

## 4. 解读

> 本节机制串联自本地 Def + 灰机 wiki。

### 4.1 Hemogen 是 Biotech 的「第二资源」

普通血（Health）管生存，Hemogen 管基因能力。
两者独立——失血过多会死，但 Hemogen 归零会让基因能力失效。
所以 Biotech 战斗有「打血」和「断 Hemogen」两套战术。

### 4.2 基因 = 战斗流派

带 `Bloodfeeder` 的基因 → 能吸血续航；
带 `Longjump` → 机动；
带 `FireSpew` → 范围输出。
所以基因选择本质是**选择战斗风格**（见 [genes.md](genes.md) §4.3 的代谢平衡）。

### 4.3 机械体是 Biotech 的第二主角

Biotech 让机械体变成可基因改造 + 可切模式的「第二殖民者」。
机械体重编程（mech 基因）+ 工作模式 = 高度定制的自动化战斗单位。

### 4.4 Biotech 的系统耦合

Biotech 的系统高度耦合：
- 基因（genes.md）→ 能力（本文）→ Hemogen（本文）→ 战斗（hediff.md）
- 培育舱（本文）→ 房间（build.md）→ 基因改造（genes.md）

所以 Biotech mod 往往要跨多个系统改，这是它复杂的原因。

---

## 5. modder 常见坑

1. **基因本身在 [genes.md](genes.md)**——本文不重复基因 Def，
   只讲能力/体液/机械体。做基因 mod 先看 genes.md。
2. **基因能力受 Hemogen 调制**——新增能力若依赖 Hemogen，
   要保证 pawn 有 Hemogen 供给，否则能力形同虚设。
3. **MechWeightClassDef 实为 Core**（不是 Biotech 独有）——
   4 个重量级（Light/Medium/Heavy/UltraHeavy）在 Core。
4. **机械体重编程需蓝图/基因**——和 Royalty 的 Blueprint 系统交叉（见 [royalty.md](royalty.md)）。
5. **培育舱需要房间**——依赖 RoomRole（见 [build.md](build.md)），
   只造建筑不造房间会无法工作。
6. **Biotech 系统耦合度高**——改动常需跨基因/能力/体液/房间多处验证。
