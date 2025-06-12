using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MsBox.Avalonia;
using SoftwareCopyrightSourceCodeCollector.ViewModels;

namespace Software_Copyright_Source_Code_Collector.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _selectedFolder = string.Empty;

    [RelayCommand]
    //遍历并获取文件夹下所有文件
    private async Task GetAllFiles()
    {
        //清空集合
        SearchedFileItemsOriginalCollection.Clear();
        //展开文件选择列表
        var tempChoseFileType = ChoseFileType;

        var endWithList = tempChoseFileType.Split(';')
            .Select(ext => ext.StartsWith('.') ? ext : "." + ext)
            .ToList();

        if (string.IsNullOrEmpty(SelectedFolder) || endWithList.Count == 0)
        {
            return;
        }

        try
        {
            var files = Directory.EnumerateFiles(SelectedFolder, "*", SearchOption.AllDirectories)
                .Where(file => endWithList.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            //遍历所有文件，挨个添加
            var tasks = files.Select(file => Task.Run(() =>
            {
                var searchedFileItem = new SearchedFileItem
                {
                    FileName = Path.GetFileName(file),
                    CreationDate = File.GetCreationTime(file).ToString(CultureInfo.InvariantCulture),
                    CodeCount = GetCodeCount(file),
                    FilePath = Path.GetFullPath(file),
                    Parent = this
                };

                return searchedFileItem;
            })).ToArray();

            var results = await Task.WhenAll(tasks);

            foreach (var item in results)
            {
                item.OrderNumber = (SearchedFileItemsOriginalCollection.Count + 1).ToString();
                SearchedFileItemsOriginalCollection.Add(item);
            }
            //更新至提示字段
            SearchedTotalCount = $"共计：{results.Length} 个文件，{results.Sum(item => item.CodeCount)} 行代码";
        }
        catch (Exception ex)
        {
            var box = MessageBoxManager
                .GetMessageBoxStandard("错误", "报错信息：" + ex.Message);
            await box.ShowAsync();
        }
    }
    //获取文件的代码数量
    private static int GetCodeCount(string file)
    {
        try
        {
            using var reader = new StreamReader(file);
            var lineCount = 0;
            var nonTextCharCount = 0;

            while (reader.ReadLine() is { } line)
            {
                foreach (var dummy in line.Where(c => char.IsControl(c) && c != '\r' && c != '\n' && c != '\t'))
                {
                    nonTextCharCount++;
                    if (nonTextCharCount >= 50)
                    {
                        return -1; // 如果累计包含非文本字符达到 50 次，则返回 -1
                    }
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

    [RelayCommand]
    //选择文件夹指令
    private async Task BrowseFolder()
    {
        var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (mainWindow != null)
        {
            var folderPicker = await mainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "选择一个文件夹",
                AllowMultiple = false
            });

            if (folderPicker.Count > 0)
            {
                var uri = folderPicker[0].Path;
                SelectedFolder = uri.IsAbsoluteUri ? uri.LocalPath :
                    uri.ToString();
            }
        }
    }



    [ObservableProperty]
    //代码统计提示词字段
    private string _searchedTotalCount = "共计： 个文件， 行代码";

    [ObservableProperty]
    //文件类型字符串字段
    private string _choseFileType = "txt;docx";

    [ObservableProperty]
    //文件列表合集
    private ObservableCollection<SearchedFileItem> _searchedFileItemsOriginalCollection = [];

    //用于子文件对象引用
    public MainViewModel()
    {
    }
    //子文件对象
    public partial class SearchedFileItem : ViewModelBase
    {
        [ObservableProperty]
        private string _fileName = string.Empty;

        [ObservableProperty]
        private string _creationDate = string.Empty;

        [ObservableProperty]
        private int _codeCount;

        [ObservableProperty]
        private string _OrderNumber = string.Empty;

        public MainViewModel? Parent { get; set; }

        [RelayCommand]
        private Task SetAsProgramEntry()
        {
            if (Parent != null)
            {
                int oldIndex = Parent.SearchedFileItemsOriginalCollection.IndexOf(this);
                if (oldIndex > 0)
                {
                    //从老位置删除该项
                    Parent.SearchedFileItemsOriginalCollection.RemoveAt(oldIndex);
                    //插入到新位置
                    Parent.SearchedFileItemsOriginalCollection.Insert(0, this);

                    //更新序号
                    for (int i = 0; i < Parent.SearchedFileItemsOriginalCollection.Count; i++)
                    {
                        Parent.SearchedFileItemsOriginalCollection[i].OrderNumber = (i + 1).ToString();
                    }
                    //通知成功了
                    MessageBoxManager.GetMessageBoxStandard("提示", "设置成功")
                        .ShowAsync();
                }
                else if (oldIndex == 0)
                {

                    MessageBoxManager
                        .GetMessageBoxStandard("错误", "该文件已是第一个文件。")
                        .ShowAsync();
                }
                else
                {
                    MessageBoxManager
                        .GetMessageBoxStandard("错误", "该文件队列位置异常,无法执行操作！")
                        .ShowAsync();
                }
            }

            return Task.CompletedTask;
        }

        public string FilePath { get; init; } = string.Empty;
    }

    [ObservableProperty] private string _softwareName = string.Empty;
    [ObservableProperty] private string _softwareVersion = "V1.0";
    [ObservableProperty] private string _softwareAuthor = string.Empty;
    [ObservableProperty] private int _maxPage = 60;

    [RelayCommand]
    //导出DOCX文件指令
    private async Task ExportToDocx()
    {
        try
        {
            var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                ?.MainWindow;
            if (mainWindow != null)
            {
                var savePicker = await mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "导出为 DOCX 文件",
                    FileTypeChoices = [new FilePickerFileType("Word Document") { Patterns = ["*.docx"] }],
                    DefaultExtension = "docx"
                });

                if (savePicker != null)
                {
                    await using var stream = new FileStream(savePicker.Path.LocalPath, FileMode.Create, FileAccess.ReadWrite);
                    await ExportToDocxUnderHood(stream);//调用导出函数
                }
            }
        }
        catch (Exception ex)
        {
            var box = MessageBoxManager
                .GetMessageBoxStandard("错误", "导出时报错信息：" + ex.Message);
            await box.ShowAsync();
        }
    }
    //真正的导出函数
    private async Task ExportToDocxUnderHood(Stream stream)
    {
        try
        {
            using var wordDocument = WordprocessingDocument.Create(stream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
            var mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            // 基本信息
            if (string.IsNullOrWhiteSpace(SoftwareName))
            {
                await MessageBoxManager.GetMessageBoxStandard("错误", "软件名称不能为空！").ShowAsync();
                return;
            }
            if (string.IsNullOrWhiteSpace(SoftwareAuthor))
            {
                await MessageBoxManager.GetMessageBoxStandard("错误", "作者信息不能为空！").ShowAsync();
                return;
            }
            if (string.IsNullOrWhiteSpace(SoftwareVersion))
            {
                await MessageBoxManager.GetMessageBoxStandard("错误", "版本号不能为空！").ShowAsync();
                return;
            }

            var softwareInfo = $"{SoftwareName}源代码";
            var softwareInfoPara = body.AppendChild(new Paragraph());
            var softwareInfoRun = softwareInfoPara.AppendChild(new Run());
            softwareInfoRun.AppendChild(new Text(softwareInfo));
            softwareInfoRun.RunProperties = new RunProperties(new Bold(), new Color() { Val = "0000FF" });

            var authorInfo = $"Copyright © {SoftwareAuthor}  " + DateTime.Now.Year;
            var authorInfoPara = body.AppendChild(new Paragraph());
            var authorInfoRun = authorInfoPara.AppendChild(new Run());
            authorInfoRun.AppendChild(new Text(authorInfo));

            var versionInfo = $"版本： {SoftwareVersion}";
            var versionInfoPara = body.AppendChild(new Paragraph());
            var versionInfoRun = versionInfoPara.AppendChild(new Run());
            versionInfoRun.AppendChild(new Text(versionInfo));

            // 文件代码部分
            if (SearchedFileItemsOriginalCollection == null || SearchedFileItemsOriginalCollection.Count == 0)
            {
                await MessageBoxManager.GetMessageBoxStandard("错误", "没有可导出的文件，请先选择并扫描文件夹。").ShowAsync();
                return;
            }

            var totalLineNumber = 1;
            var readErrorList = new System.Collections.Generic.List<string>();

            foreach (var item in SearchedFileItemsOriginalCollection)
            {
                if (item.CodeCount == -1)
                {
                    readErrorList.Add($"文件 {item.FileName} 读取失败，已跳过。");
                    continue;
                }
                if (!File.Exists(item.FilePath))
                {
                    readErrorList.Add($"文件 {item.FileName} 不存在，已跳过。");
                    continue;
                }

                var para = body.AppendChild(new Paragraph());
                var run = para.AppendChild(new Run());
                run.AppendChild(new Text($"文件: {item.FileName}"));
                run.RunProperties = new RunProperties(new Bold());

                string[] lines;
                try
                {
                    lines = await File.ReadAllLinesAsync(item.FilePath);
                }
                catch (Exception ex)
                {
                    readErrorList.Add($"读取文件 {item.FileName} 时出错：{ex.Message}，已跳过。");
                    continue;
                }

                foreach (var t in lines)
                {
                    var sanitizedLine = new string(t.Where(c => !char.IsControl(c) || c == '\r' || c == '\n' || c == '\t').ToArray());

                    var linePara = body.AppendChild(new Paragraph());

                    var paraProps = new ParagraphProperties(
                        new SpacingBetweenLines() { Line = "180", LineRule = LineSpacingRuleValues.Auto }
                    );
                    linePara.Append(paraProps);

                    var lineRun = linePara.AppendChild(new Run());
                    lineRun.AppendChild(new Text($"{totalLineNumber}\t{sanitizedLine}"));
                    lineRun.RunProperties = new RunProperties(
                        new FontSize() { Val = "10" },
                        new RunFonts() { Ascii = "Times New Roman", HighAnsi = "Times New Roman" }
                    );
                    totalLineNumber++;
                }

            }

            // 如果有读取错误，统一弹窗显示
            if (readErrorList.Count > 0)
            {
                var errorMsg = "以下文件读取失败或被跳过：\n" + string.Join("\n", readErrorList);
                await MessageBoxManager.GetMessageBoxStandard("警告", errorMsg).ShowAsync();
            }
            // 插入 pagebreak 符号用统计
            //body.AppendChild(new Paragraph(new Run(new Break() { Type = BreakValues.Page })));

            // 添加页脚（仅显示当前页码）
            var sectionProps = new SectionProperties();
            var footer = new Footer();
            var paraFooter = new Paragraph();
            var runFooter = new Run();

            runFooter.Append(new FieldChar() { FieldCharType = FieldCharValues.Begin });
            runFooter.Append(new FieldCode(" PAGE "));
            runFooter.Append(new FieldChar() { FieldCharType = FieldCharValues.Separate });
            runFooter.Append(new Text("1"));
            runFooter.Append(new FieldChar() { FieldCharType = FieldCharValues.End });

            paraFooter.Append(runFooter);
            footer.Append(paraFooter);

            var footerPart = mainPart.AddNewPart<FooterPart>();
            footerPart.Footer = footer;
            footerPart.Footer.Save();

            sectionProps.Append(new FooterReference() { Type = HeaderFooterValues.Default, Id = mainPart.GetIdOfPart(footerPart) });
            body.Append(sectionProps);

            // 第一遍保存
            try
            {
                mainPart.Document.Save();
            }
            catch (Exception ex)
            {
                await MessageBoxManager.GetMessageBoxStandard("错误", $"保存文档时出错：{ex.Message}").ShowAsync();
                return;
            }

            // 新增：如果页数超出MaxPage，保留前后各一半
            int pageCount;
            try
            {
                pageCount = GetDocxPageCount(wordDocument);
            }
            catch (Exception ex)
            {
                await MessageBoxManager.GetMessageBoxStandard("错误", $"统计页数时出错：{ex.Message}").ShowAsync();
                return;
            }

            if (pageCount > MaxPage)
            {
                int keepFront = MaxPage / 2;
                int keepEnd = MaxPage - keepFront;
                RemoveMiddlePages(wordDocument, keepFront, keepEnd);
                try
                {
                    mainPart.Document.Save();
                }
                catch (Exception ex)
                {
                    await MessageBoxManager.GetMessageBoxStandard("错误", $"压缩保存文档时出错：{ex.Message}").ShowAsync();
                    return;
                }
            }

            await MessageBoxManager.GetMessageBoxStandard("恭喜", "导出成功").ShowAsync();
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard("错误", $"导出过程中发生未处理异常：{ex.Message}").ShowAsync();
        }
    }

    //统计文档页数
    private int GetDocxPageCount(WordprocessingDocument doc)
    {
        var body = doc.MainDocumentPart?.Document.Body;
        if (body == null) return 1;
        int pageBreaks = body.Descendants<Break>().Count(b => b.Type?.Value == BreakValues.Page);
        return pageBreaks + 1;
    }

    // 移除中间页，仅保留前keepFront和后keepEnd页
    private void RemoveMiddlePages(WordprocessingDocument doc, int keepFront, int keepEnd)
    {
        var body = doc.MainDocumentPart?.Document.Body;
        if (body == null) return;

        // 找到所有分页符所在段落的索引
        var paragraphs = body.Elements<Paragraph>().ToList();
        var pageBreakIndices = new List<int> { 0 };
        for (int i = 0; i < paragraphs.Count; i++)
        {
            if (paragraphs[i].Descendants<Break>().Any(b => b.Type?.Value == BreakValues.Page))
            {
                pageBreakIndices.Add(i + 1);
            }
        }
        pageBreakIndices.Add(paragraphs.Count);

        int totalPages = pageBreakIndices.Count - 1;
        if (totalPages <= keepFront + keepEnd) return; // 不需要移除

        int removeStart = pageBreakIndices[keepFront];
        int removeEnd = pageBreakIndices[totalPages - keepEnd];

        for (int i = removeEnd - 1; i >= removeStart; i--)
        {
            paragraphs[i].Remove();
        }
    }

    [RelayCommand]
    //Url查看指令
    private static void OpenUrl(string url)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            var box = MessageBoxManager
                .GetMessageBoxStandard("错误", "无法打开网址：" + ex.Message);
            box.ShowAsync();
        }
    }
}
