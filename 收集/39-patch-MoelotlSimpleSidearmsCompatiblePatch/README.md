# 萌螈和简单的副武器模组兼容性补丁 Moelotl and SimpleSidearms Compatible Patch Notes

## 一句话定位
修复萌螈（MoeLotl）与 Simple Sidearms 的兼容性（空 Pawn 崩溃）。

## 关键要点
- **空 Pawn 防御**：Harmony Prefix 拦截 `Verb_LotiIntensifyShoot.CanUse`（getter），若 `__instance.GetPawn == null` 则 `__result = false; return false`（阻止空 Pawn 使用萌螈种族强化射击技能）。
- 直接 `using Axolotl;` 引用萌螈 DLL（`Axolotl.dll`）。
- 依赖：Harmony + MoeLotl Race（`hentailoliteam.axolotl`）。
- 备注：若萌螈官方修复此问题，本 mod 会隐藏。

## 目录结构
```
39-MoelotlSimpleSidearmsCompatiblePatch/
├── About/About.xml
└── 1.5/Source/             # Main.cs + Axolotl.dll
```

## 构建
```
cd 1.5/Source && dotnet build -c Release
```

