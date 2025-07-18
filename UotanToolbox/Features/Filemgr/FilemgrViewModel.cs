using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using ReactiveUI;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UotanToolbox.Common;
using UotanToolbox.Features.Appmgr;

namespace UotanToolbox.Features.Filemgr;

public partial class FilemgrViewModel : MainPageBase
{
    [ObservableProperty]
    private bool isBusy = false;
    [ObservableProperty]
    private bool isOpearteBusy = false;
    [ObservableProperty]
    private string opearteBusyText = "";

    [ObservableProperty]
    bool _sBoxEnabled = false;

    [ObservableProperty]
    public string _nowDir = "None"; 

    [ObservableProperty]
    private string _search;
     
    [ObservableProperty]
    private ObservableCollection<FileInfo> fileinfos = [];

    [ObservableProperty]
    private string _sBoxWater = GetTranslation("Filemgr_SearchApp");


    private static string GetTranslation(string key)
    {
        return FeaturesHelper.GetTranslation(key);
    }

    public FilemgrViewModel() : base(GetTranslation("Sidebar_Filemgr"), MaterialIconKind.File, -700)
    { 

    }

    private static readonly char[] separatorArray = ['\r', '\n'];

    public static string ExtractPackageName(string line)
    {
        string[] parts = line.Split(':');
        if (parts.Length < 2)
        {
            return null;
        }

        string packageNamePart = parts[1];
        int packageNameStartIndex = packageNamePart.LastIndexOf('/') + 1;
        return packageNameStartIndex < packageNamePart.Length
            ? packageNamePart[packageNameStartIndex..]
            : null;
    }

    [RelayCommand]
    public async Task Connect()
    {
        IsBusy = true;
        SBoxEnabled = false;
        SBoxWater = GetTranslation("Filemgr_SearchWait");
        await Task.Run(async () =>
        {
            if (await GetDevicesInfo.SetDevicesInfoLittle())
            {
                MainViewModel sukiViewModel = GlobalData.MainViewModelInstance;
                if (sukiViewModel.Status == GetTranslation("Home_Android"))
                {
                    await CallExternalProgram.ADB($"-s {Global.thisdevice} push \"{Path.Join(Global.runpath, "Push", "list_apps")}\" /data/local/tmp/");
                    await CallExternalProgram.ADB($"-s {Global.thisdevice} shell chmod 777 /data/local/tmp/list_apps");
                    string fulllists = await CallExternalProgram.ADB($"-s {Global.thisdevice} shell /data/local/tmp/list_apps ");
                     
                }
                else
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Global.MainDialogManager.CreateDialog()
                                    .OfType(NotificationType.Error)
                                    .WithTitle(GetTranslation("Common_Error"))
                                    .WithContent(GetTranslation("Common_OpenADBOrHDC"))
                                    .Dismiss().ByClickingBackground()
                                    .TryShow();
                    });
                }
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Global.MainDialogManager.CreateDialog()
                                .OfType(NotificationType.Error)
                                .WithTitle(GetTranslation("Common_Error"))
                                .WithContent(GetTranslation("Common_NotConnected"))
                                .Dismiss().ByClickingBackground()
                                .TryShow();
                });
            }
        });
        SBoxEnabled = true;
        SBoxWater = GetTranslation("Filemgr_SearchApp");
        IsBusy = false;
    }
     
    [RelayCommand]
    public async Task UploadCommand()
    {
        IsBusy = true;
        


        IsBusy = false;
    }

    [RelayCommand]
    public async Task DownloadCommand()
    {
        IsBusy = true; 


        IsBusy = false;
    }
} 
public partial class FileInfo : ObservableObject
{
    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private string name;

    [ObservableProperty]
    private string? displayName;

    [ObservableProperty]
    private string size;

    [ObservableProperty]
    private string otherInfo;
}
