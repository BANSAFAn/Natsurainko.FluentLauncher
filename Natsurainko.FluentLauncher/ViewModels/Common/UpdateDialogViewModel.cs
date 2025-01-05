﻿#if FLUENT_LAUNCHER_PREVIEW_CHANNEL
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentLauncher.Infra.UI.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Natsurainko.FluentLauncher.Services.Network;
using Natsurainko.FluentLauncher.Utils;
using Nrk.FluentCore.GameManagement.Downloader;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

#nullable disable
namespace Natsurainko.FluentLauncher.ViewModels.Common;

internal partial class UpdateDialogViewModel : ObservableObject, IDialogParameterAware
{
    private JsonNode _releaseJson = null!;
    private ContentDialog _dialog;

    private readonly UpdateService _updateService;

    public UpdateDialogViewModel(UpdateService updateService)
    {
        _updateService = updateService;
    }

    [ObservableProperty]
    public partial string TagName { get; set; }

    [ObservableProperty]
    public partial string Body { get; set; }

    [ObservableProperty]
    public partial string PublishedAt { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressText))]
    public partial double Progress { get; set; }

    [ObservableProperty]
    public partial bool IsIndeterminate { get; set; } = false;

    [ObservableProperty]
    public partial string ActionName { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProrgessVisibility))]
    [NotifyPropertyChangedFor(nameof(Enable))]
    [NotifyCanExecuteChangedFor(nameof(UpdateCommand))]
    public partial bool Running { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProxyBoxVisibility))] 
    public partial bool UseProxy { get; set; }

    [ObservableProperty]
    public partial string ProxyUrl { get; set; }

    public Visibility ProxyBoxVisibility => UseProxy ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ProrgessVisibility => Running ? Visibility.Visible : Visibility.Collapsed;

    public string ProgressText => Progress.ToString("P0");

    public bool Enable => !Running;

    void IDialogParameterAware.HandleParameter(object param)
    {
        _releaseJson = (JsonNode)param;

        TagName = _releaseJson["tag_name"]!.GetValue<string>();
        Body = _releaseJson["body"]!.GetValue<string>();
        PublishedAt = _releaseJson["published_at"]!.GetValue<string>();
    }

    [RelayCommand]
    void LoadEvent(object args)
    {
        var grid = args.As<Grid, object>().sender;
        _dialog = grid.FindName("Dialog") as ContentDialog;
    }

    [RelayCommand]
    void Update() => Task.Run(async () =>
    {
        App.DispatcherQueue.TryEnqueue(() => Running = true);

        #region Check for installer update
        App.DispatcherQueue.TryEnqueue(() => ActionName = "Check Package Installer Update");

        var (installerHasUpate, installerDownloadUrl) = await _updateService.CheckInstallerUpdateRelease();

        if (installerHasUpate)
        {
            App.DispatcherQueue.TryEnqueue(() => ActionName = "Downloading Package Installer");

            // Download installer
            var downloadTask = _updateService.CreatePackageInstallerDownloadTask(installerDownloadUrl!, ProxyUrl);

            downloadTask.BytesDownloaded += (size) =>
            {
                double progress = downloadTask.TotalBytes is null ? 0 : downloadTask.DownloadedBytes / (double)downloadTask.TotalBytes;
                App.DispatcherQueue.TryEnqueue(() => Progress = progress);
            };

            var result = await downloadTask.StartAsync();

            if (result.Type == Nrk.FluentCore.GameManagement.Downloader.DownloadResultType.Failed)
            {
                // Show error dialog
                return;
            }
        }

        #endregion

        #region Download update package

        App.DispatcherQueue.TryEnqueue(() => ActionName = "Downloading Update Package");

        var packageDownloadTask = _updateService.CreateUpdatePackageDownloadTask(_releaseJson, ProxyUrl);
        packageDownloadTask.BytesDownloaded += (size) =>
        {
            double progress = packageDownloadTask.TotalBytes is null ? 0 : packageDownloadTask.DownloadedBytes / (double)packageDownloadTask.TotalBytes;
            App.DispatcherQueue.TryEnqueue(() => Progress = progress);
        };

        var packageResult = await packageDownloadTask.StartAsync();

        if (packageResult.Type == Nrk.FluentCore.GameManagement.Downloader.DownloadResultType.Failed)
        {
            // Show error dialog
            return;
        }

        #endregion

        #region Install update

        App.DispatcherQueue.TryEnqueue(() =>
        {
            ActionName = "Running Package Installer";
            IsIndeterminate = true;
        });

        var (success, error) = await _updateService.RunInstaller();
        if (!success)
        {
            App.DispatcherQueue.TryEnqueue(() =>
            {
                Running = false;
                _dialog.Hide();
            });
        }

        #endregion
    });

    [RelayCommand]
    void Cancel() => _dialog.Hide();
}

#endif