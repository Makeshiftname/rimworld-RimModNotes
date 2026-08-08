# RimTalk Notes（第三方收藏）

## 来源

- **原作者**：juicy（GitHub: [jlibrary](https://github.com/jlibrary)）
- **GitHub**：https://github.com/jlibrary/RimTalk
- **克隆 commit**：`df80ade`（2026-08-04）
- **许可**：**CC BY-NC-SA 4.0**（署名-非商业性-相同方式共享）
- **状态**：纯学习收藏，未发布；上游完整文档见 [README_UPSTREAM.md](README_UPSTREAM.md)

## 一句话定位

AI 驱动的游戏内对话 mod：用 Google Gemini / OpenAI 兼容 API 为每个小人实时生成贴合心情、想法、处境的动态对话气泡（packageId `cj.rimtalk`，1.5/1.6）。

## 值得学习的知识点

- **大量 Harmony hook 触发对话**：`Source/Patch/` 下 15 个 patch（`MentalStatePatch`、`ThoughtPatch`、`BattleLogPatch`、`BubblePatch`、`TickManagerPatch`、`SkillLearnPatch` 等）——展示如何从游戏各事件源触发并注入自定义行为。
- **服务分层**：`Source/Service/`（`AIService`、`ContextBuilder`、`PersonaService`、`TalkService`、`RelationService`）把「上下文构建 → 对话生成 → 展示」分层。
- **Scriban 模板**：`Source/Prompt/`（`PromptManager`、`PresetSerializer`、`Parser`）用 Scriban 模板做 prompt 预设（C# 式逻辑、`for p in pawns` 迭代、访问 `pawn`/`map`/`Find` 游戏对象）。
- **多 Provider 抽象**：`Source/Settings/AIProvider.cs` 支持 Google Gemini、OpenAI 兼容、自定义 Base URL、本地 provider，多个配置并存。
- **分片设置**：`RimTalkSettings` + `Settings_Api`/`Settings_AIInstruction`/`Settings_ContextFilter`/`Settings_EventFilter`/`Settings_PromptPreset` 按域拆分设置 UI。
- **生态兼容**：`Source/Compatibility/` 处理与其他 mod 的兼容（PRs welcome）。

## 目录结构

```
收集/80-standalone-RimTalk/
├── About/About.xml          # packageId cj.rimtalk, author juicy
├── README_UPSTREAM.md       # 上游完整文档
├── LICENSE                  # CC BY-NC-SA 4.0（原样保留）
├── Defs/ Languages/ Textures/
├── Source/                  # API/ Client/ Compatibility/ Data/ Error/ Patch/ Prompt/ Service/ Settings/ UI/ Util/
├── Libs/                    # 第三方库
└── RimTalk.csproj
```

## 备注（许可红线）

- CC BY-NC-SA 4.0：**非商业、相同方式共享**——学习参考需保留署名；衍生发布须同许可且非商用。
- 本目录仅作学习收藏，不用于发布；上游为活跃维护项目，收藏后如需更新可重新克隆并更新 commit 记录。
