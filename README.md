# 软件著作权源代码收集器 [![.NET Core Desktop](https://github.com/jindongjie/SoftwareCopyrightSourceCodeCollector/actions/workflows/dotnet-desktop.yml/badge.svg)](https://github.com/jindongjie/SoftwareCopyrightSourceCodeCollector/actions/workflows/dotnet-desktop.yml) [![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=jindongjie_SoftwareCopyrightSourceCodeCollector&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=jindongjie_SoftwareCopyrightSourceCodeCollector) [![SonarQube Cloud](https://sonarcloud.io/images/project_badges/sonarcloud-light.svg)](https://sonarcloud.io/summary/new_code?id=jindongjie_SoftwareCopyrightSourceCodeCollector)

## 项目简介
一个基于 Avalonia UI 的跨平台桌面应用，开箱即用、完全开源，帮你快速整理并导出软件著作权申请所需的源代码文档。支持 Windows / Linux / macOS。

说明：已在 Windows 11、Linux（Wayland 图形后端）上测试；macOS 尚未验证，如能运行欢迎反馈。

## 软件界面预览
![软件主界面截图](./swscc-gif-jpeg/1.png)

## 安装方法
1) 通过 GitHub Release
- 在右侧「Releases」下载最新版本
- 按操作系统选择对应包，下载后直接运行
- 目前提供 x86-64 架构的自动构建，其他架构请自行编译

2) 手动编译
- 克隆项目到本地
- 执行以下命令（根据平台替换 --runtime，例如 win-x64 / linux-x64 / osx-x64）：
```bash
dotnet publish SoftwareCopyrightSourceCodeCollector.Desktop/SoftwareCopyrightSourceCodeCollector.Desktop.csproj \
  -c Release --self-contained true /p:PublishSingleFile=true /p:PublishReadyToRun=true /p:PublishTrimmed=false \
  --runtime <你的运行时标识>
```

## 主要功能
- 源代码收集
  - 选择目标文件夹
  - 自定义文件类型筛选（如：cs;json;js;cpp 等）
  - 支持文件预览
  - 可设置程序入口文件
- 文档导出
  - 导出 Word（.docx）
  - 可配置软件名称、著作权人、版本
- 申请文档编写
  - 按标准格式整理
  - 支持导出为 .txt 便于查看

## 界面布局
- 采用多 Tab 设计，包含：
  - 导出文档
  - 注册流程
  - 开发者链接

## 使用方法
1. 选择源代码所在文件夹
2. 设置文件类型筛选（分号分隔）
3. 点击查询，预览结果
4. 指定程序入口文件
5. 填写软件名称、著作权人、版本
6. 点击「导出 docx」生成文档

## 注意事项
- 文件类型请用英文分号分隔，例如：cs;json;js;cpp
- 软著申请通常建议源码行数大于 3000 行；若少于 3000 行，需在申请材料中说明
- macOS 兼容性尚未确认，如有测试结果欢迎反馈

## 技术框架
- .NET 8.0：基础开发框架
- Avalonia UI 11.2.3：跨平台 UI
- CommunityToolkit.Mvvm：MVVM 支持
- DocumentFormat.OpenXml：Word 文档处理
- Semi.Avalonia：主题与组件库
