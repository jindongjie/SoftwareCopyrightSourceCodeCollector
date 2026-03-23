using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SoftwareCopyrightSourceCodeCollector.CoreLib;

// Use alias to avoid ambiguity with System.Console in the SoftwareCopyrightSourceCodeCollector.Console namespace
using Con = System.Console;

namespace SoftwareCopyrightSourceCodeCollector.Console;

class Program
{
    static async Task Main(string[] args)
    {
        Con.OutputEncoding = System.Text.Encoding.UTF8;
        Con.WriteLine("╔════════════════════════════════════════════════════════╗");
        Con.WriteLine("║        软件著作权源代码收集工具  -  命令行版本               ║");
        Con.WriteLine("╚════════════════════════════════════════════════════════╝");

        while (true)
        {
            Con.WriteLine();
            Con.WriteLine("请选择操作:");
            Con.WriteLine("  1. 扫描文件夹并导出源代码 (DOCX)");
            Con.WriteLine("  2. 导出软著申报信息 (TXT)");
            Con.WriteLine("  0. 退出");
            Con.Write("请输入选项: ");

            var choice = Con.ReadLine()?.Trim();

            switch (choice)
            {
                case "1":
                    await ScanAndExportDocxAsync();
                    break;
                case "2":
                    await ExportInfoTxtAsync();
                    break;
                case "0":
                    Con.WriteLine("再见！");
                    return;
                default:
                    Con.WriteLine("无效选项，请重试。");
                    break;
            }
        }
    }

    // ── DOCX workflow ─────────────────────────────────────────────────────────

    static async Task ScanAndExportDocxAsync()
    {
        Con.WriteLine();

        var folder = Prompt("请输入要扫描的文件夹路径");
        if (!Directory.Exists(folder))
        {
            Con.WriteLine("错误：文件夹不存在。");
            return;
        }

        var extensionInput = Prompt("文件类型（用 ; 分隔，例: cs;ts;java）", "cs");
        var extensions = extensionInput
            .Split(';')
            .Select(e => e.Trim())
            .Where(e => !string.IsNullOrEmpty(e));

        var excludeInput = Prompt("排除路径（用 ; 分隔，支持 glob，例: **.min.js;bin/**，留空则不排除）", "");
        var excludePatterns = string.IsNullOrWhiteSpace(excludeInput)
            ? null
            : (IEnumerable<string>)excludeInput.Split(';').Select(e => e.Trim()).Where(e => !string.IsNullOrEmpty(e));

        var softwareName = Prompt("软件名称");
        var softwareAuthor = Prompt("作者/版权所有者");
        var softwareVersion = Prompt("版本号", "V1.0");
        var maxPageStr = Prompt("最大页数", "60");
        var maxPage = int.TryParse(maxPageStr, out var mp) ? mp : 60;

        var outputPath = Prompt("输出文件路径 (.docx)");
        if (!outputPath.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            outputPath += ".docx";

        try
        {
            Con.WriteLine("正在扫描文件...");
            var files = await Core.ScanFilesAsync(folder, extensions, excludePatterns);
            Con.WriteLine($"共找到 {files.Count} 个文件，{files.Sum(f => Math.Max(f.CodeCount, 0))} 行代码。");

            if (files.Count == 0)
            {
                Con.WriteLine("没有找到匹配的文件，导出取消。");
                return;
            }

            Con.WriteLine("正在导出 DOCX，请稍候...");
            var info = new SoftwareInfo
            {
                SoftwareName = softwareName,
                SoftwareAuthor = softwareAuthor,
                SoftwareVersion = softwareVersion,
            };

            await using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite);
            var warnings = await Core.ExportToDocxAsync(stream, info, files, maxPage);

            if (warnings.Count > 0)
            {
                Con.WriteLine("以下文件被跳过：");
                foreach (var w in warnings)
                    Con.WriteLine("  " + w);
            }

            Con.WriteLine($"导出成功：{Path.GetFullPath(outputPath)}");
        }
        catch (ArgumentException aex)
        {
            Con.WriteLine($"参数错误：{aex.Message}");
        }
        catch (Exception ex)
        {
            Con.WriteLine($"导出失败：{ex.Message}");
        }
    }

    // ── TXT workflow ──────────────────────────────────────────────────────────

    static async Task ExportInfoTxtAsync()
    {
        Con.WriteLine();
        Con.WriteLine("请输入软著申报信息（直接回车使用括号内默认值）：");

        var info = new SoftwareInfo
        {
            SoftwareFullName = Prompt("软件全称"),
            SoftwareShortName = Prompt("软件简称"),
            SoftwareVersionNumber = Prompt("版本号", "V1.0"),
            CopyrightOwner = Prompt("著作权人"),
            RightsAcquisitionMethod = Prompt("权利取得方式 (原始/继受)", "原始"),
            RightsScope = Prompt("权利范围 (全部权利/部分权利)", "全部权利"),
            SoftwareCategory = Prompt("软件类别"),
            DevelopmentMethod = Prompt("开发方式 (独立开发/合作开发/委托开发)", "独立开发"),
            DevelopmentFinishDate = Prompt("开发完成日期 (例: 2024-01-01)"),
            PublishStatus = Prompt("发表状态 (已发表/未发表)", "已发表"),
            DevelopmentHardwareEnvironment = Prompt("开发的硬件环境"),
            RuntimeHardwareEnvironment = Prompt("运行的硬件环境"),
            DevelopmentOS = Prompt("开发操作系统"),
            DevelopmentTool = Prompt("开发工具"),
            RuntimePlatform = Prompt("运行平台/操作系统"),
            RuntimeSupportSoftware = Prompt("运行支撑软件"),
            ProgrammingLanguage = Prompt("编程语言"),
            ProgrammingLanguageOther = Prompt("其他编程语言（可留空）", ""),
            SourceCodeAmount = Prompt("源程序量（行数）"),
            DevelopmentPurpose = Prompt("开发目的（至少 8 字）"),
            TargetIndustry = Prompt("面向行业/领域（至少 4 字）"),
            MainFunctions = Prompt("软件主要功能（至少 100 字）"),
            TechnicalFeatures = Prompt("软件技术特点（可留空）", ""),
        };

        // Software-type flags
        info.IsAppSoftware = PromptBool("是否为 APP 软件");
        info.IsGameSoftware = PromptBool("是否为游戏软件");
        info.IsEducationSoftware = PromptBool("是否为教育软件");
        info.IsFinanceSoftware = PromptBool("是否为金融软件");
        info.IsMedicalSoftware = PromptBool("是否为医疗软件");
        info.IsGISSoftware = PromptBool("是否为地理信息软件");
        info.IsCloudSoftware = PromptBool("是否为云计算软件");
        info.IsSecuritySoftware = PromptBool("是否为信息安全软件");
        info.IsBigDataSoftware = PromptBool("是否为大数据软件");
        info.IsAISoftware = PromptBool("是否为人工智能软件");
        info.IsVRSoftware = PromptBool("是否为 VR 软件");
        info.Is5GSoftware = PromptBool("是否为 5G 软件");
        info.IsMiniProgramSoftware = PromptBool("是否为小程序");
        info.IsIoTSoftware = PromptBool("是否为物联网软件");
        info.IsSmartCitySoftware = PromptBool("是否为智慧城市软件");
        info.IsIndustrialControlSoftware = PromptBool("是否为工业控制软件");

        var outputPath = Prompt("输出文件路径 (.txt)", "software_info.txt");
        if (!outputPath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            outputPath += ".txt";

        try
        {
            await Core.ExportToTxtAsync(outputPath, info);
            Con.WriteLine($"导出成功：{Path.GetFullPath(outputPath)}");
        }
        catch (Exception ex)
        {
            Con.WriteLine($"导出失败：{ex.Message}");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static string Prompt(string label, string? defaultValue = null)
    {
        if (defaultValue != null)
            Con.Write($"{label}（默认: {defaultValue}）: ");
        else
            Con.Write($"{label}: ");

        var input = Con.ReadLine()?.Trim() ?? string.Empty;
        return string.IsNullOrEmpty(input) && defaultValue != null ? defaultValue : input;
    }

    static bool PromptBool(string label)
    {
        Con.Write($"{label} (y/N): ");
        var input = Con.ReadLine()?.Trim().ToLowerInvariant() ?? "";
        return input == "y" || input == "yes";
    }
}
