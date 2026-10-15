# 资产与渲染参考：贴图 / 植物渲染 / 音效 / Asset Bundle

> 参考手册层。蒸馏自 rimworldwiki `Modding Tutorials` vetted 区，**不含仓库特定结论**。
>
> 来源页（最后核对 2026-10-15）：
> [Textures](https://rimworldwiki.com/wiki/Modding_Tutorials/Textures) ·
> [Plant Rendering](https://rimworldwiki.com/wiki/Modding_Tutorials/Plant_Rendering) ·
> [Sounds](https://rimworldwiki.com/wiki/Modding_Tutorials/Sounds) ·
> [Asset Bundles](https://rimworldwiki.com/wiki/Modding_Tutorials/Asset_Bundles)
>
> 所属 wiki 章节：Game Systems Guides + Performance and Optimization
>
> ⚠️ **页面成熟度差异很大**：`Sounds` 标了「Heavily outdated」，
> `Textures` / `Plant Rendering` / `Asset Bundles` 标了 Under Review。
> 下面已按各自实际可信度标注。

---

## 1. 贴图（Textures）

### 1.1 格式与分辨率

- 常用透明 PNG。也支持 **JPG、PSD、DDS**。
- **贴图分辨率与游戏内绘制尺寸无关**，按屏幕绘制尺寸定：
  - 原版多为 **64 px/格**；
  - 较新 DLC 贴图和多数 mod 作者以 **128 px/格**为基准；
  - 256 px/格及以上**一般算奢侈**，高细节还会和 RimWorld 画风打架；
  - 过大的贴图吃 VRAM，**VRAM 填满时游戏直接崩**。
- 尺寸应为 **4 的倍数**，理想是 **2 的幂**（32×32 / 64×64 / 128×128 / 256×256）。
- **画风**：可交互物件（物品、建筑）要有粗黑描边，约 **每 64 px 格 2 px 粗**；
  **植物用绿色描边而非黑色**。
- Ludeon 公开了 [Official RimWorld Art Source](https://www.dropbox.com/sh/mz6zjq3f1d654f3/AACX_OMdBG_J78hF2Ph-HzM3a?e=1&dl=0)，
  含几乎全部原版与 DLC 贴图，**强烈建议照着画**。

### 1.2 路径与冲突

- 路径**从 `Textures/` 开始**：`Textures/MyMod/MyTexture.png` → 引用写 `MyMod/MyTexture`。
- 所有 mod 的贴图进**同一个内容查找空间**，同路径后者覆盖前者。
  重贴图 mod 是故意利用这条；否则应给目录或文件加唯一前缀。

### 1.3 Texture Mask（材质遮罩）

可染色（stuff）/可涂装（paintable）的物件用遮罩决定哪部分上色：

| 颜色 | 含义 |
|---|---|
| 纯黑 | 不上色 |
| 100% 红 | 用主色绘制 |
| 深红 | 按比例混入主色，`#BB0000` ≈ 75% 强度 |
| 绿 | **第二色通道**（少数贴图用，如床的床位色：殖民者/医疗/囚犯等）。红绿可混 |

示例：近战武器 Basic Melee Weapon 教程里用遮罩做到「只有刀刃上材质色，握柄保持原色」。

### 1.4 分类型的绘制约定

| 类型 | 约定 |
|---|---|
| **近战武器** | **尖端朝右**画（pawn 朝西时贴图会水平镜像）。用 `graphicData` 的 `<equippedAngleOffset>` 调角度；原版多数为 **-65°**，`ikwa` 为 -25°，thrumbo horn / elephant tusk 为 -20° |
| **远程武器** | 同样**朝右瞄准**；额外要求**枪管/弹丸生成点在贴图内垂直居中**，否则弹道对不上 |
| **投射物** | **朝上**画，贴图中心对准弹丸在游戏内的精确坐标。⚠️ **投射物不像武器那样在朝西时翻转** |
| **头部服装** | 至少 **3 张**（south / north / east）。west 可选——不给时游戏镜像 east。不对称或想让 west 不同才需要补。推荐 **3+1** 张；带遮罩则 **6+2** |
| **身体服装** | 5 种体型（male / female / thin / hulk / fat）× 3 朝向 = **15+1** 张；带遮罩 **30+2** |
| **工具背包**（如毒气罐） | 可用单张贴图 + 按体型偏移。**仅限 utility 槽**，张数同头部服装（3+1 / 带遮罩 6+2） |
| **建筑** | 画布按 `drawSize` **精确**开，并把贴图在画布里居中，省得再调 draw offset |
| **墙壁 / 导管** | 特殊图形类型 **linked atlas**：一张精灵图里含 **16 个子贴图**，对应所有可能的连接组合 |
| **地形** | 单张贴图铺在 **16×16** 格区域上，必须**四向可平铺** |

### 1.5 白边问题

贴图默认会被压缩，而很多绘图软件会**丢弃全透明像素里的颜色数据**，导致边缘出现白边。
两种解法：① 让绘图软件在透明像素里保留颜色数据（最好是黑色）；② 在常规描边外再加 1% 不透明度的黑色描边。
（用 asset bundle 可以关掉压缩来规避。）

---

## 2. 植物渲染（Plant Rendering）

RimWorld 画一株植物时依次做：按生长百分比算尺寸与中心 → 决定每个格子上画几份贴图 →
在 x/z 轴上带随机偏移铺开 → 用 mesh 偏移模拟风吹 → 按基准位置加阴影（如定义了）。
**这些全都可以用植物 ThingDef 控制。**

### 2.1 `maxMeshCount`（贴图份数）

| 值 | 布局 |
|---|---|
| 1 | 单张居中，无网格 |
| 4 | 2×2 |
| 9 | 3×3 |
| 16 | 4×4 |
| 25 | 5×5 |

- **只接受 ≤25 的完全平方数**，填别的值或 >25 **启动即报错**。
- 植物长到 100% 才画满份数，之前按比例少画。
- 每份贴图有概率沿 x 轴翻转；用 `Graphic_Random` 时每份还会随机选不同贴图。

### 2.2 视觉尺寸缩放

由 `drawSize`（植物默认 **1.0**）与 `<plant>` 下的 `visualSizeRange` 的
`min`（0% 生长时）/`max`（100% 生长时）共同决定。

- 比例关系是 **`visualSizeRange × drawSize` 对当前生长百分比**。
- `drawSize`=1.0、`max`=3.0 时，100% 生长的树占 **3×3** 格；
  若 `drawSize`=2.0，则占 **6×6** 格。
- **定位基准随 `maxMeshCount` 变**：
  - `maxMeshCount = 1`（树、大型植物）→ 贴图始终相对所占格的**底部**定位；
  - `maxMeshCount > 1`（草等小植物）→ 整个 mesh 及每份贴图相对格的**中心**缩放定位。
- 阴影同样受此影响。

### 2.3 贴图与状态切换

默认贴图走 `graphicData`：

```xml
<graphicData>
  <texPath>Things/Plant/BerryPlant</texPath>
  <graphicClass>Graphic_Random</graphicClass>
</graphicData>
```

⚠️ 用 `Graphic_Random` 时，**`texPath` 要写贴图所在文件夹名，不是文件名**。
变体命名：`Dandelion.png` / `DandelionA.png` / `DandelionB.png`。

状态贴图全部写在 `<plant>` 下：

| 字段 | 生效条件 |
|---|---|
| `immatureGraphicPath` | 生长% ≤ `harvestAfterGrowth` |
| `leaflessGraphicPath` | 不可播种/收获、应落叶时 |
| `pollutedGraphicPath` | 所在格被污染（需 Biotech） |
| `snowOverlayGraphic` | **1.6+**，该格积雪 > 80% |
| `immatureSnowOverlayGraphic` / `leaflessSnowOverlayGraphic` | **1.6+** |

状态变体的数量**不必**和默认贴图一致——5 张默认 + 各 1 张状态完全没问题。
它们与默认贴图共用同一 `graphicClass`、`shaderType`、`drawSize`、`color`、`colorTwo`。

`harvestAfterGrowth` 是 1.0 制的（如 `0.30`）：生长% **超过**它才算「成熟」，
才切回默认贴图。

### 2.4 风吹：`topWindExposure`

- 默认 **0.25**，范围 0–1（1.0 = 摇到最大）。
- 通过偏移 mesh **顶点**实现，**不是简单的左右摇摆**——
  会沿 x/z 轴产生夸张的拉伸感。
- 值会影响观感但不影响性能。

### 2.5 阴影

```xml
<graphicData>
  <shadowData>
    <volume>(0.5, 0.2, 0.5)</volume>   <!-- 阴影贴图的缩放 -->
    <offset>(0.1, 0, 0.1)</offset>    <!-- 相对植物贴图中心的偏移 -->
  </shadowData>
</graphicData>
```

省略 `shadowData` 就不画阴影。

### 2.6 落叶变色（仅树）

写在 `graphicData/shaderParameters`：

| 参数 | 说明 |
|---|---|
| `_FallColorSource` | 变色起始的基准 HSV 颜色 |
| `_FallColorDestination` | 可选，贴图路径，用来按随机性覆盖目标色 |
| `_FallTransitionRange` | 过渡色相宽度与混合曲线形状 |
| `_FallBehaviorEnabled` | 1 开 / 0 关 |

⚠️ **只有 `CutoutPlant` 这个 `shaderType` 支持**，`TransparentPlant` 不支持。

---

## 3. 音效（Sounds）

> ⚠️ **该页标了「Heavily outdated」，而且自相矛盾**：
> 它整篇的流程都建立在用 **package editor** 编辑 SoundDef 上，
> 但页面开头又明写「**Package editor 已不存在**」。
> 所以下面只保留仍然成立的原理，具体编辑器操作已失效。

### 3.1 三个协作要素

1. **sound event（声音事件）**——游戏代码发出的信号，**硬编码在代码里**。
   [事件与参数总表（Google Spreadsheet）](https://docs.google.com/spreadsheet/pub?key=0AgWNUTNe4QC7dGpXa2ZHQTdPSU8yOVdrMllfbV9leEE&output=html)
2. **SoundDef**——XML 定义，规定事件触发时**做什么**（可能什么都不做、可能固定播一个文件、
   也可能带滤镜和随机参数地轮播一组文件）。
3. **sound file**——wav / ogg / mp3，用 Audacity、SoundForge 等外部工具制作。

### 3.2 仍然有效的要点

- **SoundDef 的 `defName` 必须匹配代码里查找的事件名，否则永远不会被播放。**
- 也可以把多个标识符填进 `eventNames` 列表，让同一段声音响应多个事件。
- 结构是 `subSounds` → `grains`，每个 grain 是一个采样或一段静音。
- **grain 的 `path` 相对于 mod 根目录、不带扩展名**：
  `Mods/MyMod/Sounds/Guns/Gunshot1.wav` → `Sounds/Guns/Gunshot1`。
- **参数（parameters）**由代码传入（如 `ImpactSpeed`、`FireSize`、`CameraAltitude`），
  可以作用于 sustainer 或 oneshot。参数可**每采样固定一次**或**逐帧更新**——
  同一段参数，逐帧更新会让枪声音高听上去像溜溜球一样上下抖。

---

## 4. Asset Bundle

### 4.1 是什么 / 为什么用

Asset bundle 是**预先按引擎格式打包好的 Unity 资源单文件**。
散落在 `Textures/` 的 PNG 不是引擎格式，游戏启动时必须逐个转换。

两个理由值得用：

1. **加载时间**——大 mod 几百上千张贴图的转换开销可观；bundle 跳过这一步。
2. **非贴图资源**——散文件只对贴图和音效有效。
   shader、字体、网格等 Unity 形态的东西**没有 `Shaders/` 目录可以直接扔文件进去**，
   只能走 bundle。

**1.6 起 bundle 成为「超出小型散图 mod 范围」的推荐路径**，引擎有了正式支持：
自动加载、自动按操作系统选择正确的那份、并通过同一套内容查找暴露内容。

### 4.2 三种方式对比

| 方式 | 加载开销 | 磁盘体积 | 能装什么 | 构建后还能改吗 | 适合 |
|---|---|---|---|---|---|
| 散 PNG | 每次加载都转换 | 小 | 贴图、音效 | ✅ | 小 mod、原型 |
| DDS | 无（已预转换） | **大** | 仅贴图 | ❌ 很难 | 大批量贴图 |
| Asset bundle | 无（已预转换） | **最小** | 贴图、网格、shader、字体、音频 | ❌ 要回源重建 | 大 mod、非贴图资源 |

**体积实测（同一组约 960 个文件，社区在 modding Discord 分享的数据）**：

- 散 PNG：约 **40 MB**
- 转成 DDS：约 **230 MB**（省了加载开销，但磁盘暴涨）
- 打成单个 bundle：约 **28 MB**

⚠️ **DDS 才是体积最大的那个**，不是最小的。它是拿空间换加载速度；
bundle 两者兼得，因为压进了单文件里。

### 4.3 基本要求

- **Unity 版本必须对上**：1.6 跑在 **Unity 2022.3.35f1**。
  用错版本打的 bundle 可能**静默加载失败**。用 Unity Hub 装这个精确版本。
- bundle 放 mod 的 **`AssetBundles/`** 目录，文件**无扩展名**：
  ```
  MyMod/AssetBundles/mymod_assets
  ```
- **不需要任何 DLL**——纯 XML + 贴图的 mod 可以直接带 bundle，一行 C# 都不用写。
- Unity 工程里的**目录结构要和 mod 里一致**——游戏按 bundle **内部**的路径查找，
  必须和散文件原本的路径对得上。
- 编辑器脚本所在的文件夹**必须精确命名为 `Editor`**，否则 Unity 根本不执行它，菜单项不会出现。

### 4.4 分平台 bundle

- **只有 bundle 里含 shader 时才需要按系统分副本**。
  贴图、网格、字体跨平台一致，单份无后缀的 bundle 全平台通用。
  音频通常也能放进无后缀 bundle（但导入设置可按平台覆盖，若某系统上音频异常先查这里）。
- 含编译后 shader 的 bundle 必须**按操作系统各构建一份**，用后缀区分：
  **`_win` / `_mac` / `_linux`**。
- 不带 shader 就忽略后缀，ship 单文件即可。

> 🔴 **症状对照**：mod **在 Windows 正常、只有 Mac / Linux 玩家看到满屏粉红（magenta）**，
> 几乎总是这个原因——为 DirectX 构建的 shader，换到 Metal / Vulkan 上不是同一份字节。

**建议把 shader 单独放一个 bundle**：贴图等只打一次，shader 才打三份（带后缀），
这样大批量贴图不用重复打三遍：

```
AssetBundles/
  mymod_assets          # 贴图、网格、音效、字体，无后缀，全平台通用
  mymod_shaders_win
  mymod_shaders_mac
  mymod_shaders_linux
```

> 🔴 **1.7 的额外坑**：因为改用 Vulkan，**1.7 只会加载你提供的 shader 的 Linux 版本**。
> 以前没给 Linux 构建过 shader 的 mod，1.7 下会加载失败。详见
> [version-migration.md](version-migration.md) §2.6。

### 4.5 导入设置（构建脚本要干的活）

关键是**在打包前给每张贴图设好导入设置**，否则默认未压缩，体积和显存都吃亏：

| 设置 | 值 | 理由 |
|---|---|---|
| 压缩格式 | **BC7 + CompressedHQ** | 磁盘和 VRAM 都小，适合手绘 |
| sRGB | `_mask` / `_normal` 命名的**关掉** | 遮罩和法线图存的是数据不是颜色，按 sRGB 读会静默损坏 |
| wrapMode | 路径含 `/Terrain/` 用 **Repeat**，否则 **Clamp** | 地形要无缝平铺；普通精灵要 Clamp 否则边缘渗色 |
| anisoLevel | 地形 8，其余 1 | |
| mipmap | 开启 | 缩小时不闪烁 |
| textureType | 法线图用 `NormalMap`，其余 **GUI** | 2D 平面画，避免 Unity 生成子资源 |
| alphaIsTransparency | true | |
| 打包压缩 | **LZ4**（`ChunkBasedCompression`） | |

> 脚本按**文件名**里的 `_mask` / `_normal` 和**路径**里的 `/Terrain/` 来自动区分，
> 所以文件命名要照这个约定。

### 4.6 构建与产物收集

1. **让 Unity 编译脚本**——保存后回到编辑器，右下角短暂转圈。
   编译错误会显示在 Console 窗口（Window > General > Console）。
   ⚠️ **脚本编译不过就不会注册菜单项**，先修好再往下走。
2. **运行**——菜单栏顶部出现 `Assets > Build AssetBundles`（即脚本里那行 `[MenuItem]`）。
   点它，Unity 会按 §4.5 的设置重新导入全部贴图再写出 bundle；
   大批量时会有进度条，可能要等。
3. **找产物**——输出在工程的 `Assets/BuiltBundles/`。
   每个你打过 tag 的 bundle 是一个**无扩展名**的文件，同时会生成
   各自的 `.manifest` 和一个文件夹级 manifest。
   ⚠️ **只 ship 无扩展名的 bundle 文件**；`.manifest` 只是 Unity 的记账文件，RimWorld 直接忽略。
4. **拷进 mod**——把无扩展名文件放进 `MyMod/AssetBundles/` 即完成。

之后每次新增资源就是：**丢进工程 → 打 tag → 重跑 `Assets > Build AssetBundles` → 覆盖拷贝**。

### 4.7 加载：1.6 起不用自己写加载代码

**RimWorld 启动时会自己扫每个启用 mod 的 `AssetBundles/` 目录并加载。**
不用写 `AssetBundle.LoadFromFile`，不用调 `LoadAsset`，静态构造函数里什么都不用放。
（老文档把这事写得很复杂，是因为那时要手动接线。）

内容查找也会自动穿透 bundle。以
`ContentFinder<Texture2D>.Get("Things/Item/MyThing")` 为例，查找顺序是：

1. 任意启用 mod 里的**散文件**（该路径）
2. **原版内置资源**
3. **已加载的 bundle** 里的同路径

→ **贴图和音效从散文件切到 bundle，代码一行都不用改**，路径字符串照旧。
唯一要求是资源在 bundle **内部**的路径，和散文件本该在的位置一致。

> 🔴 **bundle 不能覆盖、也不能重贴原版资源。**
> 因为 bundle 排在最后、原版资源总是先被命中，同路径的你的版本永远走不到。
> **bundle 只能加新内容，不能替换游戏自带的东西。**
> 要重贴原版仍然用老办法：同路径的**散文件**（它排在原版之前），
> 或用 Def / `texPath` 重定向把原版物件指到你自己的贴图。音效同理。
>
> ⚠️ **例外是 shader**：内容查找对 shader **完全跳过散文件步骤**（根本没有散 shader 的路径），
> 所以 shader **只可能在 bundle 里被找到**。
> `ContentFinder<Shader>.Get("MyShaderName")` 能用，纯粹是因为它在某个 bundle 里。

### 4.8 音频 / 字体 / 网格何时必须走 bundle

| 资源 | 说明 |
|---|---|
| **音频** | 多数情况**不需要 bundle**——RimWorld 有正常的 SoundDef 系统加载散音频（见 §3）。适合走 bundle 的两种情况：想和别的资源一起打包，或**直接从代码取 `AudioClip`** 而不经过 SoundDef |
| **字体** | **只能走 bundle**——RimWorld **根本没有散字体路径**。这里 bundle 不是提速手段，而是**唯一入口**：导入字体 → 打进 bundle → 作为 `Font` 加载 → 赋给绘字处 |
| **网格** | 自定义 3D 网格（不是常见平面精灵，而是真实几何体）只能走 bundle。多数 mod 碰不到 |

### 4.9 常见坑清单

| 坑 | 表现 / 原因 |
|---|---|
| **Unity 版本不匹配** | 最常见的「编辑器里好好的、游戏里没反应」——资产直接返回 null 且**没有明确报错**。1.6 必须用 **Unity 2022.3.35f1** 构建 |
| **bundle 文件压缩** | 与贴图压缩是两回事。可选 不压缩 / LZ4 / LZMA。**LZ4 是通常的最优点**：够小，且不像 LZMA 那样加载时卡顿。`BuildAssetBundleOptions.ChunkBasedCompression` = LZ4 |
| **内存内贴图压缩 → 白边** | 散贴图加载后会在内存里压缩，丢掉了全透明像素里的颜色数据 → 精灵周围出现淡白 halo（§1.5）。**bundle 可以关掉它的内存内压缩**，从而绕开这个问题而不必再额外描边 |
| **Shader stripping** | Unity 构建时会**裁掉它认为没在用的 shader 变体**。运行时 shader 表现不对、但编辑器里正常 → 首要怀疑就是变体被裁了 |
| **色彩空间** | bundle 里出来的贴图发灰或过暗 → 检查导入设置和色彩空间是否与游戏一致。**和手搓 DDS 变难看属于同一类问题** |

### 4.10 判断该用哪种

- **小 mod** → 散 PNG 即可，别碰 bundle。
- **大批量贴图** → DDS 能换加载速度，但**磁盘反而最大**；bundle 两者兼得。
- **需要 shader / 字体 / 自定义网格** → 必须 bundle，且 **shader 要单独一个 bundle 并按平台打三份**，
  否则 Mac / Linux 玩家满屏粉红。

### 4.11 1.7 状态

1.6 的 bundle 在 1.7 继续可用，但**已不再必要**——
1.7 首次加载会把资源缓存起来，之后有无 bundle 都快得多。
分平台的新坑见 §4.4。
