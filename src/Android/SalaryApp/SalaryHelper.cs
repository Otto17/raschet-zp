// Copyright (c) 2026 Otto
// Лицензия: MIT (см. LICENSE)

using System.Globalization;

namespace SalaryApp
{
    // SalaryHelper предоставляет методы для форматирования данных и вычислений
    public static class SalaryHelper
    {
        public static readonly CultureInfo RuCulture = new("ru-RU");

        public static readonly string[] MonthNames =
        [
            "Январь","Февраль","Март","Апрель","Май","Июнь",
            "Июль","Август","Сентябрь","Октябрь","Ноябрь","Декабрь"
        ];

        public static readonly string[] MonthNamesGenitiv =
        [
            "Января","Февраля","Марта","Апреля","Мая","Июня",
            "Июля","Августа","Сентября","Октября","Ноября","Декабря"
        ];

        // MonthName возвращает название месяца
        public static string MonthName(DateTime d) => MonthNames[d.Month - 1];

        // MonthGenitiv возвращает название месяца в родительном падеже
        public static string MonthGenitiv(DateTime d) => MonthNamesGenitiv[d.Month - 1];

        // FormatMoney возвращает сумму в отформатированном виде
        public static string FormatMoney(decimal value)
        {
            return value == Math.Floor(value)
                ? value.ToString("N0", RuCulture)
                : value.ToString("N2", RuCulture);
        }

        // GetExpectedDate вычисляет дату ожидания выплаты
        public static DateTime GetExpectedDate(DateTime forMonth, PaymentType type)
        {
            return type switch
            {
                PaymentType.FirstHalf => SafeDate(forMonth.Year, forMonth.Month, 27),
                PaymentType.SecondHalf => SafeDate(
                    forMonth.AddMonths(1).Year,
                    forMonth.AddMonths(1).Month, 14),
                PaymentType.Cash => SafeDate(
                    forMonth.AddMonths(1).Year,
                    forMonth.AddMonths(1).Month, 29),
                _ => forMonth
            };
        }

        // SafeDate создаёт дату с коррекцией на количество дней в месяце
        public static DateTime SafeDate(int year, int month, int day)
        {
            int max = DateTime.DaysInMonth(year, month);
            return new DateTime(year, month, Math.Min(day, max));
        }

        // Карточки для выбора
        public static readonly string[] CardOptions =
        [
            "Альфа-Банк", "ВТБ", "Райффайзен", "Наличные", "Другое..."
        ];

        // DefaultCardIndex возвращает индекс карты по умолчанию
        public static int DefaultCardIndex(PaymentType type) => type switch
        {
            PaymentType.FirstHalf => 0,
            PaymentType.SecondHalf => 1,
            _ => 3
        };
    }
}