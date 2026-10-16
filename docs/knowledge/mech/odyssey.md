# 星舰与重力（Odyssey DLC）

> **mech 层**：本文讲 **Odyssey DLC 独有的系统**——逆重力飞船（Gravship）、
> 星际航行、行星层。**复用 Core 的部分**指回其他文档。
>
> **数据源与核对日期（2026-10-16）**：
> 本地 `F:\Games\GBL\rimworld\Data\Odyssey\`，RimWorld 1.6。中文取官方简中 DefInjected。
> 复现：`python tools/game_defs/extract_defs.py --def ShipJobDef GravshipComponentTypeDef TransportShipDef PlanetLayerDef PlanetLayerSettingsDef --dlc Odyssey`
>
> 规模：**GravshipComponentTypeDef 5 · ShipJobDef 7 · TransportShipDef 4 ·
> PlanetLayerDef 2 · PlanetLayerSettingsDef 2**。
> ⚠️ Odyssey 是**最新的 DLC**（1.6 末/1.7 前），且大量内容复用 Core 机制
> （如 10 个 MapGenerator 复用地图生成，见 [mapgen.md](mapgen.md) §1.2）。
> 官方样例见 [../ref/version-migration.md](../ref/version-migration.md) 的 DLC 说明。

---

## 1. 框架

### 1.1 四大子系统

```
逆重力飞船 Gravship ──── Odyssey 的核心：可建造、可飞行的移动基地
  ├─ GravshipComponentTypeDef（5 种部件）── 飞船的 5 类组件
  │    ├─ Thruster 推进器
  │    ├─ Controls 控制台
  │    ├─ FuelStorage 燃料箱
  │    ├─ PowerCell 能量电池
  │    └─ SignalJammer 信号干扰器
  ├─ ShipJobDef（7 种）── 飞船的「工作」（建造/修理/起飞/…）
  └─ TransportShipDef（4 种）── 运输船（ shuttles / 穿梭机）

星际航行 Interstellar ──── 行星之间移动
  ├─ PlanetLayerDef（2 种）── 行星的图层（大气/地表/地下）
  ├─ PlanetLayerSettingsDef ── 图层设置
  └─ 配合 MapGenerator（Odyssey 10 个生成器）

蓝图为枢纽 ──── 逆重飞船部件蓝图来自 Royalty 的蓝图系统
  └─ Favor（帝国好感）──▶ 蓝图 ──▶ 逆重飞船（见 royalty.md §4.4）
```

🔑 **逆重飞船 = 「可飞的移动殖民地」**——它把建造、飞行、
战斗整合在一起，是 Odyssey 的核心玩法。

### 1.2 飞船部件 GravshipComponentTypeDef（5 个）

飞船由 5 类组件构成（**都是 ThingDef 建筑**，定义在
`Buildings_Gravship.xml`）：

| defName | 中文 | 作用 |
|---|---|---|
| `Thruster` | 推进器 | 提供升空/飞行能力 |
| `Controls` | 控制台 | 操控飞船 |
| `FuelStorage` | 燃料箱 | 储存推进燃料 |
| `PowerCell` | 能量电池 | 供能 |
| `SignalJammer` | 信号干扰器 | 特殊（干扰信号） |

🔑 **飞船是「拼装」的**——玩家用这些部件在太空建飞船，
组件组合决定飞船性能。这套部件用 `ThingDef` 建筑定义
（复用 Core 建造机制，见 [build.md](build.md)）。

### 1.3 飞船工作 ShipJobDef（7 种）

飞船专属的「工作」——类似普通 Job 但在飞船上执行：

- 建造/修理飞船组件
- 起飞/降落
- 星际航行
- 装卸货物

🔑 **ShipJob 是独立于普通 Work 的系统**（7 种，对比 Core 的 320 种 Job）。
飞船有自己的一套决策（见 [work.md](work.md) 的决策链，这里是飞船版）。

> 复现：`python tools/game_defs/extract_defs.py --def ShipJobDef --dlc Odyssey`

### 1.4 运输船 TransportShipDef（4 种）

运输船（shuttle）——接送访客/交易/任务的空中交通工具。
4 种定义不同用途的运输船。

### 1.5 行星层 PlanetLayerDef / PlanetLayerSettingsDef（各 2 个）

🔑 **行星分层**——Odyssey 把行星分为若干「层」（大气层/地表/地下等），
每个层有独立的地图生成规则（`PlanetLayerSettingsDef`）。

配合 `MapGeneratorDef`——Odyssey 提供了 **10 个地图生成器**（见 [mapgen.md](mapgen.md) §1.2），
用于生成不同行星层的地图。

### 1.6 逆重飞船与蓝图（跨 DLC）

🔑 **逆重飞船的部件需要蓝图**——蓝图来自 **Royalty 的 Favor 换蓝图**机制
（见 [royalty.md](royalty.md) §4.4）。

所以 Odyssey 的建造玩法实际是跨 DLC 的：
```
完成 Royalty 委托 → Favor → 换蓝图（含 Odyssey 星舰部件）
  └─▶ 建造逆重飞船 → 星际航行
```

---

## 2. 层级

### 2.1 飞船的建造-飞行链

```
蓝图（Royalty Favor 换）
  └─ 用部件（ThingDef 建筑）在太空/地面建飞船
       ├─ Thruster 推进器（必需）
       ├─ Controls 控制台（必需）
       ├─ FuelStorage 燃料箱
       └─ PowerCell 能量电池
            └─ 启动 ShipJob（起飞）
                 └─ 星际航行（PlanetLayer 切换）
```

🔑 **飞船 = 玩家建造的移动基地**——部件组合决定性能，
蓝图决定能否造。这是 Odyssey 的核心循环。

### 2.2 行星层与地图生成

```
PlanetLayer（行星层：大气/地表/地下）
  └─ PlanetLayerSettingsDef（层的设置：生物群系/生成规则）
       └─ MapGeneratorDef（Odyssey 10 个）── 生成该层的地图
            └─ GenStepDef（复用 Core 167 个，见 mapgen.md）
```

🔑 **行星层 → 地图生成器 → genSteps**——
Odyssey 只是加了一层「在哪个层」，具体地图生成仍复用 Core。

---

## 3. 内容

### 3.1 飞船部件全表（5 种）

见 §1.2。官方中文标签 5/5 全有。

### 3.2 飞船工作全表（7 种）

建造/修理/飞行/货物等工作。

### 3.3 行星层全表（2 种）

大气/地表/地下等层。

---

## 4. 解读

> 本节机制串联自本地 Def + 官方资料。

### 4.1 Odyssey 把「基地」变成可移动的

RimWorld 的殖民地通常是**固定在一个地方**。Odyssey 的逆重飞船
把殖民地变成**可建造、可飞行的移动基地**——这改变了核心玩法循环
（选址→建造→防守 变成 建造飞船→到处飞）。

### 4.2 蓝图是 Odyssey 的「门槛」

逆重飞船部件需要蓝图，蓝图来自 Royalty 的 Favor。
所以想玩 Odyssey 的飞船玩法，**必须玩 Royalty 的帝国委托攒 Favor**——
这是 DLC 间的强耦合。

### 4.3 Odyssey 复用而非重造

Odyssey 的地图生成（10 个 MapGenerator）、genSteps（复用 Core 167 个）
说明它**扩展而非重造** Core 的地图系统。做 Odyssey 地图 mod 时，
要理解它是在 Core genSteps 之上的编排（见 [mapgen.md](mapgen.md)）。

### 4.4 这是 1.7 迁移的重点关注 DLC

Odyssey 是最新 DLC，涉及新的建筑类别（飞船部件）和地图生成器，
**1.7 Waylith 迁移时重点检查这里**（见
[../ref/version-migration.md](../ref/version-migration.md)）。

---

## 5. modder 常见坑

1. **飞船部件是 ThingDef 建筑**——复用 Core 建造/工作台机制，
   别当特殊类型（见 [build.md](build.md)）。
2. **飞船部件需要蓝图**——调试时用 GM 模式跳过，否则造不出来。
3. **ShipJob 是独立系统**（7 种）——别用普通 WorkGiver 逻辑套飞船工作。
4. **行星层切换要改 MapGenerator**——Odyssey 的 10 个生成器不同于 Core 4 个
   （见 [mapgen.md](mapgen.md)）。
5. **蓝图跨 DLC**——Odyssey 飞船依赖 Royalty 的 Favor 机制，
   单独装 Odyssey 可能拿不到部件（除非 GM）。
6. **genSteps 复用 Core**——加自定义生成内容对 Odyssey 同样生效
   （它用 Core 的 genSteps）。
