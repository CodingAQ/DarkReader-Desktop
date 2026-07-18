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

DarkReader 是一个 Windows 系统托盘应用，一键将整个屏幕切换为深色模式。使用 Windows 内置的 Magnification API（合成器级色彩矩阵），零帧延迟、CPU 占用可忽略。

## 截图

<!-- TODO: 添加截图
![托盘菜单](docs/screenshot-tray.png)
![深色模式效果](docs/screenshot-dark-mode.png)
![区域选择](docs/screenshot-region.png)
-->

📷 截图待添加。

## 特性

- **一键切换**深色模式，零帧延迟（合成器级色彩矩阵）
- **7 种色彩模式**：Default + Preset 1-5 + Grayscale
- **窗口翻转**：效果限定在指定窗口，自动跟随/暂停
- **区域翻转**：效果限定在屏幕矩形区域
- **全局快捷键**，即使其他程序在前台也能触发
- **单文件发布**，无需安装 .NET Runtime
- **平滑过渡**：150ms 动画避免突兀闪烁
- **配置持久化**，重启后恢复上次状态
- **单实例运行**，二次启动切换开关

## 下载与安装

1. 前往 [Releases](https://github.com/CodingAQ/DarkReader-Desktop/releases) 下载最新发布包
2. 解压到任意目录
3. 双击 `DarkReader.exe` 启动
4. 程序最小化到系统托盘（右下角通知区域）

> **首次运行**：如果提示 Windows Aero/DWM 未启用，请点击"确定"继续（部分系统可能不需要 Aero）。

## 操作方式

### 托盘图标

| 操作 | 效果 |
|------|------|
| **左键单击** 托盘图标 | 切换 深色模式 开/关 |
| **右键单击** 托盘图标 | 打开模式菜单 |

### 菜单选项

- **Toggle** — 切换深色模式开关
- **Default** — 简单色彩反转（经典负片效果）
- **Preset 1-5** — 5 种智能反转（保留色相，视觉更舒适）
- **Grayscale** — 全局灰度（按亮度转换，黑色保持黑色调）
- **Select Region** — 选择翻转区域（当前区域显示在菜单中）
- **Clear Region** — 清除区域限制，恢复全屏
- **Select Window** — 从列表中选择目标窗口（自动跟随/调整/暂停）
- **Clear Window Target** — 清除窗口目标
- **Pause When Not Foreground** — 窗口不在前台时自动暂停（勾选启用）
- **Active On Startup** — 启动时自动开启暗色模式（勾选启用）
- **Exit** — 退出程序

### 全局快捷键

| 快捷键 | 功能 |
|--------|------|
| `Win + Alt + N` | 切换深色模式 开/关 |
| `Win + Alt + 1` | 切换到：Default |
| `Win + Alt + 2` | 切换到：Preset 1 |
| `Win + Alt + 3` | 切换到：Preset 2 |
| `Win + Alt + 4` | 切换到：Preset 3 |
| `Win + Alt + 5` | 切换到：Preset 4 |
| `Win + Alt + 6` | 切换到：Preset 5 |
| `Win + Alt + R` | 选择翻转区域 |
| `Win + Alt + H` | 退出程序 |

> Grayscale 模式只能通过菜单选择，未绑定快捷键。

### 窗口翻转

DarkReader 支持将翻转效果只应用到指定窗口：

| 操作 | 效果 |
|------|------|
| 菜单 → **Select Window** → 选择窗口 | 从列表中选择目标窗口 |
| 菜单 → **Clear Window Target** | 清除窗口目标，恢复全屏翻转 |

**智能行为**：
- 窗口移动时，翻转区域**自动跟随**
- 窗口大小时，翻转区域**自动调整**
- 窗口不在前台时，滤镜**自动暂停**（切回前台自动恢复）
- 窗口关闭时，自动清除目标

> 可通过菜单 **Pause When Not Foreground** 开关"前台暂停"功能。

### 区域翻转

DarkReader 支持将翻转效果限制在屏幕的指定区域：

| 操作 | 效果 |
|------|------|
| 菜单 → **Select Region** | 进入区域选择模式，拖拽选择翻转区域 |
| 菜单 → **Clear Region** | 清除区域限制，恢复全屏翻转 |
| `Win + Alt + R` | 快捷键进入区域选择模式 |

**区域选择模式**：
- 全屏显示半透明遮罩
- **左键拖拽**选择矩形区域
- **右键**或 **Esc** 取消选择
- 选中的区域会高亮显示

> 区域设置会自动保存，重启后恢复。

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

## 配置

配置文件位于：`%AppData%\DarkReader\settings.json`

```json
{
  "ActiveMode": 0,
  "ActiveOnStartup": false,
  "SmoothTransitions": true,
  "UseRegion": false,
  "RegionX": 0,
  "RegionY": 0,
  "RegionWidth": 0,
  "RegionHeight": 0,
  "UseWindow": false,
  "TargetWindowTitle": "",
  "PauseWhenNotInForeground": true
}
```

| 字段 | 说明 |
|------|------|
| `ActiveMode` | 当前模式（0=关闭, 1=Default, 2-6=Preset 1-5, 7=Grayscale） |
| `ActiveOnStartup` | 启动时是否自动开启 |
| `SmoothTransitions` | 是否启用 150ms 平滑过渡动画 |
| `UseRegion` | 是否启用区域限制 |
| `RegionX` | 区域左上角 X 坐标 |
| `RegionY` | 区域左上角 Y 坐标 |
| `RegionWidth` | 区域宽度 |
| `RegionHeight` | 区域高度 |
| `UseWindow` | 是否启用窗口目标模式 |
| `TargetWindowTitle` | 目标窗口标题（用于显示） |
| `PauseWhenNotInForeground` | 窗口不在前台时是否自动暂停 |

设置会在切换模式时自动保存，重启后恢复上次状态。

## 单实例

程序只允许运行一个实例。如果已运行 DarkReader 时再次启动：
- 新进程会发送信号给已有实例（切换开关）
- 新进程自动退出

## 系统要求

- Windows 10/11 64位
- 无需安装 .NET Runtime（已内置在 exe 中）
- 无需管理员权限

## 从源码构建

需要 [.NET 8 SDK](https://dotnet.microsoft.com/download) 或更高版本。

```bash
# 克隆仓库
git clone https://github.com/CodingAQ/DarkReader-Desktop.git
cd DarkReader-Desktop

# 还原依赖并构建
dotnet build

# 调试运行
dotnet run --project DarkReader

# 发布单文件自包含版本（约 68MB）
dotnet publish DarkReader -c Release -r win-x64 --self-contained true -o Release
```

构建产物在 `Release/` 目录下。详见 [CONTRIBUTING.md](./CONTRIBUTING.md)。

## 故障排除

| 问题 | 解决方案 |
|------|----------|
| 启动后屏幕无变化 | 确保 Windows DWM/Aero 已启用 |
| 快捷键无响应 | 检查是否有其他程序占用了相同快捷键 |
| 托盘图标不显示 | 检查 Windows 通知区域设置，确保 DarkReader 未被隐藏 |
| 退出后屏幕仍反转 | 重启程序并按 `Win + Alt + N` 关闭，或重启 Windows |

## 卸载

1. 确保 DarkReader 已退出（右键托盘图标 → Exit）
2. 删除程序文件夹
3. （可选）删除配置：`%AppData%\DarkReader\`

## 致谢

- [NegativeScreen](https://github.com/mlaily/NegativeScreen) by [mlaily](https://github.com/mlaily) — 本项目的上游基础
- [Tom MacLeod](https://github.com/mlaily/NegativeScreen) — Smart Inversion 算法灵感来源

## 许可证

本项目基于 [GPL-3.0](./LICENSE) 许可证发布。使用、修改、分发请遵循该许可证条款。

## 变更记录

详见 [CHANGELOG.md](./CHANGELOG.md)。
