# Colony Groups Targetable Portraits Notes

## 一句话定位
让 [LTO] Colony Groups 的顶部头像栏和悬停组弹窗头像支持能力选目标（如异常医术点头像施法）。

## 关键要点
- `[HarmonyPatch(ColonistBar, "TryGetEntryAt")]` Prefix：`TacticUtils.TacticalColonistBar` 为 null 时放行原逻辑 → `TryGetEntryAt`（主栏头像）→ fallback `TryGetGroupPawnAt`（悬停组弹窗）→ 均未命中返回 false 阻断。
- 命中后构造 `new ColonistBar.Entry(pawn, map, group)`；其余校验/高亮/音效/shift 连选全由 vanilla `Targeter` 完成。
- **根因**（docs 反编译核实）：TG 顶掉 `ColonistBarOnGUI` 绘制但未 patch `TryGetEntryAt`，vanilla `cachedDrawLocs` 不更新 → 命中永远落空。
- 编译期引用 `TacticalGroups.dll`（`<Private>false</Private>`，**不分发**，勿删勿误提交）。
- 依赖 `DerekBickley.LTOColonyGroupsFinal` + Harmony。
- docs 论证同一套源码可跨 1.2–1.6 编译（TG 侧三入口同名同签名）。

## 目录结构
```
76-ColonyGroupsTargetablePortraits/
├── About/About.xml
├── docs/changes/colony-groups-targetable-portraits/   # plan.md / proposal.md / tasks.md
└── 1.6/Source/               # ColonistBarTryGetEntryAtPatch.cs + TacticalGroups.dll（编译期引用）
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

## 相关文件
- `docs/changes/colony-groups-targetable-portraits/` — 设计/计划/任务
- Harmony 补丁模式：`../../docs/knowledge/harmony-patching.md`
