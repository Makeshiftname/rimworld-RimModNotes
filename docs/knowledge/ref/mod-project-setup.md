# Mod 工程基础：About.xml / 目录结构 / 工程搭建 / 反编译

> 参考手册层。蒸馏自 rimworldwiki `Modding Tutorials` 的 vetted 子页，**不含仓库特定结论**。
> 实操笔记见 [project-templates.md](../project-templates.md) 与 [cross-version-structure.md](../cross-version-structure.md)。
>
> 来源页（rimworldwiki.com，最后核对 2026-10-15）：
> [About.xml](https://rimworldwiki.com/wiki/Modding_Tutorials/About.xml) ·
> [Mod Folder Structure](https://rimworldwiki.com/wiki/Modding_Tutorials/Mod_Folder_Structure) ·
> [Setting up a solution](https://rimworldwiki.com/wiki/Modding_Tutorials/Setting_up_a_solution) ·
> [Decompiling source code](https://rimworldwiki.com/wiki/Modding_Tutorials/Decompiling_source_code) ·
> [Recommended software](https://rimworldwiki.com/wiki/Modding_Tutorials/Recommended_software)
>
> 所属 wiki 章节：About RimWorld + C# Guides

---

## 0. 版本基线（先看这个）

| 项 | 1.6 及更早 | **1.7（当前）** |
|---|---|---|
| 运行时 | Unity 2022.3.35 | **Waylith**（自研引擎，非 Unity） |
| 目标框架 | .NET Framework 4.7.2 | **.NET 10.0 (LTS)** |
| 编译引用 | `Assembly-CSharp.dll` + `UnityEngine*.dll` | `Assembly-CSharp.dll` + **`Waylith.dll`** + **`UnityStubs.dll`** |
| 图形 API | — | Vulkan |

⚠️ **wiki 自身存在不一致**：`Recommended software` 页仍写「确保 IDE 能编译 .NET Framework 4.7.2」，
而 `Setting up a solution` 页已全面改为 `.NET 10.0` 并引入 `Waylith.dll` / `UnityStubs.dll`。
**以 `Setting up a solution` 为准**——那页才是随 1.7 一起更新的。

---

## 1. About.xml

**位置**：`<mod>/About/About.xml`。`About/` 目录名大小写敏感，必须精确拼写。
根标签 `<ModMetaData>`，声明 `<?xml version="1.0" encoding="utf-8"?>`。

### 1.1 必填标签

| 标签 | 说明 |
|---|---|
| `<packageId>` | mod 内部唯一标识。用于依赖、加载顺序、`MayRequire`。**全局唯一**——即使不在当前 mod 列表里，只要游戏扫到两个同名 `packageId` 就报错，只有第一个可用。**大小写不敏感**（内部统一转小写），因此 `A.B` 与 `A.b` 视为同一个。仅在 fork / 续作做 drop-in 替换时才复用，此时玩家必须退订原 mod。 |
| `<name>` | 显示用标题。不要求全局唯一，但 `PatchOperationFindMod` **仍按 mod 名匹配**，故除必要外不要改名。 |
| `<author>` / `<authors>` | 多作者用逗号分隔，或用 `<authors><li>…</li></authors>`。 |
| `<description>` | 纯文本。同时用作游戏内 mod 管理器描述和 Steam Workshop 上传时的初始描述。上传后 Workshop 描述可独立改，因此**建议 About.xml 里的描述保持简短**。 |
| `<supportedVersions>` | 如 `<li>1.6</li>`。未声明的版本加载时只警告，不阻止。**强烈建议只声明你真测过的版本**——跨版本改动很大，纯 XML mod 也不保证通用。 |

### 1.2 可选标签

| 标签 | 说明 |
|---|---|
| `<modVersion>` | 自用版本号。**1.4 引入**，更早版本必须加 `IgnoreIfNoMatchingField="True"`，否则旧版报错。 |
| `<modIconPath>` | 加载画面图标路径。**1.5 引入**，同样需要 `IgnoreIfNoMatchingField`。注意 1.5+ 会自动加载 `About/ModIcon.png`，所以这个字段通常不必写，除非你想把图标放在 `Textures/` 里。图标 32×32 PNG，细节不宜多（Unity 压缩会糊）。 |
| `<url>` | 展示用链接（Workshop 页 / GitHub 仓库 / Ludeon 论坛）。 |
| `<descriptionsByVersion>` | 按版本给不同描述。Steam Workshop 不使用。 |
| `<modDependencies>` | 依赖声明。缺依赖时只警告，玩家可忽略。**Steam Workshop 不自动使用**——要让玩家被通知，得用 Workshop 的 "Required Items"。 |
| ↳ `<alternativePackageIds>` | **1.6+**：一个依赖可由多个 `packageId` 满足（正式版 / dev 版共存）。同样需要 `IgnoreIfNoMatchingField`。 |
| `<modDependenciesByVersion>` | 按版本指定依赖。 |
| `<loadBefore>` / `<loadAfter>` | 声明加载顺序，顺序不对只**警告**。必须用 `packageId` 引用。 |
| `<forceLoadBefore>` / `<forceLoadAfter>` | 同上但不满足时**拒绝加载**。⚠️ 外部 mod 管理器（RimSort、RimPy）可能不认这两个字段。 |
| `<incompatibleWith>` | 互斥声明。**只用于根本性不兼容**，不要用来标注「有 bug 冲突」。另有 `<incompatibleWithByVersion>`。 |

> **本仓库约定**：给第三方 mod 打补丁时，把对方 `packageId` 写进 `<loadAfter>` 而非
> `<modDependencies>`——补丁 mod 并不真的需要对方存在，硬依赖会造成不必要的阻塞。

### 1.3 新标签的通用规则

1.4/1.5/1.6 各自新增过标签（`modVersion` / `modIconPath` / `alternativePackageIds` / `IfModActiveAll`）。
**只要你的 mod 声明支持这些版本之前的游戏，就必须给新标签加 `IgnoreIfNoMatchingField="True"`**，
否则旧版解析器遇到未知字段直接抛错。

---

## 2. Mod 目录结构

### 2.1 被识别的顶层文件夹

```
Mods/└ MyModFolder/
  ├ About/            （唯一必填）
  │  ├ About.xml      （必填）
  │  ├ Preview.png    （必填）
  │  ├ ModIcon.png    （1.5+，可选）
  │  └ PublishedFileId.txt
  ├ Assemblies/       编译产物 DLL，游戏自动加载
  ├ AssetBundles/
  ├ Defs/             XML 内容定义
  ├ Languages/
  ├ Materials/
  ├ Patches/
  ├ Sounds/
  ├ Strings/
  └ Textures/
```

- mod 根目录名**随意**，mod 名与 ID 由 `About.xml` 决定。
- `Defs/` 下的子目录和文件名**不影响加载**，可自由组织；但建议跟随原版惯例
  （`SoundDef` 放 `/SoundDefs`，`ThingDef` 放 `/ThingDef_*`）。
- `Core` 和各 DLC 目录在结构上与 mod 目录等价，唯一差别是原版贴图/音效被打进 Unity asset bundle
  而非散文件，位于 RimWorld 目录的 `/Data` 下。

### 2.2 Preview.png

- **640×360**（16:9）是原版默认尺寸，也可用 1280×720；用其他比例会模糊或加黑边。
- **必须小于 1MB**，超了 Steam Workshop 直接拒收并报 `Error: Limit Exceeded`。
- 扩展名必须是 `.png`（可把 JPG/GIF 改名成 `.png`，但游戏按扩展名识别）。

### 2.3 大小写敏感路径 ⚠️

Windows 文件系统不敏感，所以**这个坑在 Windows 上开发时完全看不出来，一到 macOS/Linux 就集体失效**。
游戏硬编码了下列大小写敏感路径：

```
About/            About/Preview.png      About/ModIcon.png
About/PublishedFileId.txt                 Assemblies/
AssetBundles/     Common/                Defs/
Languages/        Materials/             Patches/
Sounds/           Strings/               Textures/
```

**大小写不敏感**的只有两个：`About/About.xml`、`LoadFolders.xml`。
此外 `LoadFolders.xml` 里定义的所有子路径也大小写敏感。

### 2.4 路径冲突原则

贴图和音效都**按路径名**标识。同路径同名文件，**mod 列表里靠后的会覆盖前面的**——
原版替换类 mod 就是故意利用这条规则。若只是不想冲突，用命名空间子目录隔离：

```
Sounds/MyNamespace/MyCustomSound.ogg
Textures/MyNamespace/MyCustomTexture.png
```

`Defs/` 内同理：同解析路径的 XML 后加载者胜。**唯一例外**是用版本化 `LoadFolders.xml` 时，
同解析路径文件互相覆盖，取加载顺序里最后一个。

---

## 3. 跨版本目录

按次版本号建文件夹，游戏加载第一个兼容的版本目录；所有版本都额外加载 `Common/` 和根目录。

```
MyModFolder/
  ├ 1.4/{Assemblies,Defs,Languages,Sounds,Patches,Textures}/
  ├ 1.3/…
  ├ Common/…
  ├ About/  Defs/  Languages/  Sounds/  Patches/  Textures/
```

- 除 `About/` 外，版本目录和 `Common/` 内都能放正常的根目录文件夹。
- 同相对路径的文件互相覆盖，**越具体越优先**（`Defs/X.xml` < `Common/Defs/X.xml` < `1.4/Defs/X.xml`）。
  对 XML（Def 和 Patch）、音频、语言文件、贴图一视同仁。
- **RimWorld 1.0 是特例**：不认 `Common/`；一旦用了 `1.0/` 目录就**完全不加载根目录**。

### 3.1 LoadFolders.xml

放 mod 根目录，用于精确控制各版本加载哪些目录，以及按 DLC/mod 条件加载。
**它会覆盖上面的默认版本目录规则（1.0 除外）**——用了它就必须显式列出根目录和 `Common/`。

```xml
<loadFolders>
  <v1.6>
    <li>/</li>
    <li>1.6</li>
    <!-- 任一命中即加载 -->
    <li IfModActive="Dubwise.DubsBadHygiene,Dubwise.DubsBadHygiene.Lite">DBH</li>
    <!-- 需全部命中（1.6+） -->
    <li IfModActiveAll="Ludeon.RimWorld.Biotech,Ludeon.RimWorld.Odyssey">SpaceXenotypes</li>
    <!-- 未加载时命中 -->
    <li IfModNotActive="Ludeon.RimWorld.Odyssey">CustomBiomes</li>
  </v1.6>
</loadFolders>
```

- `IfModActive` 多值之间是**或**；`IfModActiveAll`（**1.6 新增**）是**且**。
- 条件用 `packageId` 判定，与 `MayRequire` 一致。
- 1.5 及更早：若你同时有本地版和 Steam 版，要写成 `IfModActive="CETeam.CombatExtended,CETeam.CombatExtended_steam"`。

---

## 4. 编译工程搭建

### 4.1 引用哪几个 DLL（1.7）

```
<RimWorldInstall>/Assembly-CSharp.dll
<RimWorldInstall>/Waylith.dll
<RimWorldInstall>/UnityStubs.dll
```

三个都要把 **Copy Local 设为 False**（否则构建后会被拷进输出目录，白占体积还可能引发加载冲突）。
1.6 及更早对应的是 `UnityEngine*.dll` 系列。

### 4.2 推荐做法：Krafs Rimworld Ref（NuGet）

wiki 明确说这是**推荐方法**。原版 DLL 无法分发，而 `Krafs.Rimworld.Ref` 任何人任何平台都能拉下来，
工程因此可移植。

1. 先按常规建好类库工程。
2. `Tools → NuGet Package Manager → Package Manager Settings`，
   把 **Default package management format** 改成 **PackageReference**。
3. NuGet Manager 里搜 `Rimworld`，装 **Krafs Rimworld Reference**。之后等同于手动加了引用。

### 4.3 csproj 关键属性

Rider 版模板给出的配置（本仓库 `cd <版本>/Source && dotnet build -c Release` 的做法同源）：

```xml
<PropertyGroup>
  <DebugType>embedded</DebugType>              <!-- 内嵌 PDB，产物目录不散落 .pdb -->
  <GenerateDependencyFile>false</GenerateDependencyFile>
  <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
  <ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>
  <OutputPath>../1.7/Assemblies</OutputPath>   <!-- 直接落到版本目录，省去手工拷贝 -->
</PropertyGroup>
```

`DebugType` 三档权衡：`Embedded`（推荐，保留调试信息且不产生独立 PDB）、
`None`（DLL 更小但堆栈信息变差）、默认 `Portable`（会多出 PDB 文件）。

### 4.4 目标框架

- **1.7：`.NET 10.0`（LTS）**
- 1.6 及更早：`.NET Framework 4.7.2`
  - 找不到该选项时：VS 里 `Tools → Get Tools and Features → Individual Components` 勾
    `.NET Framework 4.7.2 development tools`，并确认项目类型是
    **Class Library (.NET Framework)** 而不是 .NET Core / .NET Standard。

### 4.5 非 Windows 平台

- **Linux + .NET SDK**：`dotnet build <project>.csproj /property:Configuration=Release`。
  若 .csproj 写死了 Windows 路径会失败，优先改用 NuGet 方案（平台无关）。
- **Linux + Mono**：装 `mono-msc`(Debian) / `mono`(Arch) 后用 `mcs`：

  ```sh
  mcs -target:library -nostdlib -noconfig \
      -reference:"${REFS}/mscorlib.dll" \
      -reference:"${REFS}/Assembly-CSharp.dll" \
      -out:"Assemblies/mymod.dll" "Source/mymod.cs"
  ```

  `-nostdlib -noconfig` 是关键——游戏自带一套 .NET 程序集，要引用它的而不是系统的。
  `${REFS}` 指向 `RimWorldWin64_Data/Managed` 或 NuGet 包里的 `ref/net472`。
- **Windows 纯命令行**：`csc.exe /t:library /r:"...\Assembly-CSharp.dll","...\UnityEngine.CoreModule.dll" MyMod.cs`
  （1.6 写法；1.7 需换成 `Waylith.dll` / `UnityStubs.dll`）。

### 4.6 Source 目录

`Source/` **游戏完全不读**，只是把源码随 mod 一起分发。
wiki 明确建议改用 GitHub 等公开仓库——更易浏览，且提交历史能帮助定位 bug。
本仓库保留 `Source/` 并随 DLL 提交，是为了离线可读，代价是双份维护。

---

## 5. 反编译原版源码

> ⚠️ 反编译 RimWorld 源码受 [RimWorld EULA](https://rimworldgame.com/eula/) 约束，动手前请先读。

XML 只存数据，**「XML 存数据，C# 干活」**——想搞懂游戏实际行为必须读源码。

### 5.1 工具选型

| 工具 | 评价 |
|---|---|
| **ILSpy** | **wiki 首推**。维护最好、最可靠，微软有有限支持。有 VS Code 扩展和跨平台 CLI。 |
| dnSpy / dnSpyEx | UI 像 VS，部分作者偏好，但**不推荐**：原项目多年弃维护，分叉未达同等采用度，反编译编译器生成的复杂类型时经常失败。 |
| dotPeek | JetBrains 出品，看 **IL 代码**（写 Harmony transpiler 时）有人偏爱；但回编译成 C# 时有和 dnSpy 类似的问题，有时直接不显示方法体。 |
| MonoDevelop | 仅 Linux；反编译可用但初始设置繁琐。务必把 `Visibility` 设为 **All members**、`Language` 设为 **C#**，否则看不到东西。 |

### 5.2 程序集位置

| 平台 | 路径 |
|---|---|
| Windows | `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\Assembly-CSharp.dll` |
| macOS | `../RimWorldMac.app/Contents/Resources/Data/Managed/Assembly-CSharp.dll` |

### 5.3 命令行批量导出（ILSpy CLI）

```sh
ilspycmd RimWorld/RimWorldWin64_Data/Managed/Assembly-CSharp.dll -p -o <输出目录>
```

- 可能需要同时装 .NET Core 3.1（尽管文档说 .NET 5.0 SDK 就够）。
- 找不到二进制时可直接从 release tarball 构建，找 `ilspycmd`
  （在 `ICSharpCode.Decompiler.Console/bin/Release/netcoreapp3.1`）。

### 5.4 阅读技巧

- 右键任意类型或方法 → **Analyze**，看 `Used by` / `Uses`，这是理解调用关系最快的方式。
- RimWorld 大量使用**反射 + 工厂方法**创建对象，静态搜索找不到入口。
  典型：Thing 由 `ThingMaker.MakeThing` 创建，Pawn 由 `PawnGenerator.GeneratePawn` 生成。
- 绕圈找「X 在哪里被赋值」时，先去翻 XML——**数据在 XML，行为在 C#**。

---

## 6. 工具链速查

| 用途 | 首选 | 备选 |
|---|---|---|
| XML 编辑 | VS Code（+ redhat XML 插件） | Notepad++（+ XML Tools 插件）、Sublime Text |
| C# IDE | Visual Studio（Windows）/ Rider（跨平台，社区版非商用免费） | VS Code（需自行配构建） |
| 贴图 | GIMP / Krita | Photoshop（Art Source 官方是 PSD） |
| 矢量图 | Inkscape | Illustrator |
| 音频 | Audacity | — |
| 调试 | Rider + RimWorld Development Environment 插件 | RimWorld-Doorstop、RimWorld4Debugging |

> 普通 .NET 调试器**无法附加**到 RimWorld——游戏用的是 Unity 自己的 Mono 运行时分支，
> 必须借上面几个第三方方案之一。

**外部资源**：Artstyle 指南（<https://spdskatr.github.io/RWModdingResources/artstyle>）、
Roxxploxx modding 教程集（<https://github.com/roxxploxx/RimWorldModGuide/wiki>）、
RimWorld Modding Resources 资源中心（<https://spdskatr.github.io/RWModdingResources/>）。
