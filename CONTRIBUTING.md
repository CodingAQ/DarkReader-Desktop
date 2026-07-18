# 贡献指南

感谢你对 DarkReader 的关注！欢迎通过以下方式参与贡献。

## 开发环境

- **.NET 8 SDK** 或更高版本（含 .NET 8 Desktop Runtime）
- **Windows 10/11 64 位**（项目依赖 Windows Forms 与 Magnification API）
- 推荐 Visual Studio 2022 或 VS Code + C# Dev Kit

## 构建与运行

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

## 项目结构

```
DarkReader/
├── DarkReader/              # 主项目源码
│   ├── Program.cs           # 入口 + 单实例 mutex
│   ├── MainForm.cs          # 托盘菜单 + 主逻辑
│   ├── NativeMethods.cs     # Magnification API P/Invoke
│   ├── BuiltinMatrices.cs   # 6 种色彩矩阵
│   ├── RegionOverlay.cs     # 区域放大镜窗口
│   ├── RegionSelectorForm.cs# 区域选择 UI
│   ├── Settings.cs          # 配置持久化
│   ├── WindowPickerForm.cs  # 窗口选择 UI
│   └── WindowTracker.cs     # 窗口跟随/暂停
├── DarkReader.sln
├── LICENSE                  # GPL-3.0
└── README.md
```

## 代码规范

- **换行符**：`.cs` / `.csproj` / `.sln` 使用 CRLF；`.md` / `.json` / `.yml` 使用 LF（由 `.gitattributes` 自动处理）
- **版权头**：新增 `.cs` 文件须在文件头保留 GPL-3.0 版权块（参考现有文件）
- **命名**：遵循 C# 常规命名约定（PascalCase 用于类/方法，camelCase 用于局部变量）
- **License**：所有提交的代码须兼容 GPL-3.0

## 提交规范

- 提交信息建议使用英文，格式：`<type>: <subject>`
  - `feat: 新增窗口翻转功能`
  - `fix: 修复区域选择越界问题`
  - `docs: 更新 README 快捷键说明`
  - `refactor: 重构菜单构建逻辑`
- 一次提交只做一件事，保持原子性

## 提交 Pull Request

1. Fork 本仓库并创建特性分支：`git checkout -b feat/your-feature`
2. 提交改动并推送到你的 Fork
3. 向 `main` 分支发起 Pull Request，描述清楚改动内容与动机
4. 等待 review，根据反馈调整

## 报告问题

- 通过 [Issues](../../issues) 报告 Bug 或提出功能建议
- 请附上：Windows 版本、复现步骤、预期与实际行为

## License

贡献的代码将遵循 [GPL-3.0](./LICENSE) 许可证发布。
