# RimWorld Modding 中英术语对照

> 参考手册层。rimworldwiki 全英文，而本仓库笔记全中文，没有对照表很难检索。
> 术语释义以官方口径为准；具体机制见对应文档。
>
> 最后核对：2026-10-15。

---

## 1. 工程与加载

| 英文 | 中文 | 说明 |
|---|---|---|
| `packageId` | 包 ID | mod 的全局唯一内部标识。**大小写不敏感**；用于依赖、加载顺序、`MayRequire` |
| `About.xml` | — | mod 的身份证：名称、作者、描述、依赖、加载顺序 |
| `ModMetaData` | mod 元数据 | `About.xml` 的根标签 |
| `supportedVersions` | 支持版本 | 声明支持的 RimWorld 版本列表 |
| `IgnoreIfNoMatchingField` | 忽略未知字段 | ⚠️ **老版本游戏必须加**，否则遇到新标签直接报错 |
| `loadBefore` / `loadAfter` | 加载前 / 加载后 | 顺序不对只**警告** |
| `forceLoadBefore` / `forceLoadAfter` | 强制加载前 / 后 | 顺序不对**拒绝加载**。⚠️ RimSort / RimPy 可能不认 |
| `incompatibleWith` | 互斥 | 根本不兼容才用，不要拿来标「有 bug 冲突」 |
| `modDependencies` | mod 依赖 | ⚠️ **Steam Workshop 不自动使用** |
| `alternativePackageIds` | 替代包 ID | 1.6+，一个依赖可由多个 packageId 满足 |
| `LoadFolders.xml` | 加载目录清单 | 精确控制各版本加载哪些目录；**覆盖默认版本目录规则** |
| `Common/` | 公共目录 | 所有版本共享的内容 |
| versioned folder | 版本目录 | `1.4/`、`1.6/` 等，游戏加载第一个兼容的 |
| `Manifest.xml` | — | **已废弃**，旧版 Fluffy Mod Manager 的遗留物 |
| `AssetBundles/` | 资源包目录 | 预打包的 Unity 资源 |
| `PublishedFileId.txt` | 工坊 ID 文件 | 记录 Steam Workshop 项目 ID |

---

## 2. Def 系统

| 英文 | 中文 | 说明 |
|---|---|---|
| **Def** / Definition | 定义 | 游戏内容的声明式描述，全部来自 XML。**数据在 XML，行为在 C#** |
| `defName` | 定义名 | Def 的唯一 ID，**全局唯一**。强烈建议加 mod 前缀 |
| `ThingDef` | 物件定义 | 物品、建筑、武器、投射物、植物……几乎一切的载体 |
| `ParentName` | 父级名 | 继承另一个 Def 的字段 |
| `Abstract="True"` | 抽象基类 | 不会被单独加载，只供继承 |
| `Inherit="False"` | 不继承 | 只覆盖这一个 Def 的继承值，不影响其他继承者 |
| abstract base Def | 抽象基类 Def | 同 `Abstract="True"` |
| inherited tag | 继承标签 | 从父类继承来的字段。⚠️ **补丁发生在继承展开之前，所以定位不到继承标签** |
| `modExtensions` / `DefModExtension` | Mod 扩展 | 给 Def 加自定义字段，见 `PatchOperationAddModExtension` |
| `DefOf` | 定义引用类 | C# 里的「承诺」类，用变量名匹配 `defName` 拿到 Def 引用 |
| `DefDatabase<T>` | 定义数据库 | 运行时按类型查 Def |
| `statBases` | 基础属性 | 用「标签名=defName，值=数值」的简写形式定义 StatDef |
| special parsed list | 特殊解析列表 | 如 `statBases`、物品数量表。有自己的一套解析规则 |
| cross reference | 交叉引用 | Def 之间互相按 `defName` 引用 |

---

## 3. 补丁

| 英文 | 中文 | 说明 |
|---|---|---|
| **PatchOperation** | 补丁操作 | 不覆盖整个 Def 就地修改内容的机制 |
| `PatchOperationAdd` | 添加 | 把 value 作为**子节点**插入目标。默认 `Append` |
| `PatchOperationInsert` | 插入 | 把 value 作为**兄弟节点**插入。默认 `Prepend` |
| `PatchOperationRemove` | 删除 | 删除目标节点 |
| `PatchOperationReplace` | 替换 | 用 value 替换目标节点 |
| `PatchOperationAttributeAdd/Set/Remove` | 属性 添加/设置/删除 | Add 只在属性不存在时加；Set 存在则覆盖 |
| `PatchOperationSequence` | 操作序列 | 一组顺序操作，任一失败即中止。⚠️ **wiki 不推荐用它做条件控制，会掩盖错误** |
| `PatchOperationAddModExtension` | 添加 Mod 扩展 | |
| `PatchOperationSetName` | 设置节点名 | 用于改「字典式」节点的键名 |
| `PatchOperationFindMod` | 查找 mod | ⚠️ **唯一匹配 mod 名字而非 packageId 的机制**；DLC 用单词（Royalty/Ideology/…） |
| `PatchOperationConditional` | 条件补丁 | 检测节点是否存在，分 `match` / `nomatch` 执行 |
| `PatchOperationTest` | 测试补丁 | **已废弃**，被 `Conditional` 取代 |
| `<success>` | 成功标记 | **已废弃**。`Always` 会掩盖真实错误 |
| XPath | — | ⚠️ 匹配的是**解析后的 XML 结构**，与磁盘路径无关。**大小写敏感** |
| predicate | 谓词 | 方括号里的条件，如 `[defName="Wall"]` |
| `MayRequire` | 需要 | XML 属性。逗号分隔 packageId，**全部命中**才加载 |
| `MayRequireAnyOf` | 任一需要 | **任一命中**即加载 |
| `PatchOperationFindMod` 的 match / nomatch | 命中 / 未命中分支 | |

---

## 4. 内容类型

| 英文 | 中文 | 说明 |
|---|---|---|
| **Pawn** | 角色 | ⭐ **游戏代码术语**：任何**使用寻路的可移动实体**。含人、机械体、动物、部分 DLC 实体 |
| Thing | 物件 | 世界中的实体总称。Pawn 是 Thing 的子类 |
| ThingMaker.MakeThing | 物件工厂 | 静态搜索找不到的反射工厂方法之一 |
| PawnKindDef | 角色种类定义 | 「生成什么样的 pawn」 |
| PawnJob | 岗位 | 见 §5 |
| Hediff | 健康差异 | ⭐ 疾病、中毒、植入物、药物效果……**不是「健康」的意思** |
| HediffDef | 健康差异定义 | |
| `Hediff_High` | 兴奋剂类效果 | |
| stuff | 材料 | 物品的材质类别（Metallic / Woody / 织物…） |
| apparel tag | 服装标签 | 控制着装生成的**任意字符串**，不是 Def，无需预先声明 |
| apparel | 服装 | |
| projectile | 投射物 | |
| `verbs` | 攻击行为列表 | 远程武器的攻击定义 |
| `tools` | 近战攻击列表 | 近战武器的攻击定义 |
| `capacities` | 攻击方式 | `Stab` / `Cut` / `Blunt` / `Poke` / `Demolish` |
| `weaponClasses` | 武器类别 | 如 `RangedLight` |
| recipe | 配方 / 制作 | |
| `RecipeDef` | 配方定义 | |
| `costList` | 成本清单 | 制作所需物品 |
| `costStuffCount` | 材料消耗量 | 用 stuff 计的量 |
| `statBases` | 基础属性 | `WorkToMake`、`Mass`、`MarketValue` 等 |
| `equippedStatOffsets` | 装备属性偏移 | 装备时给持有者加的属性 |
| `graphicData` / `texPath` | 图形数据 / 贴图路径 | |
| `graphicClass` | 图形类 | `Graphic_Single`（单张）/ `Graphic_Random`（文件夹随机）/ `Graphic_StackCount`（按堆叠数换图） |
| `shaderType` | 着色器类型 | `Cutout`、`CutoutComplex`、`TransparentPlant` |
| `ParentName` | 父级名 | |

---

## 5. 行为系统

| 英文 | 中文 | 说明 |
|---|---|---|
| **Job** | 任务 | ⭐ **一个 pawn 永远有 Job——倒地、已卸载、甚至死亡时也有** |
| JobDef | 任务定义 | XML 声明任务数据与用哪个 JobDriver |
| **JobDriver** | 任务驱动 | C# worker 类：怎么完成、何时中止、能否启动 |
| **Toil** | 子步骤 | Job 的单个子任务。⚠️ **一次性全部创建，不是轮到才创建** |
| `TryMakePreToilReservations` | 预约 | Job 开始前预约目标。**返回 false 则 Job 启动前中止** |
| `MakeNewToils` | 生成子步骤 | 覆写它来定义 Job 的行为 |
| `reserve` | 预约 | 独占标记，防止他人抢同一个目标 |
| `JobTag` | 任务标签 | 如 `JobTag.Misc` |
| `FloatMenuOptionProvider` | 浮动菜单选项提供者 | ⭐ **1.6 新增**。右键菜单项的独立扩展点 |
| `FloatMenuContext` | 浮动菜单上下文 | 含 `FirstSelectedPawn` |
| prioritized task | 优先任务 | 玩家直接下达、压过正常工作分配的任务 |
| `StaticConstructorOnStartup` | 启动时静态构造 | ⭐ mod 做初始化的**标准位置**（启动第 36 步） |
| GameComponent / WorldComponent / MapComponent | — | 每 tick / 每世界 / 每地图运行的组件 |
| Lord | 群体 AI 控制器 | 从商队到袭击队都是 Lord |
| Toil toil 定位 | — | `initAction` / `tickAction` / `FailOnCannotTouch` / `defaultCompleteMode` / `handlingFacing` |

---

## 6. 资产与渲染

| 英文 | 中文 | 说明 |
|---|---|---|
| texture | 贴图 | |
| texture mask | 贴图遮罩 | ⭐ **黑=不上色，纯红=主色，深红=按比例（`#BB0000`≈75%），绿=第二色通道** |
| linked atlas | 关联图集 | 墙壁/导管用的 16 子贴图精灵图 |
| `maxMeshCount` | 最大网格数 | ⭐ 植物贴图份数，**只能是 ≤25 的完全平方数（1/4/9/16/25）** |
| `visualSizeRange` | 视觉尺寸范围 | `min~max`，对应 0% / 100% 生长 |
| `topWindExposure` | 顶部风力暴露 | 默认 0.25，0–1。通过顶点偏移实现，**不是简单摇摆而是拉伸** |
| `harvestAfterGrowth` | 成熟阈值 | 生长% 超过它才算成熟 |
| sound event | 声音事件 | **硬编码在游戏代码里** |
| `SoundDef` | 声音定义 | 规定事件触发时播什么 |
| grain | 音频粒 | `SoundDef` 里的一条采样或静音 |
| `volumeRange` / `pitchRange` | 音量 / 音调区间 | 每次播放在区间内**随机**。音调随机能显著减少重复感 |
| asset bundle | 资源包 | 预打包的 Unity 资源 |
| DDS | — | 已是 GPU 格式的贴图，**磁盘体积最大**但零加载开销 |
| GLSL | — | ⭐ **1.7 的新 shader 必须用 GLSL 写** |
| VRAM | 显存 | 贴图过大填满 VRAM 时**游戏直接崩** |

---

## 7. 版本与引擎

| 英文 | 中文 | 说明 |
|---|---|---|
| **Waylith** | — | ⭐ **1.7 的自研引擎**，取代 Unity。靠 shim 翻译旧的 Unity 调用 |
| Unity | — | 1.6 及更早的引擎（2022.3.35） |
| `UnityStubs.dll` | Unity 桩 | 1.7 用的兼容层 |
| net472 / net481 / net10.0 | 目标框架 | ⭐ **1.7 起是 `net10.0`（`.0` 不能省）** |
| SDK-style csproj | SDK 风格工程文件 | ⭐ 1.7 **必须**迁移，旧格式不能用 |
| Harmony 2 / 3 | — | ⭐ **1.7 强制 Harmony 3.0**。Harmony 3 对 `__instance` / `__result` **强制类型检查** |
| Prepatcher | — | ⭐ **1.7 下失效**，因为它本身还没适配 |
| Doorstop | — | ⭐ **1.7 起不再需要**，IDE 可直接附加进程 |
| Frame Profiler | 帧分析器 | 1.7 新增的开发者模式工具 |
| `-quicktest` | 快速测试参数 | 启动直接进 Crashlanded |
| `-mod=` | — | ⭐ **1.7 新增**。指定 mod id 列表启动 |
| `-savedatafolder=` | 存档目录参数 | 覆盖存档位置 |
| `autostart.rws` | 自动载入存档 | 放这个文件启动即载入 |
| Development Mode | 开发模式 | Options → Gameplay。会一直保持 |
| Quicktest | 快速测试 | dev mode 下主菜单的选项 |

---

## 8. DLC

| 英文 | 中文 | packageId / label |
|---|---|---|
| Core | 核心 | 无 |
| Royalty | 帝权 | `Ludeon.RimWorld.Royalty` |
| Ideology | 信仰 | `Ludeon.RimWorld.Ideology` |
| Biotech | 生物技术 | `Ludeon.RimWorld.Biotech` |
| Anomaly | 异常 | `Ludeon.RimWorld.Anomaly` |
| Odyssey | 奥德赛 | `Ludeon.RimWorld.Odyssey` |

> ⚠️ **两套标识不能混用**：
> `MayRequire` / `LoadFolders` 用**完整 packageId**；
> `PatchOperationFindMod` 用**单词 label**（`Royalty` 而非 `Ludeon.RimWorld.Royalty`）。

---

## 9. 中文本地社区常见对应

> 灰机 wiki 是中文 MediaWiki，但**没有直接对应英文「Pawn」的词条**——
> 它用「生物」组织，语义更接近「遇到的生物/角色类型」集合，
> **不含** Entities（Anomaly）、Drones（Odyssey）等 DLC 新类型。
> 查 DLC 内容一律以英文页或游戏 Defs 为准。

| 英文 | 灰机词条 |
|---|---|
| Pawn | 生物 |
| Colonist | 殖民者 |
| Prisoner | 囚犯 |
| Raider | 袭击者（页内亦称掠夺者） |
| Visitor | 访客（见 Factions） |
| Drifter | 流浪者 |
| Mechanoid | 机械体 |
| Character Properties | 角色属性 |
| Modding Tutorials | MOD 教程 |
