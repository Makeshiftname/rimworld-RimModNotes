# 允许掘进机深钻 Allow Tunnelers To Drill（fork）Notes

## 一句话定位
允许 Biotech 的掘进机（Tunneler）机械体在深钻上工作（本目录是第三方 mod 的 fork 临时 1.6 存档版）。

## 关键要点
- **核心 patch（XML）**：根 `Patches/AllowMechToDrill.xml` 用 `PatchOperationReplace` 把 `WorkGiverDef[defName="Drill"]/canBeDoneByMechs` 改为 `true`。
- **XP 修复（C# Transpiler）**：`XPFix` Harmony Transpiler 以 IL 常量 `0.065f` 为锚点，在 `JobDriver_OperateDeepDrill` 的嵌套 lambda（`<>c__DisplayClass1_0`）里注入 `pawn.skills == null → 跳走` 守卫，防止无经验条的机械体加经验报错。
- **作者链**：Enduriel(1.6) / Kazamizam(1.5) / 原 1.4 by Porio；本目录是其 fork 备份（author 非 RunningBugs）。
- 依赖 Harmony + BioTech；About 明说「临时 1.6 版，原 mod 更新后删除」。

## 重要状态
- **无 README（本文件为新写）**；packageId `Porio.TunnulerFix.fork`（拼写 Tunnuler）。
- 1.5 工程带 `.idea/` IDE 残留、pdb 随源码提交。

## 目录结构
```
收集/53-patch-AllowTunnelersToDrillFork/
├── About/About.xml
├── Patches/                  # AllowMechToDrill.xml（核心）
├── 1.4/Source/TunnulerFix.cs
└── 1.5/TunnelersDeepDrill/   # 完整 C# 工程
```

## 构建
```
cd 1.4/Source && dotnet build -c Release
```
