// Copyright (c) 2026 Otto
// Лицензия: MIT (см. LICENSE)

using Android.App;
using Android.Runtime;

namespace SalaryApp.Platforms.Android
{
    // MainApplication определяет базовый класс приложения Android
    [Application]
    public class MainApplication(nint handle, JniHandleOwnership ownership) : MauiApplication(handle, ownership)
    {
        // CreateMauiApp инициализирует MAUI
        protected override MauiApp CreateMauiApp()
        {
            return MauiProgram.CreateMauiApp();
        }
    }
}