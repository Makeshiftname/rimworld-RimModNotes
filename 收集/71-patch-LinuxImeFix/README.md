# Linux IME Fix Linux 输入法修复 Notes

## 一句话定位
修复 Linux 下 RimWorld 文本框无法用 IBus 输入法（含 Rime）的问题，C# + 原生 C 双层架构。

## 关键要点
- **C# `NativeBridge`**：`dlopen/dlsym` 加载 `libLinuxImeFixNative.so`，绑定约 15 个导出（`rimworld_ime_init/process_key/poll_utf8/get_preedit/...`）；`Assembly.Location` 为空（如 PurePatcher 内存加载）时回退搜索 `ModContentPack` 目录。
- **候选窗**：`CandidateWindowRenderer`（`GUI.depth=-10000` 顶层、光标跟随 `CaretXOffset`、UI 缩放、屏幕边缘处理）。
- **原生 C** `linux_ime_native.c`：libdbus-1 连 IBus（地址读 `~/.config/ibus/bus/*`，支持 Wayland）、创建 InputContext、解析 `CommitText/UpdatePreeditText(WithMode)/UpdateLookupTable` 等 D-Bus signal、UTF-8 安全截断、`dbus_send_no_reply` 非阻塞。
- `IBUS_SIGNAL_FORMAT.md`：精确记录线上 wire format（IBusText `(s a{sv} s (s a{sv} av))`、IBusLookupTable 候选串在 `[7][i][2]`），附抓到的 "ni" 实例。
- Harmony patch 接管 IMGUI TextField/TextArea 按键，支持 Backspace 等组词编辑键、保留 late commit。
- 依赖 Harmony；仅 Linux；仓库中**唯一需要原生编译（.c→.so）的 mod**。

## 目录结构
```
收集/71-patch-LinuxImeFix/
├── About/About.xml
├── IBUS_SIGNAL_FORMAT.md     # 信号格式文档
├── 1.6/Source/               # Main.cs、linux_ime_native.c
└── tests/                    # test_ime.c、test_signals.py、run_whitebox_tests.sh
```

## 构建（需要原生编译）
```
cd 1.6/Source && dotnet build -c Release          # C#
cc -shared -fPIC -o libLinuxImeFixNative.so linux_ime_native.c -ldbus-1   # 原生 C
```

## 相关文件
- `IBUS_SIGNAL_FORMAT.md` — 线上信号格式
- `tests/` — 白盒 + 原生 C 测试（含真实 IBus 信号捕获）
