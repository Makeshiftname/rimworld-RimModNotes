# 虚空与实体（Anomaly DLC）

> **mech 层**：本文讲 **Anomaly DLC 独有的系统**——虚空巨石（Monolith）、
> 虚空之地、实体（Entity）图鉴、虚空感染。**复用 Core 的部分**
> （ThingDef / HediffDef / 能力）指回其他文档。
>
> **数据源与核对日期（2026-10-16）**：
> 本地 `F:\Games\GBL\rimworld\Data\Anomaly\`，RimWorld 1.6。中文取官方简中 DefInjected。
> 复现：`python tools/game_defs/extract_defs.py --def EntityCodexEntryDef MonolithLevelDef AnomalyPlaystyleDef EntityCategoryDef --dlc Anomaly`
>
> 规模：**实体图鉴 27 · 巨石等级 7 · Anomaly 能力 21 · 玩法风格 3 · 实体类别 3**。
> ⚠️ Anomaly 是**最复杂、最容易崩档**的 DLC——虚空感染触发战斗失败，且大量
> 效果是「持续存在的全局异常」，与存档深度绑定。

---

## 1. 框架

### 1.1 四大子系统

```
虚空巨石 Monolith ──── 世界的「异常源」
  └─ MonolithLevelDef（7 阶段）── 从沉睡到觉醒再到拥抱/干扰
       └─ 阶段决定：虚空之地是否出现、实体强度、怪物刷新

虚空之地 Void ──── 虚空侵蚀的区域
  └─ TileMutatorDef（87 种，Anomaly 占大部分）── 地面变异
       └─ 虚空生物、虚空方尖碑、虚空节点

实体 Entity ──── 特殊生物（27 种）
  └─ EntityCodexEntryDef（27）── 图鉴记录
       ├─ 实体类别 EntityCategoryDef（3 种）── 血肉/虚空/其他
       └─ 实体本身是 ThingDef（复用 Core 实体结构）

虚空感染 Void Infection ──── 「被虚空注视」的诅咒状态
  └─ HediffDef（见 hediff.md）── 严重时触发战斗失败
  └─ 与实体战斗时才被「吸引」（如专精虚空）
```

🔑 **Anomaly 的核心张力**：虚空巨石会逐渐觉醒（`MonolithLevelDef`），
世界随虚空等级越来越危险；玩家可选择「拥抱虚空」获取强力，
或「干扰虚空」保持安全。

### 1.2 虚空巨石等级 MonolithLevelDef（7 个）

🔑 **巨石有 7 个等级**，构成一条从沉睡到失控的进度线：

| defName | 中文 | 性质 |
|---|---|---|
| `Inactive` | 沉睡 | 完全不活跃 |
| `Waking` | 苏醒中 | 开始有轻微影响 |
| `Stirring` | 涌动 | 虚空之地出现 |
| `Gleaming` | 微光 | 虚空生物活跃 |
| `VoidAwakened` | 虚空已醒 | 高危 |
| `Embraced` | 已拥抱 | 玩家选择拥抱虚空（强力但危险） |
| `Disrupted` | 已干扰 | 玩家选择干扰虚空（安全但放弃强力） |

🔑 **巨石等级是全局的、存档级的**——它不随地图重置，
升级会让整个世界变危险（这是 Anomaly 最独特的机制）。

### 1.3 实体图鉴 EntityCodexEntryDef（27 个）

🔑 **实体图鉴记录 Anomaly 的 27 种实体**——不只是怪物，
还包括各种异常现象：

**实体生物**：`Fleshbeasts` 血肉兽 · `Devourer` 吞噬兽 · `Dreadmeld` 惊惧母兽 ·
`Gorehulk` 血棘巨人 · `Chimera` 嵌合兽 · `Shambler` 蹒跚怪 ·
`Ghoul` 食尸鬼 · `Noctol` 夜魇 · `Sightstealer` 隐形魔 · `Revenant` 幽魂 ·
`Nociosphere` 苦痛之球

**虚空结构**：`VoidMonolith` 虚空巨石 · `VoidNode` 虚空节点 ·
`VoidStructure` 虚空结构 · `PitGate` 巨坑 · `CorruptedObelisk` 腐化方尖碑 ·
`TwistedObelisk` 畸变方尖碑 · `Noctolith` 夜光柱

**血肉聚集体**：`FleshmassHeart` 血肉心脏 · `FleshmassNucleus` 血肉核心 ·
`HarbingerTree` 厄兆噬树

**其他现象**：`BloodRain` 血怒之雨 · `Deathpall` 死灵迷雾 ·
`GoldenCube` 金色魔方 · `Metalhorror` 金属怪形 · `UnnaturalCorpse` 异常尸体

🔑 **图鉴是解锁式的**——击杀/发现实体才解锁其图鉴条目。
`EntityCodexEntryDef` 是图鉴记录，实际实体是 `ThingDef`。

### 1.4 实体类别 EntityCategoryDef（3 种）

把 27 个实体归为 3 类（如虚空实体 / 血肉实体 / 其他）。
影响实体图鉴的 UI 分组和部分机制。

### 1.5 玩法风格 AnomalyPlaystyleDef（3 个）

Anomaly 提供 3 种玩法风格（开局选项）：
- 拥抱虚空（激进）
- 干扰虚空（保守）
- 无（经典）

### 1.6 Anomaly 能力 AbilityDef（21 个）

实体能力 + 玩家的虚空能力。与 Royalty 灵能不同——
Anomaly 能力多用**冷却**而非「专注/熵」。

---

## 2. 层级

### 2.1 虚空威胁的进度

```
MonolithLevel（全局巨石等级）
  ├─ Stirring+ ────────▶ 虚空之地出现（TileMutator）
  │                        └─ 虚空生物刷新 → 战斗
  ├─ Gleaming+ ───────▶ 实体变得强/多
  ├─ VoidAwakened ────▶ 高危（可触发战斗失败）
  └─ Embraced/Disrupted ── 玩家选择决定走向
```

🔑 **巨石等级是 Anomaly 的难度曲线**——它随时间/事件上升，
且是存档级的（不重置）。做 Anomaly 平衡 mod 要从这里下手。

### 2.2 虚空感染的危险

```
与实体战斗（尤其是虚空实体）
  └─ 可能被虚空感染（Hediff）
       └─ 感染进度条
            ├─ 杀死感染源（实体/虚空方尖碑）──▶ 解锁感染分支（战斗失败）
            │    └─ 虚空低语者 / 虚空化分支
            └─ 不理会 ──▶ 感染填满 → 强制战斗失败（Void Infection）
```

🔑 **虚空感染是 Anomaly 最凶险的机制**——处理不当直接战斗失败
（lose the colony）。这是 Anomaly 高风险高回报的核心。

### 2.3 实体类别与强度

```
EntityCategory（3 类：虚空/血肉/其他）
  └─ EntityCodexEntryDef（27 个）记录
       └─ 实体 ThingDef（复用）── 强度/能力
```

实体强度随巨石等级提升——所以同一个实体在早期/后期难度完全不同。

---

## 3. 内容

### 3.1 实体全表（27 种）

官方中文标签 27/27 全有。清单见 §1.3。

> 完整 27 行：`python tools/game_defs/extract_defs.py --def EntityCodexEntryDef --dlc Anomaly`

### 3.2 巨石等级全表（7 个）

见 §1.2。⚠️ 这 7 个等级的顶层中文标签为 0（多内部状态名，靠 Keyed 翻译）。

### 3.3 Anomaly 能力全表（21 个）

实体能力 + 玩家能力。

> 复现：`python tools/game_defs/extract_defs.py --def AbilityDef --dlc Anomaly`

---

## 4. 解读

> 本节机制串联自本地 Def + 灰机 wiki。

### 4.1 Anomaly 是「世界级的威胁」

不同于其他 DLC「给玩家加内容」，Anomaly **改变世界本身**：
- 巨石等级是存档级的全局状态
- 虚空之地会随机出现在世界各处
- 感染处理不当直接 lose the colony

所以 Anomaly mod 不能只看「给玩家什么」，要看「世界怎么变」。

### 4.2 拥抱 vs 干扰是核心决策

开局三个选择决定整个存档走向：
- **拥抱虚空**：获得强力（虚空形态/力量），但世界持续敌对
- **干扰虚空**：安全，但放弃强力
- **无**：经典模式，不受影响

这是 Anomaly 的主题——「你愿意为力量付出多少」。

### 4.3 虚空之地的生成机制

虚空之地 = 群系 + TileMutator 组合（见 [mapgen.md](mapgen.md) §1.5）。
87 个 TileMutator 里 Anomaly 占了大部分——虚空变体都靠它们。
做「虚空之地 mod」要从 TileMutator + MapGenerator 入手。

### 4.4 实体战斗的风险

与实体战斗可能感染虚空（见 §2.2）。战斗时应：
- 优先杀死感染源（实体/方尖碑）
- 备好解感染手段
- 否则可能触发战斗失败

这是 Anomaly 战斗的核心决策——**能不能打实体**。

---

## 5. modder 常见坑

1. **巨石等级是存档级的全局状态**——改它要谨慎，它影响整个世界难度且不随地图重置。
2. **实体是 ThingDef，复用 Core 结构**——加实体用 ThingDef + 实体图鉴双定义
   （图鉴 `EntityCodexEntryDef` + 实体本身 `ThingDef`）。
3. **虚空感染处理不当 lose colony**——做「实体战」相关 mod 时，
   别让感染逻辑触发强制战斗失败（除非有意为之）。
4. **虚空之地靠 TileMutator + MapGenerator 组合**——改虚空生成动这两处
   （见 [mapgen.md](mapgen.md)）。
5. **实体强度随巨石等级变化**——平衡要按「等级 + 实体」综合算，不是静态数值。
6. **Anomaly 能力多不是专注/熵机制**（不同于 Royalty 灵能），是冷却式——别照搬灵能逻辑。
7. **Anomaly 是最易崩档的 DLC**——任何影响虚空感染/战斗失败的改动
   都要在测试存档充分验证。
