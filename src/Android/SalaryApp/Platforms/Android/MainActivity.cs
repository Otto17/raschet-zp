// Copyright (c) 2026 Otto
// Лицензия: MIT (см. LICENSE)

using Android.App;
using Android.Content.PM;

namespace SalaryApp.Platforms.Android
{
    // MainActivity является основной точкой входа Android-приложения
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges =
            ConfigChanges.ScreenSize |
            ConfigChanges.Orientation |
            ConfigChanges.UiMode |
            ConfigChanges.ScreenLayout |
            ConfigChanges.SmallestScreenSize |
            ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
    }
}