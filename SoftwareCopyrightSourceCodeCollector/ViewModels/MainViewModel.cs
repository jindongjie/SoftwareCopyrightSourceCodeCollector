using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Models.TreeDataGrid;
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
    private async Task GetAllFiles()
    {
        SearchedFileItemsOriginalCollection.Clear();

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

            var tasks = files.Select(file => Task.Run(() =>
            {
                var searchedFileItem = new SearchedFileItem
                {
                    FileName = Path.GetFileName(file),
                    CreationDate = File.GetCreationTime(file).ToString(CultureInfo.InvariantCulture),
                    CodeCount = GetCodeCount(file),
                    FilePath = Path.GetFullPath(file)
                };

                return searchedFileItem;
            })).ToArray();

            var results = await Task.WhenAll(tasks);

            foreach (var item in results)
            {
                SearchedFileItemsOriginalCollection.Add(item);
            }
            SearchedTotalCount = $"共计：{results.Length} 个文件，{results.Sum(item => item.CodeCount)} 行代码";
        }
        catch (Exception ex)
        {
            var box = MessageBoxManager
                .GetMessageBoxStandard("错误", "报错信息：" + ex.Message);
            await box.ShowAsync();
        }
    }

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
    private string _searchedTotalCount = "共计： 个文件， 行代码";

    [ObservableProperty]
    private string _choseFileType = "txt;docx";

    [ObservableProperty]
    private ObservableCollection<SearchedFileItem> _searchedFileItemsOriginalCollection = [];

    public MainViewModel()
    {
        SearchedFileItemsOriginalSource = new FlatTreeDataGridSource<SearchedFileItem>(_searchedFileItemsOriginalCollection)
        {
            Columns =
            {
                new TextColumn<SearchedFileItem, string>("文件名", x => x.FileName),
                new TextColumn<SearchedFileItem, string>("创建日期", x => x.CreationDate),
                new TextColumn<SearchedFileItem, int>("代码量", x => x.CodeCount),
            },
        };
    }

    public FlatTreeDataGridSource<SearchedFileItem> SearchedFileItemsOriginalSource { get; }

    public partial class SearchedFileItem : ViewModelBase
    {

        [ObservableProperty]
        private string _fileName = string.Empty;

        [ObservableProperty]
        private string _creationDate = string.Empty;

        [ObservableProperty]
        private int _codeCount;

        public string FilePath { get; init; } = string.Empty;
    }

    [ObservableProperty] private string _softwareName = string.Empty;
    [ObservableProperty] private string _softwareVersion = "V1.0";
    [ObservableProperty] private string _softwareAuthor = string.Empty;
    [ObservableProperty] private int _maxPage = 30;


    [RelayCommand]
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
                    FileTypeChoices = new[] { new FilePickerFileType("Word Document") { Patterns = new[] { "*.docx" } } },
                    DefaultExtension = "docx"
                });

                if (savePicker != null)
                {
                    await using var stream = new FileStream(savePicker.Path.LocalPath, FileMode.Create, FileAccess.ReadWrite);
                    await ExportToDocx(stream);
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

    private async Task ExportToDocx(Stream stream)
    {
        using var wordDocument = WordprocessingDocument.Create(stream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
        var mainPart = wordDocument.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = mainPart.Document.AppendChild(new Body());

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

        var totalLineNumber = 1;

        foreach (var item in SearchedFileItemsOriginalCollection)
        {
            if (item.CodeCount == -1) continue;
            var para = body.AppendChild(new Paragraph());
            var run = para.AppendChild(new Run());
            run.AppendChild(new Text($"文件: {item.FileName}"));
            run.RunProperties = new RunProperties(new Bold());

            var lines = await File.ReadAllLinesAsync(item.FilePath);

            foreach (var t in lines)
            {
                // 过滤掉无效或非法的字符
                var sanitizedLine = new string(t.Where(c => !char.IsControl(c) || c == '\r' || c == '\n' || c == '\t').ToArray());

                var linePara = body.AppendChild(new Paragraph());
                var lineRun = linePara.AppendChild(new Run());
                lineRun.AppendChild(new Text($"{totalLineNumber}\t{sanitizedLine}"));
                lineRun.RunProperties = new RunProperties(new FontSize() { Val = "20" });
                totalLineNumber++;
            }
            body.AppendChild(new Paragraph(new Run(new Break() { Type = BreakValues.Page })));
        }

        // 保存文档
        mainPart.Document.Save();

        //RemoveMiddlePages(wordDocument, MaxPage);

        var box = MessageBoxManager
            .GetMessageBoxStandard("恭喜", "导出成功");
        await box.ShowAsync();
    }

    private static void RemoveMiddlePages(WordprocessingDocument wordDocument, int maxPages)
    {
        var body = wordDocument.MainDocumentPart?.Document.Body;
        if (body == null) return;

        var paragraphs = body.Elements<Paragraph>().ToList();
        var pageBreaks = new List<int>();

        // 查找所有分页符的位置
        for (var i = 0; i < paragraphs.Count; i++)
        {
            if (paragraphs[i].Descendants<Break>().Any(b => b.Type != null && b.Type.Value == BreakValues.Page))
            {
                pageBreaks.Add(i);
            }
        }

        // 如果总页数小于等于保留的页数，则不需要删除
        if (pageBreaks.Count <= maxPages * 2)
        {
            return;
        }

        // 计算需要删除的段落范围
        var start = pageBreaks[maxPages];
        var end = pageBreaks[pageBreaks.Count - maxPages - 1];

        // 删除中间的段落
        for (var i = start; i < end; i++)
        {
            paragraphs[i].Remove();
        }

        wordDocument.MainDocumentPart?.Document.Save();
    }
    [RelayCommand]
    private void OpenUrl(string url)
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
