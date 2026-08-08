# XML Defs 与 PatchOperation

> 来源 mod：30、37、46、52、78（纯 XML）；几乎所有 C# mod 的 `Common/Defs`、`Common/Patches`
> 适用：不改代码就能改 Defs、静音、改数值、加内容。

## 1. PatchOperation 系列（改已有 Def）

用 `Patches/*.xml` + `PatchOperationSequence` 批量改 Defs，可带 `MayRequire` 条件。
见 [`收集/30-xml-SilencedToxifierGenerator/Common/Patches/Patches.xml`](../../收集/30-xml-SilencedToxifierGenerator/Common/Patches/Patches.xml)：

```xml
<Patch>
    <Operation Class="PatchOperationSequence">
        <operations>
            <li Class="PatchOperationReplace" MayRequire="Ludeon.Rimworld.Biotech">
                <xpath>Defs/SoundDef[defName="Toxifier_Working"]/subSounds/li/volumeRange</xpath>
                <value><volumeRange>0</volumeRange></value>
            </li>
        </operations>
    </Operation>
</Patch>
```

要点：
- `MayRequire="Ludeon.Rimworld.Biotech"` —— 仅在 Biotech DLC 激活时生效（按 packageId 判定）。
- `xpath` 精确定位要改的节点；`PatchOperationReplace` 替换节点内容。
- 同类操作：`PatchOperationAdd`、`PatchOperationRemove`、`PatchOperationFindMod` 等。

## 2. 纯 XML 内容包（无 C#）

`收集/79-xml-RimFlixAnimeShows` 是范式：定义 `RimFlix.ShowDef`（自定义 DefClass）+ 帧图目录，
由脚本抽帧生成每个节目的 `<ID>.xml`。详见
[`收集/79-xml-RimFlixAnimeShows/AGENTS.md`](../../收集/79-xml-RimFlixAnimeShows/AGENTS.md) 与
[`收集/79-xml-RimFlixAnimeShows/docs/adding-shows.md`](../../收集/79-xml-RimFlixAnimeShows/docs/adding-shows.md)。

纯 XML mod 的常见形态：
- 静音补丁（30 排污发电机、46 Milira 音量）
- 列表改动（37 把食物加进成瘾品列表 `Patches/FoodPatches.xml`）
- 事件/叙事者放行（52 海星事件对所有叙事者开放）

## 3. 目录约定（跨版本共享）

- 纯 XML / 跨版本共享的 Defs、Patches、Languages 放 **`Common/`** 下，
  版本目录（1.4/1.5/1.6）只放该版本特有内容，避免重复维护。
- 参见 [cross-version-structure.md](cross-version-structure.md)。
- 测试脚本会校验所有 XML 可解析（见 [testing-and-validation.md](testing-and-validation.md)）。

## 4. 相关文件

- 数值补丁：`收集/30-xml-SilencedToxifierGenerator/Common/Patches/Patches.xml`
- 内容包：`收集/79-xml-RimFlixAnimeShows/AGENTS.md`
- 结构约定：`cross-version-structure.md`
