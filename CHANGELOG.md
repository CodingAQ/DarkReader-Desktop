# Changelog

本项目所有重要变更均记录于此文件。

格式参考 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，版本号遵循 [Semantic Versioning](https://semver.org/lang/zh-CN/)。

## [Unreleased]

### 修复

- **多窗口模式下“已关闭”窗口列表的一系列异常**：窗口关闭事件通过 `BeginInvoke` 异步派发到 UI 线程，与打开“选择窗口”菜单的时机存在竞争，可能导致 `_targetWindows`/`_closedWindowTitles` 状态不一致（窗口重新打开后暗色模式不生效，甚至在特定时序下菜单事件处理抛出未捕获异常导致程序崩溃）。修复：打开窗口列表菜单时先自校正失效的窗口句柄（同步移入“已关闭”列表），重连逻辑抽取为可复用的原子操作，并新增后台定时扫描以在窗口重新打开时自动恢复暗色模式（无需手动打开菜单）；同时为窗口菜单相关事件处理增加异常保护，避免个别异常导致整个程序崩溃
- **窗口模式在全屏游戏运行时失效**：游戏使用 `WS_EX_TOPMOST` 样式导致 Z-order 始终位于目标窗口上方，区域计算将游戏减去后目标区域几乎为空。修复：跳过 `WS_EX_TOPMOST` 窗口，覆盖层覆盖整个目标窗口，游戏绘制在覆盖层之上不会被暗化
- **目标窗口边缘白边**：`GetWindowRect` 返回的矩形包含 Windows 10/11 不可见扩展边框。改用 `DwmGetWindowAttribute(DWMWA_EXTENDED_FRAME_BOUNDS)` 获取准确可见边框
 - **Active On Startup 修复**：此前无论是否勾选，启动时都会激活暗色模式。修复后仅在勾选时才激活，但关闭前的状态始终保存

### 新增

- **多窗口模式**：可同时选择多个目标窗口应用暗色滤镜，目标窗口关闭后，状态保留，重新打开后自动恢复暗色模式
- **自定义帧率**：可在 5-60 fps 之间调整控制刷新帧率

### 测试

- 新增 7 个诊断测试项目（Test01-Test07），用于逐层排除 Magnification API、SetWindowRgn、区域算法、窗口枚举等问题

## [1.0.0] - 2026-07-18

首次发布。基于 [NegativeScreen](https://github.com/mlaily/NegativeScreen)（GPL-3.0，作者 mlaily）重新构建。

### 新增

- 迁移至 **.NET 8 / WinForms**（原项目为 .NET Framework 4.5）
- **窗口翻转模式**：可将色彩效果限定在指定窗口，窗口移动/缩放时自动跟随，窗口失焦时自动暂停
- **区域翻转模式**：可将色彩效果限定在屏幕指定矩形区域，通过全屏遮罩拖拽选择
- **全局快捷键**：
  - `Win+Alt+N` 切换开关
  - `Win+Alt+0~6` 切换 7 种色彩模式
  - `Win+Alt+R` 进入区域选择
  - `Win+Alt+H` 退出程序
- **单实例运行**：再次启动时向已有实例发送切换信号并自动退出
- **配置持久化**：设置自动保存到 `%AppData%\DarkReader\settings.json`，重启后恢复
- **应用图标**：自定义托盘图标
- **7 种色彩模式**：Default（简单反转）、Preset 1-5（智能反转）、Grayscale（灰度）

### 变更（相对上游 NegativeScreen）

- 技术栈：.NET Framework 4.5 → .NET 8
- UI 框架：WinForms 原生重构
- 发布方式：单文件自包含（原项目需安装 .NET Framework）
- 菜单交互：重构为更简洁的托盘菜单结构
- 配置存储：从 `negativescreen.conf` 改为 `settings.json`
- 快捷键：从 `Win+Alt+F1~F11` 改为 `Win+Alt+0~6` + `R` + `H`

### 移除（相对上游 NegativeScreen）

- Web API 功能（原项目内置 HTTP API 监听 8990 端口）
- 自定义配置文件语法（改用 JSON）
- Chocolatey 打包支持
- 多种额外色彩模式（Sepia、Red、Negative Red 等，仅保留 7 种核心模式）

[Unreleased]: https://github.com/CodingAQ/DarkReader-Desktop/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/CodingAQ/DarkReader-Desktop/releases/tag/v1.0.0
