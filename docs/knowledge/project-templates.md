# 项目模板与代码骨架

> 适用：新建 C# mod 时的目录/文件组织、日志工具、构建方式。
> 提示：用官方模板 `Rimworld-Mods/Template` 起步后，按本仓库约定增删。

## 1. 标准 C# mod 布局

```
NN-ModName/
├── About/About.xml
├── 1.6/Source/
│   ├── mod.csproj / mod.sln     # 项目文件（含 RootNamespace/AssemblyName）
│   ├── Main.cs                  # 入口：StaticConstructorOnStartup
│   ├── Logger.cs / LogUtility.cs# 日志封装（早期 mod 几乎人手一份）
│   └── <功能>.cs
└── 1.6/Assemblies/mod.dll       # 构建产物随源码提交
```

## 2. Logger.cs 骨架（广为复用的模板）

早期 mod（10、11、12、14、16、17、19、20、21、34 等 18+ 处）共享同一套
`Logger.cs`：把 `Verse.Log` 包一层，自动附带调用者文件/成员/行号。

见 [`10-QuestItemWatch/Source/Logger.cs`](../../10-QuestItemWatch/Source/Logger.cs)：

```csharp
public static class Log
{
    public static string prefix = "Logger";
    public static void Message(string msg,
        [CallerFilePath] string fileName = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Verse.Log.Message($"[ {prefix} {fileName}:{lineNumber} {memberName} ] " + msg);
    }
    // Warning / Error 同理
}
```

用法：`using Log = Logger.Log;` 后直接 `Log.Message("...")`。

## 3. 入口与 DefOf

见 [`10-QuestItemWatch/Source/Main.cs`](../../10-QuestItemWatch/Source/Main.cs)：

```csharp
[DefOf]                      // 让静态字段在启动时绑定 Def
public class TemplateDefOf
{
    public static LetterDef success_letter;
}
```

- `[StaticConstructorOnStartup]` + `Harmony.PatchAll`：见 harmony-patching.md。
- `[DefOf]`：静态引 Def，配合 XML 里的 Def 使用（名字需与 XML defName 对应）。

## 4. 教训：不要残留模板代码

- `18-HakuroXenohumanZh/Source/Main.cs` 残留了模板 `namespace Template`（翻译 mod 其实
  不需要 C# 源码）——复制模板后务必改 namespace 或删掉无用文件。
- `02-ItemPolicy`、`03-RecipeBook` 的 README 是空壳，且 03 误复制了 02 的标题
  （复制粘贴残留），重写时注意。

## 5. 现代写法（新 mod 参考 55）

`55-CommonModCompatibilityPatches` 使用较新的 C# 语法：文件级 `namespace`、
`new("id")`（目标类型 new）、`nameof` 等，见
[`55-CommonModCompatibilityPatches/1.6/Source/CommonModCompatibilityPatches.cs`](../../55-CommonModCompatibilityPatches/1.6/Source/CommonModCompatibilityPatches.cs)。

## 6. 相关文件

- 日志骨架：`10-QuestItemWatch/Source/Logger.cs`
- 入口/DefOf：`10-QuestItemWatch/Source/Main.cs`
- 构建命令：`cross-version-structure.md`
