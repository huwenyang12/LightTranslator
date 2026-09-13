# LightTranslator V1 需求与架构设计

日期：2026-09-13

## 1. 项目目标

LightTranslator 是一个面向 Windows 10/11 的轻量级桌面翻译工具，核心目标是：

1. 通过全局快捷键快速完成文本翻译。
2. 通过截图框选完成类似微信截图翻译的“原位置翻译覆盖”。
3. 平时后台静默运行，不打断用户当前工作。
4. OCR 尽量本地完成，截图不上传。
5. V1 保持轻量，不加入历史记录、账号体系、云同步等非核心功能。

---

## 2. 技术栈

- 开发语言：C#
- 运行时：.NET 8
- 桌面框架：WPF
- 目标系统：Windows 10 / Windows 11
- OCR：PaddleOCR 模型 + ONNX Runtime
- 图像处理：OpenCvSharp / 必要时配合 SkiaSharp 或 WPF Bitmap API
- 翻译服务：DeepSeek API
- 本地配置：JSON
- 敏感信息保护：Windows DPAPI
- 全局快捷键：Windows RegisterHotKey
- 系统托盘：Windows 托盘能力
- 日志：滚动日志文件，不记录用户翻译正文

---

## 3. V1 核心功能

### 3.1 文本输入翻译

默认快捷键：

- Alt + T：打开/关闭文本翻译窗口

交互流程：

1. 按下 Alt + T。
2. 在当前鼠标所在显示器中央偏上位置打开轻量翻译窗口。
3. 自动聚焦输入框。
4. 用户输入文字。
5. 停止输入约 400ms 后自动调用 DeepSeek 翻译。
6. 显示译文。
7. Enter：复制当前译文到剪贴板并关闭窗口。
8. Shift + Enter：输入换行。
9. Esc：关闭窗口，不复制。
10. 再次按 Alt + T：关闭窗口。

补充规则：

- 翻译请求采用防抖。
- 新输入产生时，取消上一轮未完成翻译请求。
- 用户点击复制按钮时只复制，不关闭窗口。
- 不自动读取剪贴板。
- 窗口关闭后不保存输入正文或译文。

语言行为：

- V1 开放：中文、English、日本語。
- 源语言允许“自动检测”。
- 目标语言不允许“自动检测”。
- 文本翻译单独保存自己的语言偏好。
- 语言选择记住上一次状态。
- 提供交换按钮。
- 当源语言为自动检测时，交换前使用最近一次实际检测语言。

---

### 3.2 截图区域翻译

默认快捷键：

- Alt + Q：进入截图翻译 / 取消当前截图翻译状态

完整流程：

1. 按下 Alt + Q。
2. 判断鼠标当前所在显示器。
3. 截取该显示器当前画面。
4. 用全屏 CaptureWindow 显示冻结画面。
5. 屏幕整体暗化，鼠标变成十字光标。
6. 用户拖拽框选翻译区域。
7. 松开鼠标确认选区。
8. CaptureWindow 关闭。
9. 原选区位置显示轻量 Loading Overlay。
10. 对选区 Bitmap 执行本地 OCR。
11. 获得文字块和坐标。
12. 对文字块逐块调用 DeepSeek 翻译。
13. BackgroundCleaner 擦除原文字。
14. TranslationRenderer 将译文重新绘制回原位置。
15. 生成最终翻译 Bitmap。
16. OverlayWindow 在原框选位置显示翻译后的静态画面。
17. 点击覆盖层、按 Esc、再次按 Alt + Q 都可以关闭。

V1 不允许跨显示器框选。

支持多显示器以及不同 DPI 缩放比例。

任何时候只允许存在一个截图翻译任务。

再次按 Alt + Q 时：

- 取消当前 OCR / 翻译 / 渲染任务。
- 关闭 Capture、Loading 或 Overlay。
- 恢复空闲状态。

---

## 4. 截图框选视觉规范

CaptureWindow：

- 仅覆盖鼠标当前所在显示器。
- 展示截图冻结画面，而不是直接操作实时桌面。
- 未选区域添加半透明暗色遮罩。
- 选区保持正常亮度。
- 十字光标。
- 选区使用简洁高亮边框。
- 拖动时显示宽 × 高尺寸。
- Esc 取消。
- 不增加复杂工具栏。
- 风格保持类似微信截图：干净、直接、低干扰。

---

## 5. 翻译结果 OverlayWindow

OverlayWindow 只负责显示已经生成好的翻译 Bitmap，不负责 OCR、翻译或图像处理。

窗口属性：

- 无标题栏
- 无边框
- TopMost
- 不出现在任务栏
- 不允许拖动
- 不允许缩放
- 位置严格对应原框选区域
- 尺寸严格对应原框选区域

关闭方式：

- 鼠标左键点击覆盖区域
- Esc
- Alt + Q

动画：

- 不做复杂动画。
- 最多允许约 100~150ms 的轻微淡入。

异常：

- OCR 未识别到有效文字：不显示最终 Overlay，短暂提示“未识别到可翻译文字”。
- 翻译失败：关闭 Loading，提示错误，不留下透明死窗口。

---

## 6. OCR 设计

OCR 方案：

- PaddleOCR 模型
- ONNX Runtime
- 运行于 C# 进程内
- 不依赖 Python

模型策略：

- 中文 / 英文模型随安装包内置。
- 日语模型按需下载。
- 未来新增语言继续采用按需模型机制。
- 模型下载完成后 OCR 全程本地运行。
- 截图永不发送给 OCR 云服务。

OCR 输出核心结构：

OcrBlock：

- Text：原文字
- Confidence：OCR 置信度
- BoundingBox：文字区域
- SourceLanguage：识别/检测语言
- TranslatedText：译文
- TextColor：原文字颜色估算
- BackgroundColor：背景颜色估算
- BackgroundType：背景复杂度
- FontSize：字号估算
- FontWeight：字重估算
- Alignment：对齐方式
- RenderRect：最终译文绘制区域

职责原则：

- OCR 只负责“文字是什么、在哪里”。
- 不负责翻译。
- 不负责擦背景。
- 不负责最终绘制。

---

## 7. OCR 结果过滤

V1 对 OCR 结果进行基础过滤。

默认跳过：

- 低置信度结果
- 空白
- 纯符号
- 只有数字
- URL
- 邮箱地址
- 极小文字块

正常中英日文本进入翻译流程。

后续可以增加高级规则，但 V1 不做复杂语义分类。

---

## 8. 翻译设计

统一接口：

ITranslationService

V1 实现：

DeepSeekTranslationService

架构允许未来新增：

- Google
- Microsoft
- OpenAI / DeepSeek 类大模型
- 其他服务

UI、OCR 和截图流程只依赖 ITranslationService，不依赖具体供应商。

翻译策略：

- 文本翻译：整段翻译。
- 截图翻译：每个 OcrBlock 单独翻译。
- V1 不做段落智能合并。
- 多个 OcrBlock 可限制并发翻译，建议并发数 3~5。
- 单个 Block 翻译失败不影响其他 Block。
- 失败 Block 保留原文。

---

## 9. 背景清理 BackgroundCleaner

BackgroundCleaner 只负责删除截图中的原文字。

策略：

### 简单背景

当区域背景是纯色或近似纯色：

- 采样周围背景颜色。
- 使用近似背景色覆盖文字区域。

### 复杂背景

当区域存在：

- 图片
- 渐变
- 纹理
- 复杂 UI

使用 OpenCV Inpainting。

原则：

- 不粗暴擦除整个 OCR BoundingBox。
- 尽量构建文字 Mask。
- 避免破坏按钮边框、图标、线条和其他 UI 元素。

接口保持独立，未来可以替换更高级背景修复算法。

---

## 10. TranslationRenderer

TranslationRenderer 只负责把译文绘制回处理后的 Bitmap。

渲染优先级：

1. 保持原文字位置。
2. 保持原文字颜色。
3. 尽量保持原字号。
4. 保持原对齐方式。
5. 自动换行。
6. 放不下时逐级缩小字号。
7. 达到最小字号后仍放不下，才允许小范围扩大 RenderRect。

字号：

- 根据 OCR 文字框高度估算。
- 不做真正字体识别。
- 最小字号建议不低于原字号约 65%。

文字区域扩展：

- 必须检测是否与其他 OcrBlock 冲突。
- 冲突时优先缩字号和重新换行。
- 不允许无限扩展。

字体原则：

- 不追求 100% 还原原字体。
- 中文优先使用系统中文字体。
- 英文优先使用 Segoe UI。
- 日文优先使用系统日文字体。
- 优先保证位置、字号、颜色、完整性和可读性。

---

## 11. 程序架构

V1 使用单个 WPF 项目，不拆多个 csproj。

推荐目录：

```text
LightTranslator/
│
├─ App.xaml
├─ App.xaml.cs
│
├─ Views/
│  ├─ TranslateWindow.xaml
│  ├─ CaptureWindow.xaml
│  ├─ OverlayWindow.xaml
│  ├─ SettingsWindow.xaml
│  └─ WelcomeWindow.xaml
│
├─ ViewModels/
│  ├─ TranslateViewModel.cs
│  └─ SettingsViewModel.cs
│
├─ Services/
│  ├─ Translation/
│  │  ├─ ITranslationService.cs
│  │  └─ DeepSeekTranslationService.cs
│  ├─ Ocr/
│  │  ├─ IOcrService.cs
│  │  └─ PaddleOcrService.cs
│  ├─ Capture/
│  │  └─ ScreenshotService.cs
│  ├─ Rendering/
│  │  ├─ BackgroundCleaner.cs
│  │  └─ TranslationRenderer.cs
│  ├─ Hotkeys/
│  │  └─ HotkeyService.cs
│  ├─ Windows/
│  │  └─ WindowManager.cs
│  └─ Settings/
│     └─ SettingsService.cs
│
├─ Models/
│  ├─ OcrBlock.cs
│  ├─ TranslationResult.cs
│  ├─ LanguageOption.cs
│  └─ AppSettings.cs
│
├─ Infrastructure/
│  ├─ Windows/
│  │  ├─ NativeMethods.cs
│  │  ├─ DpiHelper.cs
│  │  └─ MonitorHelper.cs
│  └─ Security/
│     └─ SecretStorage.cs
│
├─ Resources/
│  ├─ Icons/
│  └─ Fonts/
│
└─ ModelsData/
   └─ OCR/
```

设计原则：

- Window 只负责显示与交互。
- ViewModel 负责页面状态。
- Service 负责业务能力。
- Windows 原生 API 封装在 Infrastructure/Windows。
- 各模块通过接口隔离。
- 不把 OCR、DeepSeek、截图、配置逻辑写入 Window Code-Behind。

---

## 12. AppController 与生命周期

App.xaml.cs 启动后创建统一 AppController。

AppController 协调：

- TrayService
- HotkeyService
- SettingsService
- TranslationService
- OcrService
- WindowManager

窗口之间不直接互相调用。

快捷键和托盘菜单统一调用 WindowManager / AppController。

例如：

Alt + T
→ AppController
→ WindowManager.ToggleTranslateWindow()

Alt + Q
→ AppController
→ WindowManager.StartCapture()

---

## 13. 启动与系统托盘

首次启动：

1. 显示 WelcomeWindow。
2. 配置 DeepSeek API Key。
3. 测试 API。
4. 设置默认语言。
5. 完成后进入系统托盘。

后续启动：

- 不显示主窗口。
- 直接加载配置。
- 注册快捷键。
- 创建托盘图标。
- 后台静默运行。

托盘菜单：

- 文本翻译
- 截图翻译
- 截图翻译语言
- 设置
- 退出

---

## 14. 快捷键

默认：

- Alt + T：文本翻译
- Alt + Q：截图翻译

允许用户修改。

使用 RegisterHotKey。

规则：

- 注册成功后才保存新快捷键。
- 快捷键被占用时明确提示。
- 原有效快捷键继续保留。

---

## 15. 设置系统

本地目录：

```text
%LocalAppData%\LightTranslator\
│
├─ settings.json
├─ logs\
└─ models\
   └─ ocr\
```

settings.json 保存：

- 文本翻译语言
- 截图翻译语言
- 文本快捷键
- 截图快捷键
- 翻译供应商
- OCR 模型状态
- 首次启动状态
- 开机启动状态

不保存：

- 用户翻译正文
- OCR 正文
- 译文
- 截图
- API Key 明文

---

## 16. DeepSeek API Key 安全

API Key：

- 用户在设置页填写。
- 不写死在程序代码。
- 不明文写入 settings.json。
- 使用 Windows DPAPI 绑定当前 Windows 用户加密保存。

开发阶段可以使用测试 Key，但最终发布必须走安全存储。

---

## 17. OCR 模型管理

单独实现 OcrModelManager。

职责：

- 检查模型是否存在
- 下载按需模型
- 校验 SHA256
- 返回模型路径
- 管理模型版本

日语首次使用：

1. 检查 ja 模型。
2. 未安装则提示用户下载。
3. 下载模型。
4. SHA256 校验。
5. 写入本地 models/ocr/ja。
6. 后续全部离线运行。

下载失败不影响已安装语言。

---

## 18. 日志

采用滚动日志。

示例：

```text
logs/
├─ lighttranslator-2026-09-13.log
└─ lighttranslator-2026-09-14.log
```

记录：

- 程序启动
- 快捷键注册
- OCR 耗时
- OCR Block 数量
- DeepSeek 请求耗时
- Overlay 生成耗时
- 模型下载
- 异常堆栈

禁止记录：

- 用户输入正文
- OCR 识别正文
- 翻译正文
- 截图
- API Key

---

## 19. 异常处理

DeepSeek：

- 网络超时 → “翻译请求超时”
- API Key 无效 → “DeepSeek API Key 无效”
- 配额不足 → “DeepSeek API 额度不足”

OCR：

- 模型不存在 → 引导下载
- 模型加载失败 → 提示重新下载
- 无有效文字 → “未识别到可翻译文字”

快捷键：

- 注册失败 → 提示快捷键被占用

截图翻译：

无论 Capture、OCR、翻译还是渲染在哪一步失败，都必须：

1. 关闭 Loading Overlay。
2. 释放截图任务状态。
3. 恢复程序空闲。
4. 不允许残留不可关闭的透明窗口。

所有长任务支持 CancellationToken。

---

## 20. 隐私原则

V1：

- 截图 OCR 全程本地。
- 截图不上传 DeepSeek。
- DeepSeek 只接收需要翻译的文本。
- 不保存翻译历史。
- 不保存截图。
- 不在日志中记录正文。
- API Key 加密保存。

---

## 21. V1 明确不做

以下功能不进入 V1：

- 翻译历史
- 截图历史
- 用户账号
- 云同步
- AI 聊天
- OCR 云服务
- 复杂字体识别
- AI 图像修复
- 跨屏框选
- 段落智能合并
- 多翻译供应商同时上线
- 自动读取剪贴板
- macOS / Linux

---

## 22. V1 成功标准

### 文本翻译

- Alt + T 能稳定打开/关闭。
- 输入停止约 400ms 自动翻译。
- Enter 可以复制译文并关闭。
- Shift + Enter 可换行。
- 翻译请求不会发生旧结果覆盖新结果。

### 截图翻译

- Alt + Q 可以在鼠标所在显示器稳定框选。
- 100%、125%、150% DPI 下坐标准确。
- OCR 能返回文字及位置。
- 原文字可以被合理清理。
- 译文基本保持原位置、颜色和布局。
- Overlay 精准覆盖原选区。
- Esc / 点击 / Alt + Q 均可退出。
- 异常时不残留 Overlay。

### 性能目标

理想体验：

- 文本翻译：用户停止输入后尽快返回。
- 截图 OCR + 翻译 + 渲染：常规短文本区域尽量在约 1~2 秒级完成。
- UI 操作不得被 OCR 或网络请求阻塞。

---

## 23. 推荐开发顺序

1. 创建 .NET 8 WPF 项目。
2. 搭建目录与基础依赖注入/服务结构。
3. AppController + WindowManager。
4. 托盘。
5. HotkeyService。
6. TranslateWindow 静态 UI。
7. TranslateWindow 交互与 400ms 防抖。
8. ITranslationService + DeepSeek。
9. 设置与 DPAPI。
10. CaptureWindow。
11. 多显示器与 DPI。
12. ScreenshotService。
13. IOcrService + PaddleOCR ONNX。
14. OcrBlock。
15. BackgroundCleaner。
16. TranslationRenderer。
17. Loading Overlay。
18. OverlayWindow。
19. OcrModelManager。
20. 日志与统一异常处理。
21. 完整集成测试。
22. UI 微调。
23. 打包安装程序。

---

## 24. 最终产品路径

LightTranslator V1 的核心产品逻辑：

```text
Alt + T
→ 输入
→ 自动翻译
→ Enter
→ 复制并关闭
```

以及：

```text
Alt + Q
→ 框选
→ 本地 OCR
→ DeepSeek
→ 原位置翻译覆盖
→ 点击 / Esc / Alt + Q 退出
```

产品目标不是做一个“大而全的翻译平台”，而是做一个尽量不打断用户当前工作的 Windows 快捷翻译工具。
