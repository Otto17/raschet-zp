// Copyright (c) 2026 Otto
// Лицензия: MIT (см. LICENSE)

using Microsoft.Maui.Controls.Shapes;

namespace SalaryApp
{
    // MainPage отображает основной список выплат
    public partial class MainPage : ContentPage
    {
        // ─── Цвета ────────────────────────────────────────────────
        private static readonly Color ColorHeader   = Color.FromArgb("#1E1E3C");
        private static readonly Color ColorPaid     = Color.FromArgb("#DCFFDC");
        private static readonly Color ColorUnpaid   = Color.FromArgb("#FFF5C8");
        private static readonly Color ColorAccent   = Color.FromArgb("#3264C8");
        private static readonly Color ColorTotal    = Color.FromArgb("#E6F0FF");
        private static readonly Color ColorBg       = Color.FromArgb("#EBEDF5");
        private static readonly Color ColorExtraBg  = Color.FromArgb("#EBFCEB");
        private static readonly Color ColorDatePaid = Color.FromArgb("#006400");
        private static readonly Color ColorDateWait = Color.FromArgb("#825500");
        private static readonly Color ColorAmtPaid  = Color.FromArgb("#007800");
        private static readonly Color ColorDelRed   = Color.FromArgb("#B43C3C");

        private SalaryStorage _storage = null!; // Хранилище данных
        private bool _initialized = false;      // Флаг первичной инициализации

        public MainPage()
        {
            InitializeComponent();
        }

        // OnAppearing выполняет подготовку данных при появлении страницы
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _storage = SalaryStorage.Load();

            // Выполняет инициализацию только при первом запуске
            if (!_initialized)
            {
                _initialized = true;

                if (_storage.StartMonth == DateTime.MinValue
                    && _storage.ExtraMonths.Count == 0
                    && _storage.Entries.Count == 0)
                {
                    var start = await AskMonth("Первый месяц учёта");
                    if (start == null)
                    {
                        RefreshAll();
                        return;
                    }
                    _storage.StartMonth = start.Value;
                    _storage.AddExtraMonth(start.Value);
                    _storage.Save();
                }
            }

            RefreshAll();
        }

        // RefreshAll обновляет список карточек в интерфейсе
        private void RefreshAll()
        {
            // Оставляем только кнопку «Добавить месяц», убираем карточки
            while (CardsStack.Children.Count > 1)
                CardsStack.Children.RemoveAt(CardsStack.Children.Count - 1);

            var monthSet = new HashSet<DateTime>();

            if (_storage.StartMonth != DateTime.MinValue)
                monthSet.Add(new DateTime(_storage.StartMonth.Year, _storage.StartMonth.Month, 1));

            foreach (var m in _storage.ExtraMonths)
                monthSet.Add(new DateTime(m.Year, m.Month, 1));

            foreach (var entry in _storage.Entries)
                monthSet.Add(new DateTime(entry.ForMonth.Year, entry.ForMonth.Month, 1));

            var months = monthSet.OrderByDescending(m => m).ToList();

            foreach (var month in months)
                CardsStack.Children.Add(BuildMonthCard(month));
        }

        // BuildMonthCard формирует визуальную карточку месяца
        private Border BuildMonthCard(DateTime month)
        {
            var e1 = _storage.Find(month, PaymentType.FirstHalf);
            var e2 = _storage.Find(month, PaymentType.SecondHalf);
            var e3 = _storage.Find(month, PaymentType.Cash);

            var exp1 = SalaryHelper.GetExpectedDate(month, PaymentType.FirstHalf);
            var exp2 = SalaryHelper.GetExpectedDate(month, PaymentType.SecondHalf);
            var exp3 = SalaryHelper.GetExpectedDate(month, PaymentType.Cash);

            decimal total = 0;
            if (e1 != null) total += e1.Amount + e1.Extras.Sum(x => x.Amount);
            if (e2 != null) total += e2.Amount + e2.Extras.Sum(x => x.Amount);
            if (e3 != null) total += e3.Amount + e3.Extras.Sum(x => x.Amount);

            bool allPaid = e1 != null && e2 != null && e3 != null;

            // Внешняя рамка карточки
            var card = new Border
            {
                BackgroundColor = ColorBg,
                StrokeShape = new RoundRectangle { CornerRadius = 10 },
                StrokeThickness = 0,
                Padding = 0,
                Shadow = new Shadow
                {
                    Brush = Brush.Black,
                    Offset = new Point(0, 2),
                    Radius = 6,
                    Opacity = 0.15f
                }
            };

            var cardStack = new VerticalStackLayout { Spacing = 0 };

            // Заголовок карточки
            var headerGrid = new Grid
            {
                BackgroundColor = ColorHeader,
                Padding = new Thickness(12, 6),
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            headerGrid.Add(new Label
            {
                Text = $"За {SalaryHelper.MonthName(month)} {month.Year}",
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                VerticalOptions = LayoutOptions.Center
            }, 0);

            var btnDel = new Button
            {
                Text = "✕",
                BackgroundColor = ColorDelRed,
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                FontSize = 12,
                CornerRadius = 6,
                HeightRequest = 32,
                WidthRequest = 40,
                Padding = 0
            };
            var capturedMonth = month;
            btnDel.Clicked += async (s, e) => await DeleteMonth(capturedMonth);
            headerGrid.Add(btnDel, 1);

            cardStack.Children.Add(headerGrid);

            // Строки выплат
            cardStack.Children.Add(BuildPaymentRow(month, PaymentType.FirstHalf, e1, exp1));
            cardStack.Children.Add(BuildPaymentRow(month, PaymentType.SecondHalf, e2, exp2));
            cardStack.Children.Add(BuildPaymentRow(month, PaymentType.Cash, e3, exp3));

            // Доп. выплаты
            foreach (var entry in new[] { e1, e2, e3 })
            {
                if (entry?.Extras == null) continue;
                foreach (var ex in entry.Extras)
                    cardStack.Children.Add(BuildExtraRow(ex));
            }

            // Итого
            string totalStr = allPaid
                ? $"Итого: {SalaryHelper.FormatMoney(total)} ₽"
                : total > 0
                    ? $"Получено: {SalaryHelper.FormatMoney(total)} ₽ (ожидается ещё)"
                    : "Ожидается...";

            cardStack.Children.Add(new Label
            {
                Text = totalStr,
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = ColorHeader,
                BackgroundColor = allPaid ? ColorPaid : ColorTotal,
                HorizontalTextAlignment = TextAlignment.End,
                Padding = new Thickness(8, 6)
            });

            card.Content = cardStack;
            return card;
        }

        // BuildPaymentRow генерирует строку выплаты
        private Grid BuildPaymentRow(
            DateTime month,
            PaymentType type,
            SalaryEntry? entry,
            DateTime expectedDate)
        {
            bool isPaid = entry != null;
            string dateText = isPaid
                ? entry!.Date.ToString("dd.MM.yyyy")
                : $"~{expectedDate:dd.MM.yyyy}";

            string cardPart = isPaid
                ? (string.IsNullOrWhiteSpace(entry!.CardName) ? "?" : entry.CardName)
                : "?";

            string desc = type switch
            {
                PaymentType.FirstHalf  =>
                    $"1-я половина {SalaryHelper.MonthGenitiv(month)}",
                PaymentType.SecondHalf =>
                    $"2-я половина {SalaryHelper.MonthGenitiv(month)}",
                _                      => "Наличными"
            };
            string fullDesc = $"{desc} → {cardPart}";

            decimal rowTotal = 0;
            if (isPaid)
            {
                rowTotal = entry!.Amount;
                if (entry.Extras != null)
                    rowTotal += entry.Extras.Sum(x => x.Amount);
            }

            var rowGrid = new Grid
            {
                BackgroundColor = isPaid ? ColorPaid : ColorUnpaid,
                Padding = new Thickness(6, 5),
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(86) },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = new GridLength(44) }
                },
                ColumnSpacing = 4
            };

            // Дата
            rowGrid.Add(new Label
            {
                Text = dateText,
                FontSize = 11,
                FontAttributes = isPaid ? FontAttributes.None : FontAttributes.Italic,
                TextColor = isPaid ? ColorDatePaid : ColorDateWait,
                VerticalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center
            }, 0);

            // Описание
            rowGrid.Add(new Label
            {
                Text = fullDesc,
                FontSize = 11,
                TextColor = Color.FromArgb("#23234B"),
                VerticalOptions = LayoutOptions.Center,
                LineBreakMode = LineBreakMode.TailTruncation
            }, 1);

            // Сумма
            rowGrid.Add(new Label
            {
                Text = isPaid ? $"{SalaryHelper.FormatMoney(rowTotal)} ₽" : "?",
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = isPaid ? ColorAmtPaid : Colors.Gray,
                VerticalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.End
            }, 2);

            // Кнопка ＋/✎
            var btn = new Button
            {
                Text = isPaid ? "✎" : "＋",
                BackgroundColor = isPaid
                    ? Color.FromArgb("#A0D2A0")
                    : ColorAccent,
                TextColor = isPaid
                    ? Color.FromArgb("#005000")
                    : Colors.White,
                FontAttributes = FontAttributes.Bold,
                FontSize = 14,
                CornerRadius = 6,
                Padding = 0,
                HeightRequest = 34,
                WidthRequest = 40
            };

            var cm = month; var ct = type;
            var ce = entry; var cd = expectedDate;
            btn.Clicked += async (s, e) => await OpenEntryPage(cm, ct, ce, cd);
            rowGrid.Add(btn, 3);

            return rowGrid;
        }

        // BuildExtraRow создает строку для доплаты
        private static Label BuildExtraRow(ExtraPayment ex)
        {
            return new Label
            {
                Text = $"  + {ex.Label}: {SalaryHelper.FormatMoney(ex.Amount)} ₽",
                FontSize = 11,
                FontAttributes = FontAttributes.Italic,
                TextColor = Color.FromArgb("#006E00"),
                BackgroundColor = Color.FromArgb("#EBFCEB"),
                Padding = new Thickness(10, 3)
            };
        }

        // OpenEntryPage открывает модальное окно редактирования
        private async Task OpenEntryPage(
            DateTime month, PaymentType type,
            SalaryEntry? existing, DateTime suggested)
        {
            var page = new EntryPage(_storage, month, type, existing, suggested);
            await Navigation.PushModalAsync(page);
            // После возврата — обновляет список
            page.Disappearing += (s, e) => RefreshAll();
        }

        // BtnAddMonth_Clicked инициирует добавление месяца
        private async void BtnAddMonth_Clicked(object sender, EventArgs e)
        {
            var month = await AskMonth("Добавить месяц");
            if (month == null) return;
            _storage.AddExtraMonth(month.Value);

            // Если стартовый месяц ещё не задан — задаёт
            if (_storage.StartMonth == DateTime.MinValue)
                _storage.StartMonth = month.Value;

            _storage.Save();
            RefreshAll();
        }

        // DeleteMonth удаляет данные месяца, так как пользователь подтвердил удаление
        private async Task DeleteMonth(DateTime month)
        {
            string monthStr = $"{SalaryHelper.MonthName(month)} {month.Year}";
            bool hasEntries = _storage.Entries.Any(e =>
                e.ForMonth.Year == month.Year && e.ForMonth.Month == month.Month);

            string warn = hasEntries
                ? $"В месяце «{monthStr}» есть внесённые выплаты.\nОни тоже будут удалены.\n\nУдалить месяц?"
                : $"Удалить месяц «{monthStr}»?";

            bool confirm = await DisplayAlert(
                "Удаление месяца", warn, "Удалить", "Отмена");
            if (!confirm) return;

            _storage.Entries.RemoveAll(e =>
                e.ForMonth.Year == month.Year && e.ForMonth.Month == month.Month);
            _storage.ExtraMonths.RemoveAll(m =>
                m.Year == month.Year && m.Month == month.Month);

            if (_storage.StartMonth.Year == month.Year &&
                _storage.StartMonth.Month == month.Month)
            {
                var remaining = new HashSet<DateTime>();
                foreach (var m in _storage.ExtraMonths)
                    remaining.Add(new DateTime(m.Year, m.Month, 1));
                foreach (var entry in _storage.Entries)
                    remaining.Add(new DateTime(entry.ForMonth.Year, entry.ForMonth.Month, 1));

                _storage.StartMonth = remaining.Count > 0
                    ? remaining.OrderBy(m => m).First()
                    : DateTime.MinValue;
            }

            _storage.Save();
            RefreshAll();
        }

        // AskMonth запрашивает у пользователя месяц и год
        private async Task<DateTime?> AskMonth(string title)
        {
            // Выбор месяца
            string? monthName = await DisplayActionSheet(
                title + " — выберите месяц",
                "Отмена", null,
                SalaryHelper.MonthNames);

            if (monthName == null || monthName == "Отмена") return null;

            int monthIndex = Array.IndexOf(SalaryHelper.MonthNames, monthName);
            if (monthIndex < 0) return null;

            // Ввод года вручную — по умолчанию подставляется текущий год
            string? customYear = await DisplayPromptAsync(
                "Укажите год",
                "Год:",
                "OK", "Отмена",
                initialValue: DateTime.Now.Year.ToString(),
                keyboard: Keyboard.Numeric);

            if (string.IsNullOrWhiteSpace(customYear)) return null;
            if (!int.TryParse(customYear.Trim(), out int year) || year < 2000 || year > 2100)
            {
                await DisplayAlert("Ошибка", "Введите корректный год (2000–2100).", "OK");
                return null;
            }

            return new DateTime(year, monthIndex + 1, 1);
        }
    }
}