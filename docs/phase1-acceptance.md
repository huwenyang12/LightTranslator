# LightTranslator Phase 1 验收记录

验收日期：2026-09-15
验收分支：`feature/phase1-polish`
目标平台：Windows x64
技术环境：.NET 8、WPF

## 自动化验证

- `dotnet test -c Release`
  - 通过：134
  - 失败：0
  - 跳过：0
- `dotnet build -c Release`
  - 警告：0
  - 错误：0

## 功能验收

- 首次启动设置流程正常。
- DeepSeek API Key 验证与保存正常。
- 后续启动直接进入系统托盘。
- 托盘菜单包含文本翻译、截图翻译、设置和退出。
- 文本翻译支持托盘菜单与全局快捷键唤起。
- 翻译窗口支持自动聚焦、实时翻译和请求取消。
- Enter 可复制译文并关闭窗口。
- Shift+Enter 可输入换行。
- 复制按钮可复制译文并保持窗口打开。
- Esc 可关闭翻译窗口。
- 文本翻译语言配置能够持久保存。
- 文本翻译快捷键能够修改并持久保存。
- 开机启动配置能够正确写入和移除。
- 截图翻译入口能够显示阶段提示窗口。
- 托盘退出后应用进程完全结束。
- 应用、托盘、窗口及可执行文件图标显示一致。

## 数据与安全验证

- 常规设置保存在本地设置文件中。
- API Key 使用 Windows DPAPI 加密保存。
- 设置文件中不包含 API Key。
- 日志中不记录翻译内容和 API Key。

## 发布验证

发布配置：

- Release
- `win-x64`
- Self-contained
- Single-file managed application
- WPF 原生运行依赖随目录分发

发布版已完成启动、托盘菜单、快捷键、文本翻译、截图入口、设置窗口和退出测试。

分发包：

`LightTranslator-Phase1-win-x64-20260915-154933.zip`

文件大小：

`65,854,968 bytes`

SHA-256：

`A0BC7EF4B9FE9273F198B27393FBC43130CC10DF26320C2A7B793552AC86435E`
