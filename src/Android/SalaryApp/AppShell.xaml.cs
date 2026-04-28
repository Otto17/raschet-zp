// Copyright (c) 2026 Otto
// Лицензия: MIT (см. LICENSE)

namespace SalaryApp
{
    // AppShell определяет структуру навигации приложения
    public partial class AppShell : Shell
    {
        // AppShell инициализирует маршруты навигации
        public AppShell()
        {
            InitializeComponent();

            // Регистрируем маршрут для страницы ввода выплаты
            Routing.RegisterRoute(nameof(EntryPage), typeof(EntryPage));
        }
    }
}