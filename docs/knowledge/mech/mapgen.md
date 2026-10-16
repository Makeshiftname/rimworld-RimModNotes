# 地图与生物群系（MapGenerator / GenStep / Biome / TileMutator）

> **mech 层**：本文只讲**地图与世界生成系统由什么构成、字段含义、内容清单**。
> 「地形怎么影响建造」见 [build.md](build.md)；
> 「事件怎么按群系调频」见 [quests-events.md](quests-events.md)；
> 「种植/矿物产量」见 [production.md](production.md)。
>
> **数据源与核对日期（2026-10-16）**：
> 本地 `F:\Games\GBL\rimworld\Data\`（RimWorld 1.6，全 6 DLC），中文取官方简中 DefInjected。
> 复现：`python tools/game_defs/extract_defs.py --def MapGeneratorDef GenStepDef BiomeDef TileMutatorDef WorldGenStepDef`。
>
> 规模：**MapGeneratorDef 19 · GenStepDef 167 · BiomeDef 25 · TileMutatorDef 87 · WorldGenStepDef 12**。

---

## 1. 框架

### 1.1 地图生成的流水线

```
MapGeneratorDef（19 种地图生成器）──「用哪种方式生成这张图」
  └─ genSteps（有序步骤列表）── 按顺序执行
       ├─ GenStepDef（167 种生成步骤）── 一个 genStep 做什么
       │    ├─ 放岩石网格 RocksFromGrid
       │    ├─ 放聚落 Settlement
       │    ├─ 放地形 Terrain
       │    ├─ 放树 AnimaTrees / 洞穴 CaveHives
       │    └─ ...
       └─ customMapComponents（额外组件）

BiomeDef（25 种生物群系）──「这里是什么环境」
  └─ 决定：动物密度、植物密度、疾病、温度、能长什么
       └─ TileMutatorDef（87 种）── 「这个地块被变异成什么」
```

🔑 **地图生成 = MapGenerator 选一套 genSteps 逐步执行**。
每个 `genStep` 是一个独立的生成步骤（铺岩石、种树、建聚落…），
按列表顺序跑。`BiomeDef` 则控制这个环境「长什么样」。

### 1.2 地图生成器 MapGeneratorDef（19 个）

| 字段 | 出现率 | 说明 |
|---|---:|---|
| `genSteps` | 95% | **有序生成步骤列表**（每项一个 GenStep） |
| `ParentName` | 68% | 13/19 有父类（`MapCommonBase` `Base_Player` `SpaceMapGenerator`） |
| `isUnderground` | 21% | 是否地下地图 |
| `forceCaves` | 11% | 强制洞穴 |
| `pocketMapProperties.biome` | 32% | **小地图指定群系**（迷宫/金属地狱/地下迷宫） |
| `pocketMapProperties.temperature` | 32% | 小地图温度 |
| `pocketMapProperties.tileMutators` | 11% | 小地图地块变异 |
| `customMapComponents` | 11% | 额外地图组件（`LabyrinthMapComponent` 等） |
| `disableCallAid` | 32% | 禁用救援呼叫 |
| `ignoreAreaRevealedLetter` | 21% | 忽略「区域已揭示」信件 |

🔑 **`genSteps` 的顺序很重要**——先生成地形框架，再放岩石/建筑。
新增生成步骤要放在合适位置（不能放在地形之前）。

**19 个生成器分类**：
- 玩家家 `Base_Player`
- 派系据点 `Base_Faction`
- 遭遇战 `Encounter`
- 小地图 `Labyrinth` 迷宫 · `MetalHell` 金属地狱 · `Undercave` 地下迷宫（Anomaly）
- Odyssey 空间/星际地图（10 个）

### 1.3 生成步骤 GenStepDef（167 个）

**一个 genStep = 地图生成的一个原子步骤**。字段极简：

- `genStepClass` —— C# 类，负责实际生成
- `label` —— 内部标签
- 各种子配置（放多少、什么类型）

🔑 **改地图生成的最细粒度入口**——想在地图上加东西
（比如「自动生成某种遗迹」），就是加一个 `GenStepDef`，
再用 `PatchOperation` 插进某个 `MapGenerator` 的 `genSteps` 列表。

⚠️ **genSteps 用 PatchOperation 插入时要小心顺序**
（见 [../ref/defs-and-patches-ref.md](../ref/defs-and-patches-ref.md)：
`PatchOperationInsert` 插兄弟、`PatchOperationAdd` 插子节点）。

### 1.4 生物群系 BiomeDef（25 个）

🔑 **群系定义「一个环境的性格」**：

| 字段 | 出现率 | 说明 |
|---|---:|---|
| `animalDensity` | 84% | 动物密度（2.8 / 1.1 / 3.3） |
| `plantDensity` | 84% | 植物密度（0.40 / 0.19 / 0.60） |
| `workerClass` | 80% | C# 类：`BiomeWorker_*` |
| `diseaseMtbDays` | 80% | 疾病平均触发间隔（60 天 / 80 天） |
| `diseases` | 80% | 这个群系的**特有疾病** + 权重（`Disease_Flu` / `Malaria` / `OrganDecay`…） |
| `maxFishPopulation` | 76% | 鱼的产量上限 |
| `texture` | 76% | 世界地图上的贴图 |
| `description` | 100% | 官方群系描述 |

🔑 **`diseases` 让群系绑定疾病**——热带 `ColdBog` 沼泽多病、
寒冷地区易有冻伤相关。20/25 个群系都定义了特有疾病。

**25 个群系**（Core 15 · Anomaly 3 · Odyssey 7）：

| 类别 | 群系 |
|---|---|
| **温带** | `TemperateForest` 温带森林 · `TemperateGrassland` |
| **寒带** | `BorealForest` 北方针叶林 · `Tundra` 苔原 · `ColdBog` 冷沼泽 |
| **干旱** | `AridShrubland` 干旱灌木地 · `ExtremeDesert` 极端沙漠 |
| **热带** | `TropicalRainforest` 热带雨林 · `TropicalSavanna` |
| **海洋** | `SeaIce` 海冰 · `Ocean` 海洋 |
| **特殊（Anomaly）** | `Undercave` · `Lichenlands` · `Highlands` |
| **特殊（Odyssey）** | 7 个空间/星际群系 |

### 1.5 地块变异 TileMutatorDef（87 个）

🔑 **在已有群系上「变异」出一个新环境**——
Anomaly 的虚空之地常用这个（在普通群系上叠加虚空变异）。

87 个变异器，按 DLC 分布（Anomaly/其他地方），
通过 `pocketMapProperties.tileMutators` 或地图生成时应用。

### 1.6 世界生成步骤 WorldGenStepDef（12 个）

**世界地图（星球视图）**的生成步骤——定义整个星球如何生成
（大陆分布、河流、道路、生物群系布局）。12 个步骤。

---

## 2. 层级

### 2.1 地图生成的执行顺序

```
选一个 MapGeneratorDef（玩家家/据点/遭遇/小地图）
  └─ 按 genSteps 顺序跑每个 GenStepDef
       ├─ 早期：地形框架、RocksFromGrid（岩石网格）
       ├─ 中期：Terrain（地面类型）、植被
       ├─ 后期：Settlement（聚落建筑）、特殊物件
       └─ 每个步骤可读 BiomeDef（当前群系）决定参数
```

🔑 **顺序敏感**——岩石网格要在聚落之前、地形要在植被之前。
用 PatchOperation 插入 genStep 时要选对位置。

### 2.2 群系 → 地块 → 地图

```
BiomeDef（25 种）── 环境性格
  └─ TileMutatorDef（87 种）── 在群系上叠加变异
       └─ MapGeneratorDef（19 种）── 用某套 genSteps 生成具体地图
            └─ GenStepDef（167 种）── 每个生成步骤
```

🔑 **三层叠加**：群系（基础性格）+ 变异（叠加特性）→ 生成具体地图。
Anomaly 的虚空之地 = 群系 + TileMutator 组合出来的。

### 2.3 群系对系统的影响

```
BiomeDef（群系）
  ├─ diseases ────────▶ 疾病事件频率（hediff.md / quests-events.md）
  ├─ animalDensity ───▶ 遭遇战难度（quests-events.md）
  ├─ plantDensity ────▶ 可收获植物量（production.md）
  ├─ mtbDaysByBiome ──▶ 事件频率（quests-events.md）
  └─ 温度 ────────────▶ 作物生长、建造（build.md）
```

🔑 **群系是多系统的输入**——一个 BiomeDef 同时影响疾病、战斗、资源、事件。

### 2.4 世界生成 vs 地图生成

```
WorldGenStepDef（12 个）── 生成整个星球（世界地图层）
  └─ 决定：哪里是大陆、哪里有河、生物群系如何布局
       └─ 玩家点击某地块
            └─ MapGeneratorDef（19 种）── 生成该地块的具体地图
                 └─ GenStepDef（167 种）── 具体生成步骤
```

🔑 **世界生成（星球）→ 地图生成（每张图）是两层**——
做「世界地图 mod」动 WorldGenStep，做「地图 mod」动 MapGenerator/GenStep。

---

## 3. 内容

### 3.1 地图生成器全表（19 个）

规模：**Odyssey 10 · Core 4 · Anomaly 3 · Ideology 2**。
官方中文标签 13/19（差额是多参数生成器的 base 类）。

按用途（代表性）：

| 类型 | 生成器 |
|---|---|
| 玩家家 | `Base_Player` |
| 派系据点 | `Base_Faction` |
| 遭遇战 | `Encounter` |
| 小地图（Anomaly） | `Labyrinth` 迷宫 · `MetalHell` 金属地狱 · `Undercave` 地下迷宫 |
| 空间（Odyssey） | 10 个 |

> 完整 19 行：`python tools/game_defs/extract_defs.py --def MapGeneratorDef`

### 3.2 生物群系全表（25 种）

Core 15 · Anomaly 3 · Odyssey 7。官方中文标签 25/25 全有。清单见 §1.4。

> 完整 25 行：`python tools/game_defs/extract_defs.py --def BiomeDef`
> 字段级（每群系的疾病/密度）：
> `python tools/game_defs/extract_defs.py --def BiomeDef --fields animalDensity plantDensity diseaseMtbDays`

### 3.3 生成步骤全表（167 个）

按功能分组（代表性）：

| 功能 | genStep |
|---|---|
| **岩石地形** | `RocksFromGrid` · `RocksFromGrid_NoMinerals` |
| **聚落建筑** | `Settlement` · `SettlementPower` |
| **地面类型** | `Terrain` |
| **植被/树** | `AnimaTrees` · `PoluxTrees` |
| **洞穴** | `CaveHives` |
| **特殊结构** | `EscapeShip` 逃生舱 · `ScatterShrines` |

> 完整 167 行：`python tools/game_defs/extract_defs.py --def GenStepDef`

---

## 4. 解读

### 4.1 genSteps 是地图 mod 的唯一入口

想在 RimWorld 地图上加/改任何生成内容（建筑、植被、矿物、陷阱），
唯一正规途径就是**加一个 `GenStepDef` 并插进某个 `MapGeneratorDef` 的 `genSteps`**。

这是 mod「地图增强」的标准做法，也是 [../ref/defs-and-patches-ref.md](../ref/defs-and-patches-ref.md)
里 PatchOperation 的典型用例。

### 4.2 genSteps 顺序敏感

插入 genStep 时位置很重要：
- 放在地形生成**之前** → 你放的东西可能被地形覆盖
- 放在聚落生成**之后** → 你的东西会和建筑重叠

通常「后加特殊物件」要放在列表靠后。这是地图 mod 最常见的翻车点。

### 4.3 群系是难度调节的隐性旋钮

`animalDensity` / `plantDensity` / `diseases` 让同一个地图生成器
在不同群系下体验差异巨大：
- 冷沼泽（`ColdBog`）：动物多、疾病多、沼泽地难建
- 极端沙漠：植物少、温度高、不能种大部分作物

做「难度/平衡 mod」时，群系参数是比全局数值更精细的调节方式。

### 4.4 Anomaly 的虚空之地 = 群系 + TileMutator

虚空之地不是独立群系，而是**普通群系 + TileMutator 叠加**——
这是 87 个 TileMutator 存在的意义。做「自定义地块环境」用这套组合。

### 4.5 Odyssey 大幅扩充了地图生成

19 个生成器里 Odyssey 占 10 个——DLC 加入了大量空间/星际地图生成器。
做地图 mod 若涉及太空/星际，注意 Odyssey 的生成器结构与 Core 不同。

---

## 5. modder 常见坑

1. **genSteps 顺序敏感**——插入位置错了东西会被覆盖或和建筑重叠，
   这是地图 mod 最常见的失败。
2. **改 genSteps 用 PatchOperation 要选对操作**——
   改列表内容（插入步骤）用 `PatchOperationInsert`（插兄弟），
   见 [../ref/defs-and-patches-ref.md](../ref/defs-and-patches-ref.md)。
3. **MapGeneratorDef 13/19 有父类**——改 `MapCommonBase` 影响所有地图。
4. **小地图（迷宫等）是独立生成器**——不是「换个 biome」，
   有自己的 `pocketMapProperties` 和 `customMapComponents`。
5. **群系的 `diseases` 直接绑疾病**——想改某地区的疾病看这里，
   别去改疾病本身的 Def（那影响所有地区）。
6. **`workerClass` 是真正执行生成/群系逻辑的 C#**——
   `BiomeWorker_*` / `MapGenStep_*` 类，Def 只是配置。
7. **Odyssey 的生成器占一半**——涉及太空地图时结构不同于 Core。
