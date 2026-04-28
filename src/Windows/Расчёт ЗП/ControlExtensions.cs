// Copyright (c) 2026 Otto
// Лицензия: MIT (см. LICENSE)

using System.Reflection;
using System.Windows.Forms;

namespace Расчёт_ЗП
{
    // Расширения для контролов
    internal static class ControlExtensions
    {
        // SetStyle_DoubleBuffer включает двойную буферизацию для устранения мерцания при перерисовке
        public static void SetStyle_DoubleBuffer(this Control control)
        {
            typeof(Control)
                .GetMethod("SetStyle", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(control,
                [
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.AllPaintingInWmPaint  |
                    ControlStyles.UserPaint,
                    true
                ]);
        }
    }
}