# RimTalk - Expand Memory Notes（第三方收藏）

## 来源

- **原作者**：SANGUO（GitHub: [sanguodxj-byte](https://github.com/sanguodxj-byte)）
- **GitHub**：https://github.com/sanguodxj-byte/RimTalk-ExpandMemory
- **克隆 commit**：`ae1d0a4`（2026-06-26）
- **版本**：3.4.0（packageId `cj.rimtalk.expandmemory`，1.5/1.6）
- **许可**：⚠️ **仓库无 LICENSE 文件**，作者未明确授权（默认保留所有权利）
- **状态**：纯学习收藏，**不发布**（详见下方红线）；依赖 RimTalk（收集/80-standalone-RimTalk）

## 一句话定位

RimTalk 的记忆扩展 mod：给殖民者加一套**四层记忆系统**，让 AI 对话时能记住更多、说得更像真人；附带常识库与记忆时间线 UI。

## 值得学习的知识点

- **四层记忆系统**（`Source/Memory/FourLayerMemoryComp.cs`）：超短期(ABM)、短期(SCM)、中期(ELS)、长期(CLPA)。越老权重越低，重要内容留更久；对话时自动挑选最相关记忆注入（`DynamicMemoryInjection`、`SmartInjectionManager`）。
- **检索与评分双引擎**：`MemoryVectorSearch` + `VectorDB/`（向量检索）+ `SemanticScoringSystem`/`AdvancedScoringSystem`（语义评分）+ `SuperKeywordEngine`/`KeywordExtractionHelper`（关键词）。
- **常识库**（`CommonKnowledgeLibrary.cs`、`CommonKnowledgeAPI.cs`）：手动写入背景知识，格式 `[标签|重要性]内容`；分类管理、批量操作、导入导出；带标签匹配测试工具。
- **三大生命周期组件架构**（见 `项目管线.md`）：常识系统 `WorldComponent` + 轮次记忆 `GameComponent` + 个人记忆 `ThingComp`，各自独立收集/存储/衰减，最终在 API 注入端汇聚。
- **与 RimTalk 集成**（`Source/Patches/`）：hook `GenerateAndProcessTalkAsync`、`PromptContext_FromTalkRequest` 等，把扩展记忆注入 RimTalk 对话管线。
- **文档即学习库**：`Docs/` 有 30+ 篇中文开发文档（记忆系统重构设计、世界书实现、常识库匹配引擎评估、性能优化、时间戳 bug 修复等），是本仓库最大的学习价值。

## 目录结构

```
收集/81-patch-RimTalkExpandMemory/
├── About/About.xml          # packageId cj.rimtalk.expandmemory, author SANGUO
├── 1.5/ 1.6/                # 版本目录（含 Source/Assemblies）
├── Source/                  # API/ Capture/ Memory/ Patches/ Settings/ Utils/
├── Docs/                    # 30+ 篇中文开发文档（重点学习）
├── Languages/  Textures/  Deploy/
├── 项目管线.md              # 架构与运作管线（作者文档）
├── CHANGELOG.md
└── RimTalk-ExpandMemory.csproj
```

## 备注（⚠️ 无许可红线）

- 该仓库**没有 LICENSE 文件**：作者未以任何许可协议授权，默认「保留所有权利」。
- 本目录**仅供本地学习参考**，不得发布、不得制作衍生作品分发；如需使用请先联系作者（GitHub Issues）。
- 与 `收集/80-standalone-RimTalk`（CC BY-NC-SA 4.0）不同，本 mod 授权状态未明，收藏与引用时务必保守。
