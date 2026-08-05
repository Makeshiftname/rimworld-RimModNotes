# Ghoul Attack Spin 食尸鬼攻击旋转特效 Notes

## 一句话定位
食尸鬼近战攻击时加纯视觉 360° 旋转 + 可选「自动激素心脏」开关与食尸鬼狂热快捷键。

## 关键要点
- `[HarmonyPatch(Pawn_DrawTracker, "Notify_MeleeAttackOn")]` Postfix：`GhoulAttackSpinState.StartSpin`（`SpinDurationTicks=28` 匹配原版 melee jitter 时长，记录 start/end/sign）。
- `[HarmonyPatch(PawnRenderer, "GetDrawParms")]` Postfix：`__result.matrix *= Matrix4x4.Rotate(Quaternion.AngleAxis(angle, Vector3.up))`，**只改渲染**不影响伤害/命中/移动/AI/工作。
- 只作用于 `Pawn.IsGhoul` 的 pawn；旋转由 `Notify_MeleeAttackOn` 触发，和原版 melee jitter 动画窗口一致，结束后自动恢复。
- `GhoulAttackSpinSettings`：direction（Clockwise/Counterclockwise/Random 默认 Random）+ `enableAutoFrenzy`。
- `GhoulAutoFrenzy`：`AutoFrenzyState : GameComponent`（按 pawn id 持久化，`GameComponentTick` 每 tick 检测但 `AnyEnabled()` 短路）；`Pawn.GetGizmos` Postfix 注入「自动激素心脏」开关 + 给食尸鬼狂热按钮挂 `GhoulAttackSpin_GhoulFrenzy` 快捷键（`GetNamedSilentFail` 判空，兼容无 Anomaly）。
- KeyBindingDef 在 `1.6/Defs/KeyBindingDefs/`（默认 None）。
- 依赖 Harmony；可选 Anomaly DLC（食尸鬼/狂热）。

## 目录结构
```
74-GhoulAttackSpin/
├── About/About.xml
├── 1.6/Source/               # Main.cs、GhoulAutoFrenzy.cs
├── 1.6/Defs/KeyBindingDefs/
├── Languages/                # 中英
└── _PublisherPlus.xml
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```
