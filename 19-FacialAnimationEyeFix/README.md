# Facial Animation Eye Fix Notes

## 一句话定位
修复 [NL] Facial Animation 的眼部纹理不刷新问题：游戏开始后强制重绘全部面部贴图。

## 关键要点
- **GameComponent 一次性修复**：`TextureReDraw : GameComponent` 在首次 `GameComponentTick()` 调用 `FacialAnimation.MyGraphicPool.RepaintAllGraphic()` 后置 `runOnce = true`，只执行一次。
- 依赖：[NL] Facial Animation（`nals.facialanimation`），写入 `<loadAfter>`。
- 参考：GameComponent 用法见 `../../docs/knowledge/game-and-world-components.md`。

## 目录结构
```
19-FacialAnimationEyeFix/
├── About/About.xml
└── Source/                # Main.cs（TextureReDraw）、Logger.cs
```

## 构建
```
cd Source && dotnet build -c Release
```

