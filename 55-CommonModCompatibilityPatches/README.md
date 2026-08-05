# Common Mod Compatibility Patches Notes

## 一句话定位
常用第三方 mod 的运行时兼容补丁集合：启动时动态检测目标，缺失则静默跳过。

## 关键要点
- **架构**：`CommonCompatibilityBootstrap : StaticConstructorOnStartup` 依次 `TryApply(Harmony)` **21 个补丁组**；每补丁组一个 `internal static class`，目标 mod 未激活/方法未找到→返回 `false` 跳过；`ModDetection.IsActive(packageId)` 检测。
- **代表性补丁**：
  - ZeroWeightSongSelection：清除原版音乐 recent-song 过滤，避免只剩零权重歌时返回 null 刷红错
  - AllowToolHaulUrgently：Allow Tool 搬运缓存守卫
  - InvokeHoraxOffering：Anomaly 召唤 Horax 仪式，快照库存祭品防 collection-modified
  - NewRatkinWanderingCaravan：游荡商队 null-safe 清理
  - SleepingSlotFallback：床位占位红错→优雅回退
  - PawnDuplicatorGeneCopy：复制人基因解析容忍缺失
  - 其余 15 组：ReservationEvent、ReplaceStuffBridge/OverMineable、BuildFromInventory、TinyTweaksAutoRebuild、RimStory、GoodwillSituation、NalsDynamicPortraits、PawnHealthBar、KiiroStealthMapTick、AlienRace、MinifyEverything、LifeStageMinAge、QualityBuilder 等
- **约定**（见 `AGENTS.md`）：改动必须同步 `About.xml` 的 "Current patches" 清单；patch 第三方 mod 时其 packageId 进 `<loadAfter>` 而非依赖；编译后 DLL 随源码提交。
- 旧单功能 bugfix mod（AllowToolBugfix / ReservationEventBugfix / ReplaceStuffBugfix）标记 `incompatibleWith`。

## 目录结构
```
55-CommonModCompatibilityPatches/
├── About/About.xml              # 唯一文档（含 Current patches 清单）
├── AGENTS.md                    # 维护约定
├── 1.6/Source/                  # CommonModCompatibilityPatches.cs（单文件 ~1700 行）
├── 1.6/Assemblies/              # DLL
├── Tests/whitebox/              # 白盒测试 + run_whitebox.sh
└── LoadFolders.xml
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

## 相关文件
- `AGENTS.md` — 补丁约定
- `Tests/whitebox/` — 白盒测试（断言 loadAfter 而非依赖、旧 bugfix 互斥）
- Harmony 补丁模式：`../../docs/knowledge/harmony-patching.md`
- 测试知识：`../../docs/knowledge/testing-and-validation.md`
