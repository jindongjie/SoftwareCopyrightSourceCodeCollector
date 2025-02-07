# 项目名称

本项目使用 Avalonia 框架构建，目标为 .NET 8，支持跨平台开发和 NativeAOT。

## 概述

Avalonia 是一个用于 .NET 的跨平台 UI 框架，提供了一种现代且灵活的方式来构建桌面应用程序。它支持 Windows、macOS 和 Linux，让您可以编写一次应用程序并在任何地方运行。

## 功能

- **跨平台**：编写一次应用程序，可以在 Windows、macOS 和 Linux 上运行。
- **NativeAOT 支持**：将应用程序编译为本机代码，以提高性能和减少启动时间。
- **现代 UI**：使用 XAML 创建美观且响应迅速的用户界面。
- **MVVM 支持**：内置对 Model-View-ViewModel 模式的支持。
- **丰富的控件**：内置多种控件，加快开发进程。
- **可定制**：轻松定制应用程序的外观和感觉。

## 快速开始

### 前提条件

- .NET 8 SDK
- 一个 IDE，如 Visual Studio

### 安装

1. 克隆仓库：
    
```git clone https://github.com/your-repo/project-name.git```

2. 进入项目目录：
    
```cd project-name```

3. 运行项目：
```dotnet run```




### 使用 NativeAOT 发布

要使用 NativeAOT 发布项目，运行：

```dotnet publish -c Release -r win-x64 --self-contained```

将 `win-x64` 替换为 `osx-x64` 或 `linux-x64` 以适应其他平台。

## 主界面功能

`MainView.axaml` 文件定义了主界面的布局和功能，包括：

- **文件夹选择**：选择文件夹路径，并通过 `BrowseFolderCommand` 命令打开文件夹选择对话框。
- **后缀名过滤**：输入文件类型（以英文分号分隔），并通过 `GetAllFilesCommand` 命令查询文件。
- **文件预览**：显示筛选后的文件预览。
- **软件信息输入**：输入软件名称、著作权人、软件版本等信息。
- **导出文档**：通过 `ExportToDocxCommand` 命令导出为 docx 文档。

## 贡献

欢迎贡献！请提交拉取请求或打开问题以讨论您的想法。

## 许可证

本项目使用 MIT 许可证。有关详细信息，请参阅 [LICENSE](LICENSE) 文件。

## 鸣谢

- [Avalonia](https://avaloniaui.net/)
- [Microsoft .NET](https://dotnet.microsoft.com/)

## 联系

如有任何问题或建议，请联系 [1@jindongjie.cn](mailto:1@jindongjie.cn)
