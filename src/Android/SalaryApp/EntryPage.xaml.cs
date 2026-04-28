// Copyright (c) 2026 Otto
// Лицензия: MIT (см. LICENSE)

using System.Globalization;

namespace SalaryApp
{
    // EntryPage управляет вводом и редактированием данных о выплате
    public partial class EntryPage : ContentPage
    {
        private readonly SalaryStorage _storage; // Хранилище данных
        private readonly DateTime _forMonth;     // Месяц редактирования
        private readonly PaymentType _type;      // Тип выплаты
        private readonly SalaryEntry? _existing; // Существующая запись
        private readonly DateTime _suggested;    // Предлагаемая дата

        // Список строк доп. выплат (поле названия + поле суммы)
        private readonly List<(Entry LblEntry, Entry AmtEntry)> _extraRows = [];

        // Цвета
        private static readonly Color ColorAccent = Color.FromArgb("#3264C8");
        private static readonly Color ColorDelRed = Color.FromArgb("#DC5050");
        private static readonly Color ColorHeader = Color.FromArgb("#1E1E3C");

        // EntryPage инициализирует страницу данными
        public EntryPage(
            SalaryStorage storage,
            DateTime forMonth,
            PaymentType type,
            SalaryEntry? existing,
            DateTime suggested)
        {
            InitializeComponent();

            _storage = storage;
            _forMonth = forMonth;
            _type = type;
            _existing = existing;
            _suggested = suggested;

            SetupPage();
        }

        // SetupPage настраивает элементы интерфейса
        private void SetupPage()
        {
            // Заголовок
            LblTitle.Text = _existing == null ? "Внести выплату" : "Изменить выплату";

            // Подзаголовок
            string typeLabel = _type switch
            {
                PaymentType.FirstHalf =>
                    $"1-я половина {SalaryHelper.MonthGenitiv(_forMonth)}",
                PaymentType.SecondHalf =>
                    $"2-я половина {SalaryHelper.MonthGenitiv(_forMonth)}",
                _ =>
                    $"Наличными за {SalaryHelper.MonthGenitiv(_forMonth)}"
            };
            LblSubtitle.Text =
                $"За {SalaryHelper.MonthName(_forMonth)} {_forMonth.Year} — {typeLabel}";

            // Дата
            DtpDate.Date = _existing?.Date.Date ?? _suggested.Date;

            // Сумма
            TxtAmount.Text = _existing != null
                ? _existing.Amount.ToString("F2", SalaryHelper.RuCulture)
                : string.Empty;

            // Карточки
            foreach (var c in SalaryHelper.CardOptions)
                PkrCard.Items.Add(c);

            if (_existing != null && !string.IsNullOrWhiteSpace(_existing.CardName))
            {
                int idx = SalaryHelper.CardOptions
                              .ToList().IndexOf(_existing.CardName);
                if (idx >= 0)
                    PkrCard.SelectedIndex = idx;
                else
                {
                    // Пользовательское название — вставляет перед «Другое...»
                    PkrCard.Items.Insert(PkrCard.Items.Count - 1, _existing.CardName);
                    PkrCard.SelectedIndex = PkrCard.Items.Count - 2;
                }
            }
            else
            {
                PkrCard.SelectedIndex = SalaryHelper.DefaultCardIndex(_type);
            }

            // Поле «Другое...»
            TxtCardCustom.IsVisible =
                PkrCard.Items[PkrCard.SelectedIndex] == "Другое...";

            if (_existing != null
                && !string.IsNullOrEmpty(_existing.CardName)
                && !SalaryHelper.CardOptions.Contains(_existing.CardName))
            {
                TxtCardCustom.Text = _existing.CardName;
            }

            // Кнопка удаления — только при редактировании
            BtnDelete.IsVisible = _existing != null;

            // Загружает существующие доп. выплаты
            if (_existing?.Extras != null)
                foreach (var ex in _existing.Extras)
                    AddExtraRow(ex.Label, ex.Amount);
        }

        // PkrCard_SelectedIndexChanged изменяет видимость поля названия карты, если выбран тип "Другое"
        private void PkrCard_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (PkrCard.SelectedIndex < 0) return;
            TxtCardCustom.IsVisible =
                PkrCard.Items[PkrCard.SelectedIndex] == "Другое...";
        }

        // AddExtraRow создаёт элементы управления для дополнительной выплаты
        private void AddExtraRow(string label = "", decimal amount = 0)
        {
            // Строка: [название] [сумма] [✕]
            var rowGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = new GridLength(110) },
                    new ColumnDefinition { Width = new GridLength(36) }
                },
                ColumnSpacing = 6
            };

            var entryLabel = new Entry
            {
                Placeholder = "Больничный / Командировка",
                Text = label,
                FontSize = 13,
                BackgroundColor = Colors.White,
                TextColor = ColorHeader
            };

            var entryAmt = new Entry
            {
                Placeholder = "Сумма ₽",
                Text = amount > 0
                    ? amount.ToString("F2", SalaryHelper.RuCulture)
                    : string.Empty,
                Keyboard = Keyboard.Numeric,
                FontSize = 13,
                BackgroundColor = Colors.White,
                TextColor = ColorHeader
            };

            var btnDel = new Button
            {
                Text = "✕",
                BackgroundColor = ColorDelRed,
                TextColor = Colors.White,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 6,
                Padding = 0,
                HeightRequest = 36,
                WidthRequest = 36
            };

            rowGrid.Add(entryLabel, 0);
            rowGrid.Add(entryAmt, 1);
            rowGrid.Add(btnDel, 2);

            var row = (entryLabel, entryAmt);
            _extraRows.Add(row);
            ExtrasStack.Children.Add(rowGrid);

            // Удаляет строку, так как пользователь нажал на крестик
            btnDel.Clicked += (s, e) =>
            {
                _extraRows.Remove(row);
                ExtrasStack.Children.Remove(rowGrid);
            };
        }

        // BtnAddExtra_Clicked добавляет новую пустую строку для доплаты
        private void BtnAddExtra_Clicked(object sender, EventArgs e)
            => AddExtraRow();

        // BtnSave_Clicked сохраняет данные, так как пользователь завершил ввод
        private async void BtnSave_Clicked(object sender, EventArgs e)
        {
            // Валидация суммы
            string raw = (TxtAmount.Text ?? "")
                .Trim().Replace(" ", "").Replace(",", ".");

            if (!decimal.TryParse(raw, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out decimal amount)
                || amount <= 0)
            {
                await DisplayAlert("Ошибка",
                    "Введите корректную сумму больше нуля.", "OK");
                TxtAmount.Focus();
                return;
            }

            // Определяет название карты
            string cardName;
            if (PkrCard.SelectedIndex >= 0
                && PkrCard.Items[PkrCard.SelectedIndex] == "Другое...")
            {
                cardName = (TxtCardCustom.Text ?? "").Trim();
                if (string.IsNullOrEmpty(cardName))
                {
                    await DisplayAlert("Ошибка",
                        "Укажите название карты или способ получения.", "OK");
                    TxtCardCustom.Focus();
                    return;
                }
            }
            else
            {
                cardName = PkrCard.SelectedIndex >= 0
                    ? PkrCard.Items[PkrCard.SelectedIndex]
                    : "";
            }

            // Собирает доп. выплаты
            var extras = new List<ExtraPayment>();
            foreach (var (lblEntry, amtEntry) in _extraRows)
            {
                string lbl = (lblEntry.Text ?? "").Trim();
                string rawAmt = (amtEntry.Text ?? "")
                    .Trim().Replace(" ", "").Replace(",", ".");

                if (string.IsNullOrEmpty(lbl) && string.IsNullOrEmpty(rawAmt))
                    continue;

                if (string.IsNullOrEmpty(lbl))
                {
                    await DisplayAlert("Ошибка",
                        "Укажите название дополнительной выплаты.", "OK");
                    lblEntry.Focus();
                    return;
                }

                if (!decimal.TryParse(rawAmt, NumberStyles.Any,
                        CultureInfo.InvariantCulture, out decimal extraAmt)
                    || extraAmt <= 0)
                {
                    await DisplayAlert("Ошибка",
                        $"Укажите корректную сумму для «{lbl}».", "OK");
                    amtEntry.Focus();
                    return;
                }

                extras.Add(new ExtraPayment { Label = lbl, Amount = extraAmt });
            }

            // Сохраняет
            _storage.AddOrUpdate(
                DtpDate.Date, amount, _type,
                _forMonth, cardName, extras);
            _storage.Save();

            await Navigation.PopModalAsync();
        }

        // BtnDelete_Clicked удаляет запись, так как пользователь подтвердил действие
        private async void BtnDelete_Clicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert(
                "Удаление", "Удалить эту запись?", "Удалить", "Отмена");
            if (!confirm) return;

            _storage.Remove(_forMonth, _type);
            _storage.Save();
            await Navigation.PopModalAsync();
        }

        // BtnBack_Clicked закрывает страницу, так как пользователь передумал
        private async void BtnBack_Clicked(object sender, EventArgs e)
            => await Navigation.PopModalAsync();
    }
}