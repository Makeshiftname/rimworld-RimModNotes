# 性能优化与 GUI 剖析

> 来源 mod：收集/76-standalone-SmoothDragSelect（本仓库含金量最高的性能笔记）
> 适用：一切"卡顿"问题的定位与优化；尤其警惕 GUI 层的坑。

## 1. 核心结论：IMGUI 的隐形成本

RimWorld 用 Unity **IMGUI**（`OnGUI` 逐帧绘制 UI）。致命点：**每次鼠标事件**
（MouseMove / MouseDrag 等）都会让引擎把整个 OnGUI 树完整跑一遍——包括殖民者栏、
主按钮、窗口栈、各 MOD 窗口。

- 实测全量遍历约 **20ms/次**（来源：收集/76-standalone-SmoothDragSelect 的压测）。
- 换句话说，OnGUI 被调用的次数**远大于帧数**，在 OnGUI 里做任何重活都会被放大。

## 2. GUI 死亡螺旋（恶性循环）

```
帧率下降 → 输入事件排队变多 → 每帧要处理的 OnGUI 遍历次数增多
        → 单帧时间更长 → 帧率进一步下降 → ...
```

- 最坏情况：单帧全部时间（实测 **5.7 秒**）都耗在 GUI 遍历上。
- 挂在 OnGUI 里的 FPS 计数器会**虚高到 900+**，因为触发次数 ≠ 帧数——用 OnGUI
  计数做帧率统计是**错误**的。

## 3. 参考解法：入口级自适应限流

- 在 `UIRoot_Play.UIRootOnGUI` 入口对**非 Repaint** 事件做限流：
  间隔 = 平滑帧时间 ÷ 2，范围夹在 **60Hz–10Hz**。
- `Repaint` 事件**不拦截**（保证 UI 刷新正常）。
- 效果：帧率恢复时自动放宽，卡顿时主动降低事件处理频率，打破死亡螺旋。
- 完整设计与数据：[`收集/76-standalone-SmoothDragSelect/docs/superpowers/specs/2026-07-27-mouse-event-throttle-design.md`](../../收集/76-standalone-SmoothDragSelect/docs/superpowers/specs/2026-07-27-mouse-event-throttle-design.md)
- 原文笔记：[`收集/76-standalone-SmoothDragSelect/README.md`](../../收集/76-standalone-SmoothDragSelect/README.md)

## 4. 实用原则（从笔记提炼）

1. 不要相信 OnGUI 里的 FPS 计数。
2. 每 tick 逻辑要轻（`WorldComponentTick` 内避免每帧分配/字符串拼接，见
   game-and-world-components.md）。
3. 自定义窗口绘制（`DoWindowContents`）同样属于 OnGUI 树，保持轻量。
4. 用 `Verse.Root.Update` 后缀计真实帧（75 的 specs 里提到）。

## 5. 相关文件

- 核心笔记：`收集/76-standalone-SmoothDragSelect/README.md`
- 设计文档：`收集/76-standalone-SmoothDragSelect/docs/superpowers/specs/2026-07-27-mouse-event-throttle-design.md`

## 相关主题

- UI 绘制：`ui-and-windows.md`
- 每 tick 逻辑：`game-and-world-components.md`
