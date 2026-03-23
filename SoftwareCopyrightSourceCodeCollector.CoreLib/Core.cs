using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.FileSystemGlobbing;

namespace SoftwareCopyrightSourceCodeCollector.CoreLib;

/// <summary>
/// Platform-agnostic core logic for scanning source files and exporting
/// software-copyright documents.  This class has no dependency on any UI
/// framework; all Avalonia / MessageBox interactions remain in the ViewModel.
/// </summary>
public static class Core
{
    // ── File scanning ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the number of non-binary lines in <paramref name="file"/>,
    /// or <c>-1</c> if the file appears to be binary.
    /// </summary>
    public static int GetCodeCount(string file)
    {
        try
        {
            using var reader = new StreamReader(file);
            var lineCount = 0;
            var nonTextCharCount = 0;

            while (reader.ReadLine() is { } line)
            {
                foreach (var _ in line.Where(c => char.IsControl(c) && c != '\r' && c != '\n' && c != '\t'))
                {
                    nonTextCharCount++;
                    if (nonTextCharCount >= 50)
                        return -1;
                }
                lineCount++;
            }

            return lineCount;
        }
        catch (Exception)
        {
            return -1;
        }
    }

    /// <summary>
    /// Scans <paramref name="folder"/> recursively for files whose extension
    /// matches any entry in <paramref name="extensions"/> and returns the
    /// resulting <see cref="FileItem"/> list (order numbers are pre-assigned).
    /// </summary>
    /// <param name="folder">Root folder to scan.</param>
    /// <param name="extensions">File extensions to include (e.g. "cs", ".ts").</param>
    /// <param name="excludePatterns">
    /// Optional glob patterns (e.g. "**.min.js", "src/**/obj/**") for files to exclude.
    /// Uses the same syntax as <see cref="Matcher"/>.
    /// </param>
    public static async Task<List<FileItem>> ScanFilesAsync(
        string folder,
        IEnumerable<string> extensions,
        IEnumerable<string>? excludePatterns = null)
    {
        var endWithList = extensions
            .Select(ext => ext.StartsWith('.') ? ext : "." + ext)
            .ToList();

        Matcher? matcher = null;
        var patternList = excludePatterns?.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        if (patternList is { Count: > 0 })
        {
            matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
            foreach (var pattern in patternList)
                matcher.AddInclude(pattern);
        }

        var files = Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
            .Where(file =>
                endWithList.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) &&
                (matcher == null || !IsExcluded(file, folder, matcher)))
            .ToArray();

        var tasks = files.Select(file => Task.Run(() => new FileItem
        {
            FileName = Path.GetFileName(file),
            CreationDate = File.GetCreationTime(file).ToString(CultureInfo.InvariantCulture),
            CodeCount = GetCodeCount(file),
            FilePath = Path.GetFullPath(file),
        })).ToArray();

        var results = await Task.WhenAll(tasks);

        for (var i = 0; i < results.Length; i++)
            results[i].OrderNumber = (i + 1).ToString();

        return results.ToList();
    }

    private static bool IsExcluded(string filePath, string rootFolder, Matcher matcher)
    {
        var relativePath = Path.GetRelativePath(rootFolder, filePath)
            .Replace("\\", "/");
        return matcher.Match(relativePath).HasMatches;
    }

    // ── DOCX export ───────────────────────────────────────────────────────────

    /// <summary>
    /// Writes the source-code DOCX to <paramref name="stream"/>.
    /// </summary>
    /// <returns>
    /// A list of warning messages for individual files that could not be read
    /// (they were skipped rather than causing a fatal error).
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when a required field in <paramref name="info"/> is empty, or
    /// when <paramref name="files"/> contains no entries.
    /// </exception>
    public static async Task<IReadOnlyList<string>> ExportToDocxAsync(
        Stream stream,
        SoftwareInfo info,
        IList<FileItem> files,
        int maxPage)
    {
        if (string.IsNullOrWhiteSpace(info.SoftwareName))
            throw new ArgumentException("软件名称不能为空！", nameof(info));
        if (string.IsNullOrWhiteSpace(info.SoftwareAuthor))
            throw new ArgumentException("作者信息不能为空！", nameof(info));
        if (string.IsNullOrWhiteSpace(info.SoftwareVersion))
            throw new ArgumentException("版本号不能为空！", nameof(info));
        if (files == null || files.Count == 0)
            throw new ArgumentException("没有可导出的文件，请先选择并扫描文件夹。", nameof(files));

        var warnings = new List<string>();

        using var wordDocument = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var mainPart = wordDocument.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = mainPart.Document.AppendChild(new Body());

        // Header – software title
        var titlePara = body.AppendChild(new Paragraph());
        var titleRun = titlePara.AppendChild(new Run());
        titleRun.AppendChild(new Text($"{info.SoftwareName}源代码"));
        titleRun.RunProperties = new RunProperties(new Bold(), new Color { Val = "0000FF" });

        // Header – copyright
        var authorPara = body.AppendChild(new Paragraph());
        var authorRun = authorPara.AppendChild(new Run());
        authorRun.AppendChild(new Text($"Copyright © {info.SoftwareAuthor}  {DateTime.Now.Year}"));

        // Header – version
        var versionPara = body.AppendChild(new Paragraph());
        var versionRun = versionPara.AppendChild(new Run());
        versionRun.AppendChild(new Text($"版本： {info.SoftwareVersion}"));

        // File contents
        var totalLineNumber = 1;

        foreach (var fileItem in files)
        {
            if (fileItem.CodeCount == -1)
            {
                warnings.Add($"文件 {fileItem.FileName} 读取失败，已跳过。");
                continue;
            }
            if (!File.Exists(fileItem.FilePath))
            {
                warnings.Add($"文件 {fileItem.FileName} 不存在，已跳过。");
                continue;
            }

            var filePara = body.AppendChild(new Paragraph());
            var fileRun = filePara.AppendChild(new Run());
            fileRun.AppendChild(new Text($"文件: {fileItem.FileName}"));
            fileRun.RunProperties = new RunProperties(new Bold());

            string[] sourceLines;
            try
            {
                sourceLines = await File.ReadAllLinesAsync(fileItem.FilePath);
            }
            catch (Exception ex)
            {
                warnings.Add($"读取文件 {fileItem.FileName} 时出错：{ex.Message}，已跳过。");
                continue;
            }

            foreach (var sourceLine in sourceLines)
            {
                var sanitizedLine = new string(
                    sourceLine.Where(c => !char.IsControl(c) || c == '\r' || c == '\n' || c == '\t').ToArray());

                var linePara = body.AppendChild(new Paragraph());
                linePara.Append(new ParagraphProperties(
                    new SpacingBetweenLines { Line = "180", LineRule = LineSpacingRuleValues.Auto }));

                var lineRun = linePara.AppendChild(new Run());
                lineRun.AppendChild(new Text($"{totalLineNumber}\t{sanitizedLine}"));
                lineRun.RunProperties = new RunProperties(
                    new FontSize { Val = "10" },
                    new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" });

                totalLineNumber++;
            }
        }

        // Footer – page number
        var footer = new Footer();
        var footerPara = new Paragraph();
        var footerRun = new Run();
        footerRun.Append(new FieldChar { FieldCharType = FieldCharValues.Begin });
        footerRun.Append(new FieldCode(" PAGE "));
        footerRun.Append(new FieldChar { FieldCharType = FieldCharValues.Separate });
        footerRun.Append(new Text("1"));
        footerRun.Append(new FieldChar { FieldCharType = FieldCharValues.End });
        footerPara.Append(footerRun);
        footer.Append(footerPara);

        var footerPart = mainPart.AddNewPart<FooterPart>();
        footerPart.Footer = footer;
        footerPart.Footer.Save();

        var sectionProps = new SectionProperties();
        sectionProps.Append(new FooterReference
        {
            Type = HeaderFooterValues.Default,
            Id = mainPart.GetIdOfPart(footerPart)
        });
        body.Append(sectionProps);

        mainPart.Document.Save();

        // Trim to maxPage if needed
        var pageCount = GetDocxPageCount(wordDocument);
        if (pageCount > maxPage)
        {
            var keepFront = maxPage / 2;
            var keepEnd = maxPage - keepFront;
            RemoveMiddlePages(wordDocument, keepFront, keepEnd);
            mainPart.Document.Save();
        }

        return warnings;
    }

    // ── DOCX helpers ──────────────────────────────────────────────────────────

    private static int GetDocxPageCount(WordprocessingDocument doc)
    {
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return 1;
        var pageBreaks = body.Descendants<Break>().Count(b => b.Type?.Value == BreakValues.Page);
        return pageBreaks + 1;
    }

    private static void RemoveMiddlePages(WordprocessingDocument doc, int keepFront, int keepEnd)
    {
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return;

        var paragraphs = body.Elements<Paragraph>().ToList();
        var pageBreakIndices = new List<int> { 0 };
        for (var i = 0; i < paragraphs.Count; i++)
        {
            if (paragraphs[i].Descendants<Break>().Any(b => b.Type?.Value == BreakValues.Page))
                pageBreakIndices.Add(i + 1);
        }
        pageBreakIndices.Add(paragraphs.Count);

        var totalPages = pageBreakIndices.Count - 1;
        if (totalPages <= keepFront + keepEnd) return;

        var removeStart = pageBreakIndices[keepFront];
        var removeEnd = pageBreakIndices[totalPages - keepEnd];

        for (var i = removeEnd - 1; i >= removeStart; i--)
            paragraphs[i].Remove();
    }

    // ── TXT export ────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds and returns the lines for the software-copyright TXT export.
    /// </summary>
    public static List<string> BuildTxtLines(SoftwareInfo info)
    {
        var lines = new List<string>
        {
            "╔════════════════════════════════════════════════════════════════════════════╗",
            "║                            软件著作权登记信息                                 ║",
            "╚════════════════════════════════════════════════════════════════════════════╝",
            "",
            "【软件全称】",
            info.SoftwareFullName,
            "",
            "【软件简称】",
            info.SoftwareShortName,
            "",
            "【版本号】",
            info.SoftwareVersionNumber,
            "",
            "【权利取得方式】",
            info.RightsAcquisitionMethod,
            "",
            "【权利范围】",
            info.RightsScope,
            "",
            "【软件分类】",
            info.SoftwareCategory,
            "",
            "【开发方式】",
            info.DevelopmentMethod,
            "",
            "【开发完成日期】",
            info.DevelopmentFinishDate ?? "",
            "",
            "【发表状态】",
            info.PublishStatus,
            "",
            "【著作权人】",
            info.CopyrightOwner,
            "",
            "【开发的硬件环境】",
            info.DevelopmentHardwareEnvironment,
            "",
            "【运行的硬件环境】",
            info.RuntimeHardwareEnvironment,
            "",
            "【开发该软件的操作系统】",
            info.DevelopmentOS,
            "",
            "【软件开发环境/开发工具】",
            info.DevelopmentTool,
            "",
            "【该软件的运行平台/操作系统】",
            info.RuntimePlatform,
            "",
            "【软件运行支撑环境/支持软件】",
            info.RuntimeSupportSoftware,
            "",
            "【编程语言】",
            info.ProgrammingLanguage +
                (string.IsNullOrWhiteSpace(info.ProgrammingLanguageOther)
                    ? ""
                    : $" 手工填写： {info.ProgrammingLanguageOther}"),
            "",
            "【源程序量】",
            info.SourceCodeAmount,
            "",
            "【开发目的】",
            info.DevelopmentPurpose,
            "",
            "【面向行业/领域】",
            info.TargetIndustry,
            "",
            "【软件的主要功能】",
            info.MainFunctions,
            "",
            "【软件的技术特点】"
        };

        var techTags = new List<string>();
        if (info.IsAppSoftware) techTags.Add("APP");
        if (info.IsGameSoftware) techTags.Add("游戏软件");
        if (info.IsEducationSoftware) techTags.Add("教育软件");
        if (info.IsFinanceSoftware) techTags.Add("金融软件");
        if (info.IsMedicalSoftware) techTags.Add("医疗软件");
        if (info.IsGISSoftware) techTags.Add("地理信息软件");
        if (info.IsCloudSoftware) techTags.Add("云计算软件");
        if (info.IsSecuritySoftware) techTags.Add("信息安全软件");
        if (info.IsBigDataSoftware) techTags.Add("大数据软件");
        if (info.IsAISoftware) techTags.Add("人工智能软件");
        if (info.IsVRSoftware) techTags.Add("VR软件");
        if (info.Is5GSoftware) techTags.Add("5G软件");
        if (info.IsMiniProgramSoftware) techTags.Add("小程序");
        if (info.IsIoTSoftware) techTags.Add("物联网软件");
        if (info.IsSmartCitySoftware) techTags.Add("智慧城市软件");
        if (info.IsIndustrialControlSoftware) techTags.Add("工业控制软件");

        if (techTags.Count > 0)
            lines.Add("标签: " + string.Join(" | ", techTags));
        if (!string.IsNullOrWhiteSpace(info.TechnicalFeatures))
            lines.Add(info.TechnicalFeatures);
        lines.Add("");
        lines.Add("────────────────────────────────────────────────────────────────────────────");
        lines.Add("温馨提示：请核对信息后直接在(https://register.ccopyright.com.cn/r11.html) 申请软著或存档。");
        lines.Add("────────────────────────────────────────────────────────────────────────────");

        return lines;
    }

    /// <summary>
    /// Writes the software-copyright TXT to <paramref name="outputPath"/>.
    /// </summary>
    public static async Task ExportToTxtAsync(string outputPath, SoftwareInfo info)
    {
        var lines = BuildTxtLines(info);
        await File.WriteAllLinesAsync(outputPath, lines);
    }
}
