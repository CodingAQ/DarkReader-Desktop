# DarkReader 使用教程

> **致谢与衍生声明**：本项目基于 [NegativeScreen](https://github.com/mlaily/NegativeScreen)（作者 [mlaily](https://github.com/mlaily)，GPL-3.0）改进而成，在此对原作者表示感谢。
> 主要改进：迁移至 .NET 8 / WinForms、单文件自包含发布、新增窗口翻转、区域翻转、平滑过渡、托盘菜单交互重构等。
> 本项目同样采用 [GPL-3.0](./LICENSE) 许可证。

## 简介

DarkReader 是一个 Windows 系统托盘应用，一键将整个屏幕切换为深色模式。使用 Windows 内置的 Magnification API（合成器级色彩矩阵），零帧延迟、CPU 占用可忽略。

## 下载运行

1. 解压发布包到任意目录
2. 双击 `DarkReader.exe` 启动
3. 程序最小化到系统托盘（右下角通知区域）

> **首次运行**：如果提示 Windows Aero/DWM 未启用，请点击"确定"继续（部分系统可能不需要 Aero）。

## 操作方式

### 托盘图标

| 操作 | 效果 |
|------|------|
| **左键单击** 托盘图标 | 切换 深色模式 开/关 |
| **右键单击** 托盘图标 | 打开模式菜单 |

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

### 菜单选项

- **Toggle** — 切换深色模式开关
- **Simple Inversion** — 简单色彩反转（经典负片效果）
- **Smart Inversion 1-5** — 5 种智能反转（保留色相，视觉更舒适）
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
| `Win + Alt + 1` | 切换到：简单反转 |
| `Win + Alt + 2` | 切换到：智能反转 1 |
| `Win + Alt + 3` | 切换到：智能反转 2 |
| `Win + Alt + 4` | 切换到：智能反转 3 |
| `Win + Alt + 5` | 切换到：智能反转 4 |
| `Win + Alt + 6` | 切换到：灰度 |
| `Win + Alt + R` | 选择翻转区域 |
| `Win + Alt + H` | 退出程序 |

> 快捷键全局有效，即使其他程序在焦点也能触发。

## 6 种暗色模式说明

| 模式 | 说明 |
|------|------|
| **Simple Inversion** | 简单 RGB 反转，效果强烈，适合高对比度需求 |
| **Smart Inversion 1** | 理论最优变换（Tom MacLeod 方案），色彩准确但可能过饱和 |
| **Smart Inversion 2** | 最简洁的 180° 色相偏移，高饱和度，纯色表现好 |
| **Smart Inversion 3** | 整体去饱和，黄色和蓝色偏暗，适合长时间阅读 |
| **Smart Inversion 4** | 高饱和度，黄色和蓝色偏暗，可读性较好 |
| **Smart Inversion 5** | 中等饱和度，CMY 色彩轻微去饱和，颜色自然 |
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
| `ActiveMode` | 当前模式（0=关闭, 1=简单反转, 2-6=智能反转, 7=灰度） |
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
- .NET 8 Runtime（已内置在 exe 中，无需额外安装）
- 无需管理员权限

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
