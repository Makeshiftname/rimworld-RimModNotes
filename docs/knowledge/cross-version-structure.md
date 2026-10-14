# 跨版本结构（1.4 / 1.5 / 1.6 + Common/）

> 适用：新写 mod 时如何组织多版本目录、共享资源与发布物。
> 数据来源：`scan_mods.py` 对全部 85 个 mod 目录的扫描。

## 1. 目录布局

```
NN-ModName/
├── About/About.xml        # 元数据（packageId/name/supportedVersions/依赖）
├── Common/                # 跨版本共享：Defs/、Patches/、Languages/
├── 1.4/   (或 1.5/ 1.6/)  # 该版本特有：Source/、Assemblies/、Defs/、Patches/
│   ├── Source/            # C# 源码 + mod.csproj
│   └── Assemblies/        # 编译产物 .dll（随源码一起提交）
└── README.md
```

**`Common/` 是跨版本共享枢纽**：绝大多数多版本/纯 XML mod 把共享的 Defs、Patches、
Languages 放这里，版本目录只放该版本特有内容，避免重复维护（25、30、54 等均为此模式）。

## 2. 仓库内的版本覆盖规律（按编号演进）

- 老 mod（编号 ≤23）：多为单版本 **1.4**（07、08、10、11、13、18、19、20、21、23）。
- 中段（24–52）：覆盖 **1.5 + 1.6**（29、31、34、35、36、40、41、42、43、44、45、47、48、49）。
- 新 mod（编号 ≥54）：**只做 1.6**（54–78 全部）。
- 少数做满三版本（01、02、05、14、16、17、22、26）或 1.4+1.5（09、24、27、28）。

> 提示：若只支持最新版本，可省去多版本目录，直接 `1.6/` + `Common/`。

## 3. LoadFolders.xml 的演进

- 早期做法：`LoadFolders.xml` 显式把 `v1.6 → / + 1.6` 等映射到版本目录
  （仅 01、55、78 在用）。
- 现仓库倾向：**弃用 LoadFolders，改用 `Common/` + 版本目录** 的隐式约定。
- 遗留：30、54 保留了 `LoadFolders.xml.bak`（曾用后被停用），可见迁移痕迹。

## 4. 构建命令

C# mod 在版本目录下构建（`mod.csproj` 位于 `Source/`）：

```bash
cd 1.6/Source && dotnet build -c Release
```

产物 `bin/Release/.../mod.dll` 复制到 `1.6/Assemblies/` 随源码提交（55 的 AGENTS.md 约定：
源码改动后必须重新编译并连同 DLL 一起提交）。

### csproj 的 OutputPath 反推目录布局

上游 mod 的 csproj 常把产物路径写死，**它反过来告诉你在游戏目录里的正确层级**。
例如 84 号（`收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/RimTalkEventPlus.csproj`）：

```xml
<OutputPath>..\..\1.6\Assemblies\</OutputPath>   <!-- 相对 Source/RimTalkEventMemory/ -->
```

即工程放在 `<mod 根>/Source/<工程名>/`，产物落到 `<mod 根>/1.6/Assemblies/`。
**收纳第三方 mod 时按这个反推 DLL 该放哪，别照搬上游的 flat 习惯。**

### 没有预编译 DLL 的第三方 mod

部分上游**只发源码不提交产物**（`.gitignore` 里含 `*.dll`），此时本仓库也无 `Assemblies/`。
84 号就是这种情况（本机无 RimWorld 安装也无法补编）。处置：

- 保持上游 flat 布局不动，**不要凭空造版本目录**；
- 在 mod 的 README 里写全构建前置条件（游戏程序集 / 第三方 mod DLL / NuGet 包）与产物落点；
- `mod-index.md` 的 Assemblies 列显示 `–`，属于正常状态；
- 这类 mod 的价值在**源码学习**，不在可运行性。

### 声明版本 ≠ 有该版本产物

84 号的 `About.xml` 声明 `1.5 + 1.6`，但 csproj 只输出到 `1.6/Assemblies\`，
**没有任何 1.5 分支**。核对时要看三处是否一致：
`supportedVersions` × 实际输出目录 × 实际编译引用（csproj 里引用的游戏 mod DLL 路径）。

### 更好的做法：一份 csproj 模板化出两个版本（85 号）

85 号是目前本仓库最干净的多版本工程：

```xml
<PropertyGroup>
  <GameVersion Condition=" '$(GameVersion)' == '' ">1.5</GameVersion>   <!-- 可被命令行覆盖 -->
</PropertyGroup>
<PropertyGroup>
  <OutputPath>..\$(GameVersion)\Assemblies</OutputPath>                    <!-- 输出跟随版本 -->
</PropertyGroup>
```

```bash
dotnet build -c Release -p:GameVersion=1.5   # → ../1.5/Assemblies/
dotnet build -c Release -p:GameVersion=1.6   # → ../1.6/Assemblies/
```

关键配套是 `Krafs.Rimworld.Ref` 的版本号也引用同一个变量：

```xml
<PackageReference Include="Krafs.Rimworld.Ref" Version="$(GameVersion).*-*" PrivateAssets="All" />
```

**声明、输出、引用三者都由 `$(GameVersion)` 串起来，天然不会脱节。**

⚠️ 反例就在同一个文件里：`<Reference Include="RimTalk"><HintPath>..\..\RimTalk-main\1.5\Assemblies\RimTalk.dll</HintPath>`
**把第三方依赖的路径写死成 1.5**，所以 `-p:GameVersion=1.6` 编出来的仍是 1.5 的 RimTalk.dll。
**模板化必须同时覆盖输出路径和引用路径**，只做一半反而更隐蔽——目录名对了，依赖却是旧的。

### 引用第三方 mod 的 DLL：`Private=false` 是必须的

85 号的 csproj：

```xml
<Reference Include="RimTalk">
  <HintPath>..\..\RimTalk-main\1.5\Assemblies\RimTalk.dll</HintPath>
  <Private>false</Private>          <!-- 关键 -->
</Reference>
```

- **`Private=false`（CopyLocal=false）**：不把第三方 DLL 复制进自己 `Assemblies/`。
  对方 mod 自己会加载它，复制一份会造成**同一个类在进程里有两份类型身份**——
  `is` 判断、`as` 转换、静态字段全部对不上，是极难定位的诡异 bug。
  此时编译期能用到的第三方类型在运行时也不可直接引用，得靠
  `AppDomain.CurrentDomain.GetAssemblies()` 按名找（见
  [harmony-patching.md 第 9 节](harmony-patching.md)）。
- **`Private=true`（CopyLocal=true，默认）**：适用于公共库。85 号的 `Scriban` 就标了 `true`。
  ⚠️ 但它连 `Newtonsoft.Json.dll`（712 KB）也一起复制进了**两个** `Assemblies/` 目录。
  **把公共库 DLL 塞进 mod 的 `Assemblies/` 是版本冲突高发点**：玩家同时装了另一个携带不同版本
  Newtonsoft.Json 的 mod 时，加载顺序决定胜者，症状是莫名其妙的 `MissingMethodException`。

**记忆点：引用别人的 mod → `Private=false`；引用公共库 → 先确认版本协商策略，别无脑打包。**

## 5. 相关文件

- 多版本 + Common 的例子：`收集/01-standalone-AlertUtility/`（1.4/1.5/1.6 + LoadFolders）、
  `收集/30-xml-SilencedToxifierGenerator/`（纯 Common/）、`收集/54-standalone-AnotherAllowTool/`（1.6 + Common/）
- 构建与提交约定：`收集/55-patch-CommonModCompatibilityPatches/AGENTS.md`
- 构建命令示例：`收集/76-standalone-SmoothDragSelect/README.md`
- 无预编译 DLL 的收录实例：`收集/84-patch-RimTalkEventPlus/README.md`、`收集/84-patch-RimTalkEventPlus/Source/RimTalkEventMemory/RimTalkEventPlus.csproj`

## 相关主题

- XML 共享目录：`xml-defs-and-patches.md`
- 测试（版本目录内 Source 的结构化校验）：`testing-and-validation.md`
