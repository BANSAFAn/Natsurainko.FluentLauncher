using FluentLauncher.Infra.UI.Navigation;
using Microsoft.UI.Xaml.Controls;
using Natsurainko.FluentLauncher.ViewModels.Settings;

namespace Natsurainko.FluentLauncher.Views.Settings;

public sealed partial class AboutPage : Page, IBreadcrumbBarAware
{
    public string Route => "About";

    AboutViewModel VM => (AboutViewModel)DataContext;

    public AboutPage()
    {
        InitializeComponent();
    }
}
