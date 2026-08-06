# Hauts Added Traits Notes（第三方收藏）

## 来源

- **原作者**：Hautarche（GitHub: [LaserToothLiger](https://github.com/LaserToothLiger)）
- **GitHub**：https://github.com/LaserToothLiger/Hauts-Added-Traits
- **克隆 commit**：`eb83882`（v1.4.14）
- **版本**：v1.4.14（packageId `Hautarche.HautsTraits`，1.5/1.6）
- **许可**：**MIT**（Copyright © 2025 LaserToothLiger）
- **状态**：纯学习收藏，未发布；上游完整文档见 [README_UPSTREAM.md](README_UPSTREAM.md)

## 一句话定位

「加特质」教科书级 mod：给小人加 89 个特质（50 核心 + 15 Ideology + 7 Biotech + 4 Royalty + 6 Anomaly + 8 Odyssey），**大部分带独特机制**（主动战斗技能、新的灵感来源、触发式心情/精神崩溃/袭击等）；附带「特质血清」道具（`TraitSerumWindow` 选特质）与 ModSettings（出生特质数量 1–9）。

## 值得学习的知识点

- **三层「独特机制特质」写法**（对应知识库 `xml-defs-and-patches.md` / `harmony-patching.md`）：
  1. **XML 层**：`Defs/Traits.xml` 用标准 `TraitDef`（`degreeDatas`/`commonality`/`conflictingTraits`/`requiredWorkTags`），并在 `Thoughts_Trait_Specific.xml`、`Hediffs.xml`、`MentalStateDefs.xml`、`AbilityDefs.xml`、`Jobs.xml` 里用 `thoughtClass`/`workerClass`/`compClass` 把 C# 类挂到 Def 上。
  2. **Harmony 层**：`HarmonyPatches.cs` `[StaticConstructorOnStartup]` 静态构造里 `harmony.Patch(...)` 打 vanilla 方法，补丁内 `pawn.story.traits.HasTrait(HVTDefOf.HVT_XXX)` 判断再改行为——社交交互、精神崩溃、受伤、意见、小人生成、成长时刻全覆盖。
  3. **DefOf 门控**：`DefOfs.cs` `[DefOf]` + `[MayRequireIdeology/Biotech/Royalty/Anomaly/Odyssey]`，对应 XML `<li MayRequire="...">`，无 DLC 不加载不报错。
- **继承 vanilla 类写自定义行为**：`Thoughts_*.cs` 的 `ThoughtWorker_*`/`Thought_Situational`（如 `ThoughtWorker_GlobetrotHediff` 心情随去过地标数缩放）、`Hediffs_*.cs` 的 `Hediff`/`HediffComp`（`Hediff_Conform` 每 2 天按意识形态转化）、`MentalStates.cs`、`JobDriver`（特质血清使用）、`Window`（`TraitSerumWindow` 选特质 UI）、`LordJob`+`PawnsArrivalModeDef`（Skulker 潜入式袭击）。
- **DefModExtension 数据驱动**：`SuperPsychicTrait` / `SpecificPNFChargeCost` / `PersoneuroformatterScrambler` 挂在 TraitDef 上，让 XML 给 C# 传参（避免硬编码）。
- **ModSettings + 成长时刻 hook**：`traitsMin/Max`（1–9）设置，patch `ChoiceLetter_GrowthMoment` / `Pawn_AgeTracker` 让小孩有更多成长时刻凑满特质数。
- **启动期动态改 Def**：静态构造里遍历 `DefDatabase<TraitDef>`，给 Tranquil 特质动态补 `conflictingTraits`（排除所有 Violent worktag 特质）。
- **第三方兼容工程分离**：`1.6/Mods/` 下是独立工程（VPE/CE/VRE/Royalty/Anomaly 等），`About.xml` 用一串 `loadAfter`——正好演示「patch 第三方 mod 用 loadAfter」的做法。

## 目录结构

```
81-HautsAddedTraits/
├── About/About.xml          # packageId Hautarche.HautsTraits, author Hautarche
├── README_UPSTREAM.md       # 上游完整文档
├── LICENSE                  # MIT（原样保留）
├── loadFolders.xml          # 1.5/1.6 版本映射
├── 1.5/  1.6/               # 版本目录
│   ├── Defs/                # Traits.xml + Thoughts/Hediffs/GeneDefs/AbilityDefs/MentalStateDefs/Jobs...
│   ├── Source/HautsTraits/  # HarmonyPatches/DefOfs/Thoughts_*/Hediffs_*/MentalStates/ModSettings/...
│   ├── Patches/             # XML 补丁（PawnKinds/ThinkTree/Biotech/Ideology/Weather...）
│   ├── Mods/                # 第三方兼容独立工程（VPE/CE/VRE...）
│   └── Assemblies/
├── Languages/   Textures/
└── .gitignore   .gitattributes
```

## 备注（许可红线）

- **MIT**：宽松许可，可自由使用/修改/分发（保留版权声明即可）。本仓库仍按收藏惯例保留原作者署名与 `About.xml` 的 `<author>`（Hautarche）。
- 依赖 `brrainz.harmony` 与 `Hautarche.HautsFramework`，并 `loadAfter` 一堆第三方 mod——作为纯学习收藏，未做运行验证。
- 上游为活跃维护项目，收藏后如需更新可重新克隆并更新 commit 记录。
