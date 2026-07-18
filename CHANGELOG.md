# Changelog

本项目所有重要变更均记录于此文件。

格式参考 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，版本号遵循 [Semantic Versioning](https://semver.org/lang/zh-CN/)。

## [Unreleased]

## [1.0.0] - 2026-07-18

首次发布。基于 [NegativeScreen](https://github.com/mlaily/NegativeScreen)（GPL-3.0，作者 mlaily）重新构建。

### 新增

- 迁移至 **.NET 8 / WinForms**（原项目为 .NET Framework 4.5）
- **单文件自包含发布**：一个 `DarkReader.exe`（约 68MB）即可运行，无需安装 .NET Runtime
- **窗口翻转模式**：可将色彩效果限定在指定窗口，窗口移动/缩放时自动跟随，窗口失焦时自动暂停
- **区域翻转模式**：可将色彩效果限定在屏幕指定矩形区域，通过全屏遮罩拖拽选择
- **平滑过渡动画**：模式切换时 150ms 平滑过渡，避免突兀的闪烁
- **全局快捷键**：
  - `Win+Alt+N` 切换开关
  - `Win+Alt+1~6` 切换 6 种色彩模式
  - `Win+Alt+R` 进入区域选择
  - `Win+Alt+H` 退出程序
- **单实例运行**：再次启动时向已有实例发送切换信号并自动退出
- **配置持久化**：设置自动保存到 `%AppData%\DarkReader\settings.json`，重启后恢复
- **应用图标**：自定义托盘图标
- **6 种色彩模式**：Default（简单反转）、Preset 1-5（智能反转）、Grayscale（灰度）

### 变更（相对上游 NegativeScreen）

- 技术栈：.NET Framework 4.5 → .NET 8
- UI 框架：WinForms 原生重构
- 发布方式：单文件自包含（原项目需安装 .NET Framework）
- 菜单交互：重构为更简洁的托盘菜单结构
- 配置存储：从 `negativescreen.conf` 改为 `settings.json`
- 快捷键：从 `Win+Alt+F1~F11` 改为 `Win+Alt+1~6` + `R` + `H`

### 移除（相对上游 NegativeScreen）

- Web API 功能（原项目内置 HTTP API 监听 8990 端口）
- 自定义配置文件语法（改用 JSON）
- Chocolatey 打包支持
- 多种额外色彩模式（Sepia、Red、Negative Red 等，仅保留 7 种核心模式）

[Unreleased]: https://github.com/CodingAQ/DarkReader-Desktop/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/CodingAQ/DarkReader-Desktop/releases/tag/v1.0.0
