# DarkReader

[![License: GPL-3.0](https://img.shields.io/badge/License-GPL--3.0-blue.svg)](./LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2B-blue.svg)](#系统要求)
[![Release](https://img.shields.io/github/v/release/CodingAQ/DarkReader-Desktop)](https://github.com/CodingAQ/DarkReader-Desktop/releases)

**[English](./README.en.md)** | 简体中文

> **致谢**：本项目基于 [NegativeScreen](https://github.com/mlaily/NegativeScreen)（作者 [mlaily](https://github.com/mlaily)，GPL-3.0）改进而成。
> 主要改进：新增窗口翻转、区域翻转、托盘菜单交互重构等。
> [GPL-3.0](./LICENSE) License.

## 简介

DarkReader-Desktop 是一个 Windows 系统托盘应用，将整个屏幕或指定区域通过反色切换为深色模式，使用 Windows 内置的 Magnification API（合成器级色彩矩阵）。

## 截图

| Before | After |
|--------|-------|
| <img src="docs/screenshot-before.png" alt="Before" style="zoom:75%;" /> | <img src="docs/screenshot-dark-mode.png" alt="After" style="zoom:75%;" /> |



## 特性

- **多种预设色彩模式**：默认（简单反色） + 预设1-5 + 灰度模式
- **跟随窗口**：将效果限定在指定窗口
- **自定义区域**：将效果限定在指定矩形区域

## 下载与安装

1. 前往 [Releases](https://github.com/CodingAQ/DarkReader-Desktop/releases) 下载最新发布包（分打包依赖和不打包依赖的版本）
2. 解压到任意目录
3. 双击 `DarkReader.exe` 启动

## 操作方式

### 托盘图标

| 操作 | 效果 |
|------|------|
| **左键**托盘图标 | 切换深色模式开关 |
| **右键**托盘图标 | 打开菜单 |

### 菜单选项

- **Toggle** — 切换深色模式开关
- **Mode**（推荐**配置3**）
  - **Default** — 简单色彩反转
  - **Preset 1-5** — 5 种配置反转（保留色相）
  - **Grayscale** — 灰度
- **Select Region** — 选择区域
- **Clear Region** — 清除选择区域
- **Select Window** — 选择目标窗口
- **Clear Window Target** — 清除窗口目标
- **Pause When Not Foreground** — 窗口不在前台时自动暂停（推荐启用）
- **Active On Startup** — 启动时启用
- **Exit** — 退出程序

### 全局快捷键

| 快捷键 | 功能 |
|--------|------|
| `Win + Alt + N` | 切换深色模式开关 |
| `Win + Alt + 0` | 切换到默认配置（Default） |
| `Win + Alt + [1-5]` | 切换到配置1-5（Preset 1-5） |
| `Win + Alt + 6` | 切换到灰度模式（Grayscale） |
| `Win + Alt + R` | 选择深色模式的区域 |
| `Win + Alt + H` | 退出程序 |

### 窗口翻转

DarkReader 支持将翻转效果只应用到指定窗口：

| 操作 | 效果 |
|------|------|
| 菜单 → **Select Window** → 选择窗口 | 从列表中选择目标窗口 |
| 菜单 → **Clear Window Target** | 清除窗口目标，恢复全屏区域 |

### 区域翻转

DarkReader 支持将翻转效果限制在屏幕的指定区域：

| 操作 | 效果 |
|------|------|
| 菜单 → **Select Region** （`Win + Alt + R`） | 进入区域选择模式 |
| 菜单 → **Clear Region** | 清除选择区域，恢复全屏区域 |



## 7 种色彩模式说明

| 模式 | 说明 |
|------|------|
| **Default** | 简单 RGB 反转，效果强烈，适合高对比度需求 |
| **Preset 1** | 理论最优变换（Tom MacLeod 方案），色彩准确但可能过饱和 |
| **Preset 2** | 最简洁的 180° 色相偏移，高饱和度，纯色表现好 |
| **Preset 3** | 整体去饱和，黄色和蓝色偏暗，适合长时间阅读 |
| **Preset 4** | 高饱和度，黄色和蓝色偏暗，可读性较好 |
| **Preset 5** | 中等饱和度，CMY 色彩轻微去饱和，颜色自然 |
| **Grayscale** | 全局灰度模式，所有像素按亮度转为灰度，黑色保持深色调 |



## 系统要求

- Windows 10/11 64位
- `*_Framework.exe` 需安装 .NET 8 Runtime 或更高版本

## 从源码构建

需要 [.NET 8 SDK](https://dotnet.microsoft.com/download) 或更高版本。

```bash
# 克隆仓库
git clone https://github.com/CodingAQ/DarkReader-Desktop.git
cd DarkReader-Desktop
```

## 故障排除

| 问题 | 解决方案 |
|------|----------|
| 启动后屏幕无变化 | 确保 Windows DWM/Aero 已启用 |
| 快捷键无响应 | 检查是否有其他程序占用了相同快捷键 |
| 托盘图标不显示 | 检查 Windows 通知区域设置，确保 DarkReader 未被隐藏 |
| 退出后屏幕仍反转 | 重启程序并按 `Win + Alt + N` 关闭，或重启 Windows |


## 许可证

本项目基于 [GPL-3.0](./LICENSE) 许可证发布。使用、修改、分发请遵循该许可证条款。
