// Copyright (c) 2026 Otto
// Лицензия: MIT (см. LICENSE)

namespace SalaryApp
{
    // MauiProgram настраивает приложение MAUI
    public static class MauiProgram
    {
        // CreateMauiApp создаёт экземпляр приложения
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            return builder.Build();
        }
    }
}