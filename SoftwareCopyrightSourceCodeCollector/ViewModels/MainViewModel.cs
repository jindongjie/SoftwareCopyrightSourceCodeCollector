using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using SoftwareCopyrightSourceCodeCollector.CoreLib;
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
            var items = await Core.ScanFilesAsync(SelectedFolder, endWithList);
            //遍历所有文件，挨个添加
            foreach (var item in items)
            {
                SearchedFileItemsOriginalCollection.Add(new SearchedFileItem
                {
                    FileName = item.FileName,
                    CreationDate = item.CreationDate,
                    CodeCount = item.CodeCount,
                    FilePath = item.FilePath,
                    OrderNumber = item.OrderNumber,
                    Parent = this
                });
            }
            //更新至提示字段
            SearchedTotalCount = $"共计：{items.Count} 个文件，{items.Sum(item => item.CodeCount)} 行代码";
        }
        catch (Exception ex)
        {
            var box = MessageBoxManager
                .GetMessageBoxStandard("错误", "报错信息：" + ex.Message);
            await box.ShowAsync();
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
            var fileItems = SearchedFileItemsOriginalCollection
                .Select(f => new FileItem
                {
                    FileName = f.FileName,
                    CreationDate = f.CreationDate,
                    CodeCount = f.CodeCount,
                    FilePath = f.FilePath,
                    OrderNumber = f.OrderNumber
                })
                .ToList();

            var info = new SoftwareInfo
            {
                SoftwareName = SoftwareName,
                SoftwareAuthor = SoftwareAuthor,
                SoftwareVersion = SoftwareVersion,
            };

            var warnings = await Core.ExportToDocxAsync(stream, info, fileItems, MaxPage);

            if (warnings.Count > 0)
            {
                var errorMsg = "以下文件读取失败或被跳过：\n" + string.Join("\n", warnings);
                await MessageBoxManager.GetMessageBoxStandard("警告", errorMsg).ShowAsync();
            }

            await MessageBoxManager.GetMessageBoxStandard("恭喜", "导出成功").ShowAsync();
        }
        catch (ArgumentException ex)
        {
            await MessageBoxManager.GetMessageBoxStandard("错误", ex.Message).ShowAsync();
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard("错误", $"导出过程中发生未处理异常：{ex.Message}").ShowAsync();
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


    [ObservableProperty]
    private string _softwareFullName = string.Empty;

    [ObservableProperty]
    private string _softwareShortName = string.Empty;

    [ObservableProperty]
    private string _softwareVersionNumber = "V1.0";

    [ObservableProperty]
    private ComboBoxItem _softwareCategory;

    [ObservableProperty]
    private string? _developmentFinishDate;

    [ObservableProperty]
    private ComboBoxItem _developmentMethod;

    [ObservableProperty]
    private string _softwareDescription = string.Empty;

    [ObservableProperty]
    private ComboBoxItem _publishStatus;

    [ObservableProperty]
    private string _copyrightOwner = string.Empty;

    [ObservableProperty]
    private ComboBoxItem _rightsScope;

    [ObservableProperty]
    private ComboBoxItem _rightsAcquisitionMethod;

    [ObservableProperty]
    private string _developmentHardwareEnvironment = string.Empty;

    [ObservableProperty]
    private string _runtimeHardwareEnvironment = string.Empty;

    [ObservableProperty]
    private string _developmentOS = string.Empty;

    [ObservableProperty]
    private string _developmentTool = string.Empty;

    [ObservableProperty]
    private string _runtimePlatform = string.Empty;

    [ObservableProperty]
    private string _runtimeSupportSoftware = string.Empty;

    [ObservableProperty]
    private ComboBoxItem _programmingLanguage;

    [ObservableProperty]
    private string _programmingLanguageOther = string.Empty;


    [ObservableProperty]
    private string _sourceCodeAmount = string.Empty;

    [ObservableProperty]
    private string _developmentPurpose = string.Empty;

    [ObservableProperty]
    private string _targetIndustry = string.Empty;

    [ObservableProperty]
    private bool _isAppSoftware;

    [ObservableProperty]
    private bool _isGameSoftware;

    [ObservableProperty]
    private bool _isEducationSoftware;

    [ObservableProperty]
    private bool _isFinanceSoftware;

    [ObservableProperty]
    private bool _isMedicalSoftware;

    [ObservableProperty]
    private bool _isGISSoftware;

    [ObservableProperty]
    private bool _isCloudSoftware;

    [ObservableProperty]
    private bool _isSecuritySoftware;

    [ObservableProperty]
    private bool _isBigDataSoftware;

    [ObservableProperty]
    private bool _isAISoftware;

    [ObservableProperty]
    private bool _isVRSoftware;

    [ObservableProperty]
    private bool _is5GSoftware;

    [ObservableProperty]
    private bool _isMiniProgramSoftware;

    [ObservableProperty]
    private bool _isSmartCitySoftware;
    [ObservableProperty]
    private bool _isIoTSoftware;

    [ObservableProperty]
    private bool _isIndustrialControlSoftware;

    [ObservableProperty]
    private string _mainFunctions = string.Empty;

    [ObservableProperty]
    private string _technicalFeatures = string.Empty;

    [RelayCommand]
    private async Task ExportApplicationTXT()
    {
        if (SoftwareFullName == "")
        {
            await MessageBoxManager.GetMessageBoxStandard("导出报错", "软件全名不能为空！").ShowAsync();
            return;
        }

        if (SoftwareVersionNumber == "")
        {
            await MessageBoxManager.GetMessageBoxStandard("导出报错", "取得方式不能为空！").ShowAsync();
            return;
        }

        if (SourceCodeAmount == "")
        {
            await MessageBoxManager.GetMessageBoxStandard("导出报错", "程序行数不能为空！").ShowAsync();
            return;
        }
        if (MainFunctions == "")
        {
            await MessageBoxManager.GetMessageBoxStandard("导出报错", "主要功能不能为空！").ShowAsync();
            return;
        }

        if (DevelopmentPurpose.Length < 8)
        {
            await MessageBoxManager.GetMessageBoxStandard("导出报错", "开发目的至少需要8字").ShowAsync();
            return;
        }

        if (TargetIndustry.Length < 4)
        {
            await MessageBoxManager.GetMessageBoxStandard("导出报错", "目标领域/行业至少需要4字").ShowAsync();
            return;
        }

        if (MainFunctions.Length < 100)
        {
            await MessageBoxManager.GetMessageBoxStandard("导出报错", "主要功能至少需要100字").ShowAsync();
            return;
        }

        var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (mainWindow == null)
            return;

        var savePicker = await mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "导出为 TXT 文件",
            FileTypeChoices = [new FilePickerFileType("Text File") { Patterns = ["*.txt"] }],
            DefaultExtension = "txt"
        });

        if (savePicker == null)
            return;

        var info = new SoftwareInfo
        {
            SoftwareFullName = SoftwareFullName,
            SoftwareShortName = SoftwareShortName,
            SoftwareVersionNumber = SoftwareVersionNumber,
            RightsAcquisitionMethod = RightsAcquisitionMethod.Content?.ToString() ?? "",
            RightsScope = RightsScope.Content?.ToString() ?? "",
            SoftwareCategory = SoftwareCategory.Content?.ToString() ?? "",
            DevelopmentMethod = DevelopmentMethod.Content?.ToString() ?? "",
            DevelopmentFinishDate = DevelopmentFinishDate,
            PublishStatus = PublishStatus.Content?.ToString() ?? "",
            CopyrightOwner = CopyrightOwner,
            DevelopmentHardwareEnvironment = DevelopmentHardwareEnvironment,
            RuntimeHardwareEnvironment = RuntimeHardwareEnvironment,
            DevelopmentOS = DevelopmentOS,
            DevelopmentTool = DevelopmentTool,
            RuntimePlatform = RuntimePlatform,
            RuntimeSupportSoftware = RuntimeSupportSoftware,
            ProgrammingLanguage = ProgrammingLanguage.Content?.ToString() ?? "",
            ProgrammingLanguageOther = ProgrammingLanguageOther,
            SourceCodeAmount = SourceCodeAmount,
            DevelopmentPurpose = DevelopmentPurpose,
            TargetIndustry = TargetIndustry,
            MainFunctions = MainFunctions,
            TechnicalFeatures = TechnicalFeatures,
            IsAppSoftware = IsAppSoftware,
            IsGameSoftware = IsGameSoftware,
            IsEducationSoftware = IsEducationSoftware,
            IsFinanceSoftware = IsFinanceSoftware,
            IsMedicalSoftware = IsMedicalSoftware,
            IsGISSoftware = IsGISSoftware,
            IsCloudSoftware = IsCloudSoftware,
            IsSecuritySoftware = IsSecuritySoftware,
            IsBigDataSoftware = IsBigDataSoftware,
            IsAISoftware = IsAISoftware,
            IsVRSoftware = IsVRSoftware,
            Is5GSoftware = Is5GSoftware,
            IsMiniProgramSoftware = IsMiniProgramSoftware,
            IsIoTSoftware = IsIoTSoftware,
            IsSmartCitySoftware = IsSmartCitySoftware,
            IsIndustrialControlSoftware = IsIndustrialControlSoftware,
        };

        try
        {
            await Core.ExportToTxtAsync(savePicker.Path.LocalPath, info);
            await MessageBoxManager.GetMessageBoxStandard("导出成功", "申报资料TXT文件已生成").ShowAsync();
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard("错误", $"导出TXT时出错：{ex.Message}").ShowAsync();
        }
    }
}