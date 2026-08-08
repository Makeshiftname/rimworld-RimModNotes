# Research Prerequisites 前置研究 Notes

## 一句话定位
给原生科研页加「自动研究前置科技」功能：选择一项研究后，其所有未完成前置自动进入研究队列，逐项研究。

## 关键要点
- **研究队列**：`ResearchQueue` + `ResearchQueueController` 管理待研究列表；`ResearchCategory` 处理科研页交互。
- **行为规则**（来自 About.xml）：选中研究需有研究台/科研蓝图、不能已完成或正在研究；已有队列时再选其他科技会丢弃旧队列；可直接研究时点原生按钮，完成后自动继续旧队列。
- **ModSettings**：`RPModSettings` 提供设置项。
- **patch**：`1.6/Source/Patches/` 内补丁接入原生研究 UI。
- **设计文档**：`docs/superpowers/specs|plans`（研究队列重设计，含 1.6 冻结说明）。
- 依赖 Harmony；支持 1.4/1.5/1.6。

## 目录结构
```
收集/16-standalone-ResearchPrerequisites/
├── About/About.xml
├── docs/                  # superpowers specs/plans 设计文档
├── 1.4|1.5|1.6/Source/    # ResearchQueue*.cs、RPModSettings.cs、Patches/
└── _PublisherPlus.xml     # 发布配置
```

## 构建
```
cd 1.6/Source && dotnet build -c Release
```

## 相关文件
- `docs/` — 设计文档
- `1.6/Source/ResearchQueueController.cs` — 队列核心

