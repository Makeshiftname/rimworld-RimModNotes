# 跨版本结构（1.4 / 1.5 / 1.6 + Common/）

> 适用：新写 mod 时如何组织多版本目录、共享资源与发布物。
> 数据来源：`scan_mods.py` 对全部 79 个 mod 的目录扫描。

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
- 新 mod（编号 ≥54）：**只做 1.6**（54–77 全部）。
- 少数做满三版本（01、02、05、14、16、17、22、26）或 1.4+1.5（09、24、27、28）。

> 提示：若只支持最新版本，可省去多版本目录，直接 `1.6/` + `Common/`。

## 3. LoadFolders.xml 的演进

- 早期做法：`LoadFolders.xml` 显式把 `v1.6 → / + 1.6` 等映射到版本目录
  （仅 01、55-Common、77 在用）。
- 现仓库倾向：**弃用 LoadFolders，改用 `Common/` + 版本目录** 的隐式约定。
- 遗留：30、54 保留了 `LoadFolders.xml.bak`（曾用后被停用），可见迁移痕迹。

## 4. 构建命令

C# mod 在版本目录下构建（`mod.csproj` 位于 `Source/`）：

```bash
cd 1.6/Source && dotnet build -c Release
```

产物 `bin/Release/.../mod.dll` 复制到 `1.6/Assemblies/` 随源码提交（55 的 AGENTS.md 约定：
源码改动后必须重新编译并连同 DLL 一起提交）。

## 5. 相关文件

- 多版本 + Common 的例子：`收集/01-standalone-AlertUtility/`（1.4/1.5/1.6 + LoadFolders）、
  `收集/30-xml-SilencedToxifierGenerator/`（纯 Common/）、`收集/54-standalone-AnotherAllowTool/`（1.6 + Common/）
- 构建与提交约定：`收集/55-patch-CommonModCompatibilityPatches/AGENTS.md`
- 构建命令示例：`收集/76-standalone-SmoothDragSelect/README.md`

## 相关主题

- XML 共享目录：`xml-defs-and-patches.md`
- 测试（版本目录内 Source 的结构化校验）：`testing-and-validation.md`
