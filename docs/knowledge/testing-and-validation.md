# 测试与校验（白盒测试 + 单元测试）

> 来源 mod：54、55-Common、68、70、72、73、77
> 适用：RimWorld mod 难以在游戏外实例化运行时对象，如何做可自动化的回归测试。

## 1. 为什么需要"白盒测试"

RimWorld 的大多数运行时对象必须运行在主游戏线程内，外部单元测试 runner 无法安全
实例化。仓库的解法是 **Python 静态白盒测试**：直接检查源码与 XML 结构（正则/解析），
不运行游戏。

## 2. Python 白盒测试模式

统一模式：`Tests/whitebox/test_<mod>_static.py` + `Tests/run_whitebox.sh`。

```python
class XmlTests(unittest.TestCase):
    def test_all_xml_files_parse(self) -> None:
        for path in ROOT.rglob("*.xml"):
            ET.parse(path)                      # 所有 XML 可解析

    def test_designation_category_contains_known_designators(self) -> None:
        tree = ET.parse(COMMON / "Defs" / ".../DesignationCategoryDefs.xml")
        values = {node.text for node in tree.findall(".//specialDesignatorClasses/li")}
        self.assertIn("AAT.Designator_Allow", values)   # 关键 Def 存在且内容正确
```

见 [`54-AnotherAllowTool/Tests/whitebox/test_aat_static.py`](../../54-AnotherAllowTool/Tests/whitebox/test_aat_static.py)
（其 docstring 明确说明了为何用白盒方式）。

白盒测试覆盖的 mod：54、55-Common、68、70、72、73、77。

## 3. C# 单元测试项目

对纯逻辑（不依赖游戏运行时）的部分，建立正规 C# 单测项目：
- `72-RimLocksmith`：`tests/RimLocksmith.Tests/`
- `73-UsefulStats`：`tests/UsefulStats.Tests/`
- `77-KillingReward`：`Tests/unit/`（4 个 C# 单测 + csproj）

## 4. 原生代码测试（70-LinuxImeFix）

`70-LinuxImeFix` 涉及原生 C（IME 信号格式），配了
`tests/source_invariant_tests.py` + `native_whitebox_tests.c` + `run_whitebox_tests.sh`。
信号格式文档见 [`70-LinuxImeFix/IBUS_SIGNAL_FORMAT.md`](../../70-LinuxImeFix/IBUS_SIGNAL_FORMAT.md)。

## 5. 建议的新 mod 测试基线

1. 至少一个 `test_*_static.py`：校验所有 XML 可解析 + 关键 Def/Designator 存在。
2. 纯 C# 逻辑（数值、状态机、算法）抽成可单测的类，放 C# 单测项目。
3. 用一个 `.sh`（Windows 下用 PowerShell 或直接 `python -m unittest`）统一入口。

## 6. 相关文件

- 白盒样例：`54-AnotherAllowTool/Tests/whitebox/test_aat_static.py`
- C# 单测：`73-UsefulStats/tests/UsefulStats.Tests/`、`77-KillingReward/Tests/unit/`
- 原生测试：`70-LinuxImeFix/tests/`
