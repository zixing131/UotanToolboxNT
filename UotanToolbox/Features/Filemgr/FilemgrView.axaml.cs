using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.DeviceCommands;
using AdvancedSharpAdbClient.Models;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using HarfBuzzSharp;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UotanToolbox.Common;
using static System.Net.Mime.MediaTypeNames;

namespace UotanToolbox.Features.Filemgr;

public partial class FilemgrView : UserControl
{
    private static string GetTranslation(string key)
    {
        return FeaturesHelper.GetTranslation(key);
    }

    public FilemgrView()
    {
        InitializeComponent();
        this.txt_download.Text = UotanToolbox.Settings.Default.DownloadPath;
        this.txt_upload.Text = UotanToolbox.Settings.Default.UploadPath;

    }

    private void saveConfig()
    {
        UotanToolbox.Settings.Default.DownloadPath = this.txt_download.Text;
        UotanToolbox.Settings.Default.UploadPath = this.txt_upload.Text;
        UotanToolbox.Settings.Default.Save(); 
    }
    private static FilePickerFileType AllPicker { get; } = new("All File")
    {
        Patterns = new[] { "*.*","*"},
        AppleUniformTypeIdentifiers = new[] { "*.*", "*" }
    };

    private void Button_Delete_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private async void Button_Download_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var datacontext = this.DataContext as FilemgrViewModel;
        if (datacontext != null)
        {
            if (datacontext.IsOpearteBusy)
            {
                return;
            }
        }

        datacontext.IsOpearteBusy = true;
        try { 
        datacontext.OpearteBusyText = "开始下载";
            saveConfig();


        string filepath = txt_download.Text.Trim(); 
        if (string.IsNullOrWhiteSpace(filepath))
        {
            Global.MainToastManager.CreateToast()
                                           .OfType(NotificationType.Information)
                                           .WithTitle("错误提示")
                                           .WithContent($"路径不能为空")
                                           .Dismiss().ByClicking()
                                           .Dismiss().After(TimeSpan.FromSeconds(3))
                                           .Queue();
            return;
        }
        string path =  Path.Combine(Directory.GetCurrentDirectory(), "download");
            if(Global.System == "macOS")
            {
                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "filedownload");
            }
        try
        {
            if (Directory.Exists(path) == false)
            {
                Directory.CreateDirectory(path);
            }
        }
        catch (Exception ex)
        {

        }


        MainViewModel sukiViewModel = GlobalData.MainViewModelInstance;
        if (sukiViewModel.Status == GetTranslation("Home_Android"))
        {

            datacontext.OpearteBusyText = $"正在下载: {filepath}";

            var adbDevice = Global.adbClient.GetDevices().Where(p => p.Serial == Global.thisdevice).First();

            if(adbDevice==null || adbDevice.IsEmpty ||  adbDevice.State == DeviceState.Offline || adbDevice.State == DeviceState.Unknown || adbDevice.State == DeviceState.Unauthorized || adbDevice.State == DeviceState.NoPermissions)
            {
                return;
            } 
            using (SyncService service = new SyncService(adbDevice))
            {
                List<string> filesToPull = new List<string>();
                if (filepath.Contains("*"))
                {
                    string filename = Path.GetFileName(filepath);
                    string pathoffile = filepath.Replace(filename, "");
                    var files = service.GetDirectoryListing(pathoffile).ToList();
                    foreach (FileStatistics fileinfo in files)
                    {
                        if ((fileinfo.FileMode & UnixFileStatus.Directory) == UnixFileStatus.Directory)
                        {
                            continue;
                        }
                        string targetName = Path.GetFileName(fileinfo.Path);

                        string pattern = filename;
                        string regexPattern = pattern.Replace("*", "(.*?)");

                        if (Regex.IsMatch(targetName, regexPattern))
                        {
                            filesToPull.Add(pathoffile + fileinfo.Path);
                        }
                    }
                }
                else
                {
                    filesToPull.Add(filepath);
                }
                if (filesToPull.Count == 0)
                {
                    Global.MainDialogManager.CreateDialog()
                    .OfType(NotificationType.Error)
                    .WithTitle(GetTranslation("Common_Error"))
                    .WithContent( "未找到要下载的文件！")
                    .Dismiss().ByClickingBackground()
                    .TryShow();
                    return;
                }
                foreach (var filetopull in filesToPull)
                {
                    try
                    {

                        datacontext.OpearteBusyText ="正在下载 " + Path.GetFileName(filetopull);
                        using (FileStream stream = File.OpenWrite(Path.Combine( path , Path.GetFileName(filetopull))))
                        {
                            await service.PullAsync(filetopull, stream, (process) =>
                            {
                                int percent = (int)(process.ProgressPercentage);
                                datacontext.OpearteBusyText = $"正在下载: {Path.GetFileName(filetopull)} 进度:{percent:D2}%";
                            });
                        }
                        if (isCancelled)
                        {
                            break;
                        } 
                    }
                    catch (Exception ex)
                    {
                        if (isCancelled)
                        {
                            break;
                        }
                    }
                }
            } 
        }
        else if (sukiViewModel.Status == GetTranslation("Home_OpenHOS"))
        {

        }
        else
        {
            Global.MainDialogManager.CreateDialog()
                    .OfType(NotificationType.Error)
                    .WithTitle(GetTranslation("Common_Error"))
                    .WithContent(GetTranslation("Common_OpenADBOrHDC"))
                    .Dismiss().ByClickingBackground()
                    .TryShow();
        }
        }
        finally
        {
            datacontext.OpearteBusyText = "结束下载";
            datacontext.IsOpearteBusy = false; 
        }

    } 

    bool isCancelled = false;
    private async void Button_Upload_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {

        var datacontext = this.DataContext as FilemgrViewModel;
        if (datacontext != null) {
            if (datacontext.IsOpearteBusy)
            {
                return;
            }

        }

        saveConfig();
        TopLevel topLevel = TopLevel.GetTopLevel(this);
        System.Collections.Generic.IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open File",
            AllowMultiple = true,
            FileTypeFilter = new[] { AllPicker, FilePickerFileTypes.TextPlain }
        });
        if (files.Count >= 1)
        {
            var adbDevice = Global.adbClient.GetDevices().Where(p => p.Serial == Global.thisdevice).FirstOrDefault();

            if (adbDevice == null || adbDevice.IsEmpty || adbDevice.State == DeviceState.Offline || adbDevice.State == DeviceState.Unknown || adbDevice.State == DeviceState.Unauthorized || adbDevice.State == DeviceState.NoPermissions)
            {
                return;
            }
            if (adbDevice != null)
            {
                datacontext.IsOpearteBusy = true;
                datacontext.OpearteBusyText = "开始上传";
                try
                {
                    for (int i = 0; i < files.Count; i++)
                    {
                        string filepath = files[i].TryGetLocalPath();

                        MainViewModel sukiViewModel = GlobalData.MainViewModelInstance;
                        if (sukiViewModel.Status == GetTranslation("Home_Android"))
                        {

                            datacontext.OpearteBusyText = $"正在上传: {filepath}";

                            //await CallExternalProgram.ADB($"-s {Global.thisdevice} push {filepath} /sdcard");
                            
                            try
                            {
                                string sdcard = txt_upload.Text;
                                if (sdcard.EndsWith("/") == false)
                                {
                                    sdcard = sdcard + "/";
                                }
                                using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                                {
                                    await Global.adbClient.PushAsync(adbDevice, sdcard + Path.GetFileName(filepath), fs, UnixFileStatus.AllPermissions, DateTimeOffset.Now, new Action<SyncProgressChangedEventArgs>((process) =>
                                    {
                                        int percent = (int)(process.ProgressPercentage);
                                        datacontext.OpearteBusyText = $"正在上传: {filepath} 进度:{percent:D2}%";
                                        if (process.ProgressPercentage >= 100)
                                        {
                                            //successcount++;
                                        }
                                    }));
                                }
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                        else if (sukiViewModel.Status == GetTranslation("Home_OpenHOS"))
                        {

                        }
                        else
                        {
                            Global.MainDialogManager.CreateDialog()
                                  .OfType(NotificationType.Error)
                                  .WithTitle(GetTranslation("Common_Error"))
                                  .WithContent(GetTranslation("Common_OpenADBOrHDC"))
                                  .Dismiss().ByClickingBackground()
                                  .TryShow();
                        }

                    }
                }
                finally
                {
                    datacontext.OpearteBusyText = "结束上传";
                    datacontext.IsOpearteBusy = false; 
                }
            }
        }
    }
}