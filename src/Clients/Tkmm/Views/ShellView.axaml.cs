using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FluentAvalonia.UI.Windowing;
using Tkmm.Components;
using Tkmm.Models;

namespace Tkmm.Views;

public partial class ShellView : AppWindow
{
    public ShellView()
    {
        InitializeComponent();
        SplashScreen = new SplashScreen();

        TitleBar.ExtendsContentIntoTitleBar = true;
        TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;

        Bitmap bitmap = new(AssetLoader.Open(new Uri("icon.ico"), App.AssetsUri));
        Icon = bitmap.CreateScaledBitmap(new PixelSize(48, 48));

        PageManager.Shared.PropertyChanged += (_, e) => {
            if (e.PropertyName == nameof(Tkmm.Components.PageManager.Current)) {
                MainNavigation.Content = PageManager.Shared.Current?.Content;
            }
        };
    }
}
