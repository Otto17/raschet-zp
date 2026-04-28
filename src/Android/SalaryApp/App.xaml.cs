// Copyright (c) 2026 Otto
// Лицензия: MIT (см. LICENSE)

namespace SalaryApp
{
    // App управляет жизненным циклом приложения
    public partial class App : Application
    {
        // App инициализирует ресурсы
        public App()
        {
            InitializeComponent();
        }

        // CreateWindow создаёт основное окно с оболочкой приложения
        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}