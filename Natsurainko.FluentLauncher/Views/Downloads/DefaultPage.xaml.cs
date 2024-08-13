using FluentLauncher.Infra.UI.Navigation;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Natsurainko.FluentLauncher.Views.Downloads;

public sealed partial class DefaultPage : Page, IBreadcrumbBarAware
{
    public string Route => "Download";

    public DefaultPage()
    {
        this.InitializeComponent();
    }

    private void Page_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        GC.Collect();
    }
}
