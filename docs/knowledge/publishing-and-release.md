# 发布与部署

> 来源 mod：78、55-Common、16、75、77 等
> 适用：本地软链接部署、PublisherPlus 发布配置、创意工坊注意事项。

## 1. 本地部署：软链接到游戏 Mods 目录

`收集/79-xml-RimFlixAnimeShows/AGENTS.md` 的做法：把 mod 目录软链接到游戏目录，改动即生效。

```bash
# Linux/macOS
ln -s /path/to/NN-ModName "$HOME/.steam/steam/steamapps/common/RimWorld/Mods/NN-ModName"
# Windows（管理员 PowerShell）
New-Item -ItemType SymbolicLink -Path "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\NN-ModName" -Target "F:\Git Repository\rimworld-RimModNotes\NN-ModName"
```

注意游戏内加载顺序：依赖/被补丁的 mod 要排在被 patch 的 mod 之后
（见 [`收集/79-xml-RimFlixAnimeShows/AGENTS.md`](../../收集/79-xml-RimFlixAnimeShows/AGENTS.md)）。

## 2. PublisherPlus 发布配置

仓库有 **13 个 mod** 带 `_PublisherPlus.xml`（16、26、29、56、59、65、66、68、69、
70、72、74、75、77）。这是 RimWorld 的 **PublisherPlus** 发布工具配置，示例见
[`收集/16-standalone-ResearchPrerequisites/_PublisherPlus.xml`](../../收集/16-standalone-ResearchPrerequisites/_PublisherPlus.xml)：

```xml
<Configuration>
  <Excluded>
    <exclude>1.6/Source/obj</exclude>   <!-- 排除构建中间产物 -->
  </Excluded>
</Configuration>
```

作用：发布时自动排除 `obj/` 等不应上传的目录，其余按 mod 结构打包。

## 3. 创意工坊红线（版权）

`收集/79-xml-RimFlixAnimeShows/AGENTS.md` 明确：**本地自用随意；AI 生成的碧蓝航线等角色图/视频
不要上传创意工坊**（角色 IP 属原厂）。发布前检查素材版权。

## 4. 补丁类 mod 的 About.xml 同步规则

`收集/55-patch-CommonModCompatibilityPatches/AGENTS.md` 约定（发布前必查）：
- 每次增删补丁组，同步更新 `About.xml` 的 `<description>` 里的补丁清单。
- patch 第三方 mod 时，把其 packageId 加入 `<loadAfter>`（而非 `<modDependencies>`）；
  原版/Anomaly 目标无需 loadAfter。

## 5. 相关文件

- 部署与版权：`收集/79-xml-RimFlixAnimeShows/AGENTS.md`
- 发布配置：`收集/16-standalone-ResearchPrerequisites/_PublisherPlus.xml`
- 补丁发布约定：`收集/55-patch-CommonModCompatibilityPatches/AGENTS.md`

## 相关主题

- 依赖与加载顺序：`harmony-patching.md`
