# 发布与部署

> 来源 mod：79、55、16、76、78、83、84 等
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

仓库有 **13 个 mod** 带 `_PublisherPlus.xml`（16、26、57、60、66、67、69、70、71、73、75、76、78）。
这是 RimWorld 的 **PublisherPlus** 发布工具配置，示例见
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

**收录第三方 mod 前必看 LICENSE**——仓库里三种情况都有先例：

| mod | LICENSE | 处置 |
|---|---|---|
| 80-standalone-RimTalk | CC BY-NC-SA 4.0 | 非商用 + 相同方式共享，README 标注 |
| 82-patch-HautsAddedTraits | MIT | 可自由使用 |
| 81 / 83 | **无 LICENSE 文件** | 作者未明确授权 → **不做再分发授权推定**，标注「仅本地学习、勿发布」 |

⚠️ **CC BY-NC-SA 的 NonCommercial 是实质限制**：可学习、可个人使用、可改，
但**转载与再分发必须署名 + 保持同协议 + 不得用于商业用途**。`scan_mods.py` 会对所有
非 `RunningBugs` 作者的 mod 报 `third-party author` 警告，这是预期行为不是错误。

## 4. 补丁类 mod 的 About.xml 同步规则

`收集/55-patch-CommonModCompatibilityPatches/AGENTS.md` 约定（发布前必查）：
- 每次增删补丁组，同步更新 `About.xml` 的 `<description>` 里的补丁清单。
- patch 第三方 mod 时，把其 packageId 加入 `<loadAfter>`（而非 `<modDependencies>`）；
  原版/Anomaly 目标无需 loadAfter。

### `<modDependencies>` vs `<loadAfter>`：别写混

55 号那条约定有个前提容易漏记：**它适用于「补丁目标存在与否无所谓」的兼容性补丁**
（对方没装就直接跳过）。如果确实**要求**对方在场，就得用 `<modDependencies>`，
否则玩家没装 RimTalk 时你的 mod 照样加载，只在运行期刷一屏警告。

正反对照（本仓库实例）：

- ✅ **84-patch-RimTalkEventPlus** 正确：`modDependencies` 里写了 `brrainz.harmony` + `cj.rimtalk`
  （带 `displayName` 与 `steamWorkshopUrl`），**`loadAfter` 里的 `cj.rimtalk` 大小写与依赖项一致**。
  ⚠️ 但它的 `loadAfter` 与 `modDependencies` 重复表达同一件事，属冗余——依赖已由 `modDependencies` 表达。
- ❌ **83-patch-RPDPersonaDirector** 踩坑：`loadAfter` 写成 `cj.RimTalk`，
  而依赖项里是 `cj.rimtalk`——**packageId 大小写不一致导致该行实际失效**。
  又因为它同样没把 RimTalk 写进 `modDependencies`，结果就是「少了必需依赖也不报错」。

**自检：`loadAfter` 里的每个 packageId 必须与 `modDependencies` 里的拼写逐字符相同。**

### 声明支持的版本要对得上实际产物

84 号的 `About.xml` 写 `<supportedVersions><li>1.5</li><li>1.6</li></supportedVersions>`，
但 `.csproj` 的 `OutputPath` 只有 `..\..\1.6\Assemblies\`，没有任何 1.5 分支——
**声明支持 1.5，产物却只落在 1.6 目录**，1.5 玩家拿不到 DLL。发布前核对
「`supportedVersions` × 实际输出目录 × 实际编译引用」三者一致。

## 5. 跨 mod 抢同一功能：特性检测 + 互斥锁定（84 号范式）

两个 mod 都往 RimTalk 的 prompt 里塞东西时，硬报错会劝退玩家。84 号给了软处理，
见 [`收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/EnhancedPromptDetector.cs`](../../收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/EnhancedPromptDetector.cs)：

```csharp
private const string PACKAGE_ID = "ruaji.rimtalkpromptenhance";

static EnhancedPromptDetector()
{
    IsLoaded = ModLister.GetActiveModWithIdentifier(PACKAGE_ID, ignorePostfix: true) != null;
    if (!IsLoaded) return;
    // 启动期把 FieldInfo/PropertyInfo 元数据缓存下来，属性优先于字段
    _enableAutoEventCaptureProperty = AccessTools.Property(settingsInstance.GetType(), "EnableAutoEventCapture");
    if (_enableAutoEventCaptureProperty == null)
        _enableAutoEventCaptureField = AccessTools.Field(settingsInstance.GetType(), "EnableAutoEventCapture");
}
```

三个关键点：

1. **`ModLister.GetActiveModWithIdentifier` 判断对方是否加载**，比 `LoadedModManager` 遍历轻。
2. **缓存的是反射「句柄」（`FieldInfo`/`PropertyInfo`）而不是「值」**——
   每次读都 `Invoke` 拿最新设置，否则对方改了开关自己不跟着变。
3. **冲突时软锁**：检测到对方开了自动事件采集，就把本 mod 的对应分类锁死
   （`IsEnhancedPromptLockActive`），并在设置页顶部画横幅说明原因。
   **能力互斥要显式告诉用户，别静默丢弃功能。**

## 6. 相关文件

- 部署与版权：`收集/79-xml-RimFlixAnimeShows/AGENTS.md`
- 发布配置：`收集/16-standalone-ResearchPrerequisites/_PublisherPlus.xml`
- 补丁发布约定：`收集/55-patch-CommonModCompatibilityPatches/AGENTS.md`
- 依赖声明正反对照：`收集/83-patch-RPDPersonaDirector/About/About.xml`、`收集/84-patch-RimTalkEventPlus/About/About.xml`
- 跨 mod 互斥检测：`收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/EnhancedPromptDetector.cs`

## 相关主题

- 依赖与加载顺序：`harmony-patching.md`
