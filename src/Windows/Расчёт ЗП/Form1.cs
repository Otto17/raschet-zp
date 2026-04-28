// Copyright (c) 2026 Otto
// Лицензия: MIT (см. LICENSE)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Расчёт_ЗП
{
    public partial class Form1 : Form
    {
        // SalaryStorage управляет хранением данных о выплатах
        private SalaryStorage _storage;

        // Культура для форматирования чисел
        private static readonly CultureInfo RuCulture = new("ru-RU");

        // Цвета
        private readonly Color ColorHeader = Color.FromArgb(30, 30, 60);
        private readonly Color ColorPaid = Color.FromArgb(220, 255, 220);
        private readonly Color ColorUnpaid = Color.FromArgb(255, 245, 200);
        private readonly Color ColorAccent = Color.FromArgb(50, 100, 200);
        private readonly Color ColorTotal = Color.FromArgb(230, 240, 255);
        private readonly Color ColorBg = Color.FromArgb(235, 237, 250);
        private readonly Color ColorExtraBg = Color.FromArgb(235, 252, 235);

        // Ширина карточки: 580 - 17 (полоса прокрутки) - 16 (отступы) = 547
        private const int CardWidth = 547;

        public Form1()
        {
            InitializeComponent();
        }

        // Form1_Load загружает формы
        private void Form1_Load(object sender, EventArgs e)
        {
            _storage = SalaryStorage.Load();

            if (_storage.StartMonth == DateTime.MinValue)
            {
                var start = AskMonth("Расчёт ЗП — первый месяц учёта",
                                     "Укажите первый месяц учёта:");
                if (start == null) { Application.Exit(); return; }
                _storage.StartMonth = start.Value;
                _storage.Save();
            }

            RefreshAll();
        }

        // Form1_KeyDown горячие клавиши главного окна
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add)
            {
                e.Handled = true;
                AddNewMonth();
            }
        }

        // AskMonth запрашивает у пользователя месяц и год
        private DateTime? AskMonth(string title, string prompt)
        {
            DateTime? result = null;

            using (var dlg = new Form())
            {
                dlg.Text = title;
                dlg.Size = new Size(310, 165);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.Font = new Font("Segoe UI", 10);
                dlg.BackColor = Color.FromArgb(245, 247, 255);

                dlg.Controls.Add(new Label
                {
                    Text = prompt,
                    Location = new Point(12, 12),
                    Size = new Size(270, 22),
                    TextAlign = ContentAlignment.MiddleLeft
                });

                var cmbMonth = new ComboBox
                {
                    Location = new Point(12, 42),
                    Size = new Size(150, 28),
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                cmbMonth.Items.AddRange(MonthNames);
                cmbMonth.SelectedIndex = DateTime.Now.Month - 1;
                dlg.Controls.Add(cmbMonth);

                var nudYear = new NumericUpDown
                {
                    Location = new Point(172, 42),
                    Size = new Size(100, 28),
                    Minimum = 2000,
                    Maximum = 2100,
                    Value = DateTime.Now.Year,
                    DecimalPlaces = 0
                };
                dlg.Controls.Add(nudYear);

                var btnOk = new Button
                {
                    Text = "Готово",
                    Location = new Point(12, 84),
                    Size = new Size(100, 32),
                    BackColor = ColorAccent,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    DialogResult = DialogResult.OK
                };
                btnOk.FlatAppearance.BorderSize = 0;
                dlg.Controls.Add(btnOk);
                dlg.AcceptButton = btnOk;

                var btnCancel = new Button
                {
                    Text = "Отмена",
                    Location = new Point(120, 84),
                    Size = new Size(80, 32),
                    FlatStyle = FlatStyle.Flat,
                    DialogResult = DialogResult.Cancel
                };
                dlg.Controls.Add(btnCancel);
                dlg.CancelButton = btnCancel;

                if (dlg.ShowDialog(this) == DialogResult.OK)
                    result = new DateTime((int)nudYear.Value, cmbMonth.SelectedIndex + 1, 1);
            }

            return result;
        }

        // AddNewMonth добавляет новый месяц
        private void AddNewMonth()
        {
            var month = AskMonth("Расчёт ЗП — добавить месяц",
                                 "Выберите месяц для добавления:");
            if (month == null) return;

            _storage.AddExtraMonth(month.Value);
            _storage.Save();
            RefreshAll();
        }

        // DeleteMonth удаляет месяц
        private void DeleteMonth(DateTime month)
        {
            string monthStr = $"{MonthName(month)} {month.Year}";
            bool hasEntries = _storage.Entries.Any(e =>
                e.ForMonth.Year == month.Year && e.ForMonth.Month == month.Month);

            string warn = hasEntries
                ? $"В месяце «{monthStr}» есть внесённые выплаты.\nОни тоже будут удалены.\n\nУдалить месяц?"
                : $"Удалить месяц «{monthStr}»?";

            if (MessageBox.Show(warn, "Расчёт ЗП — удаление месяца",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            // Удаляет все записи этого месяца
            _storage.Entries.RemoveAll(e =>
                e.ForMonth.Year == month.Year && e.ForMonth.Month == month.Month);

            // Удаляет из списка вручную добавленных
            _storage.ExtraMonths.RemoveAll(m =>
                m.Year == month.Year && m.Month == month.Month);

            // Если это стартовый месяц — сдвигает на следующий имеющийся
            if (_storage.StartMonth.Year == month.Year &&
                _storage.StartMonth.Month == month.Month)
            {
                // Собирает оставшиеся месяцы
                var remaining = new HashSet<DateTime>();
                foreach (var m in _storage.ExtraMonths)
                    remaining.Add(new DateTime(m.Year, m.Month, 1));
                foreach (var entry in _storage.Entries)
                    remaining.Add(new DateTime(entry.ForMonth.Year, entry.ForMonth.Month, 1));

                if (remaining.Count > 0)
                    _storage.StartMonth = remaining.OrderBy(m => m).First();
                else
                    _storage.StartMonth = DateTime.MinValue;
            }

            _storage.Save();
            RefreshAll();
        }

        // RefreshAll обновляет интерфейс
        private void RefreshAll()
        {
            BuildSummaryPanel();
        }

        // BuildSummaryPanel создаёт построение сводной панели
        private void BuildSummaryPanel()
        {
            panelSummary.Controls.Clear();
            panelSummary.AutoScroll = false;
            panelSummary.AutoScroll = true;

            var monthSet = new HashSet<DateTime>();

            if (_storage.StartMonth != DateTime.MinValue)
                monthSet.Add(new DateTime(_storage.StartMonth.Year, _storage.StartMonth.Month, 1));

            foreach (var m in _storage.ExtraMonths)
                monthSet.Add(new DateTime(m.Year, m.Month, 1));

            foreach (var entry in _storage.Entries)
                monthSet.Add(new DateTime(entry.ForMonth.Year, entry.ForMonth.Month, 1));

            var months = monthSet.OrderByDescending(m => m).ToList();

            // Кнопка добавления месяца
            var btnAddMonth = new Button
            {
                Text = "＋ Добавить месяц",
                Location = new Point(8, 8),
                Size = new Size(CardWidth, 30),
                BackColor = ColorAccent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnAddMonth.FlatAppearance.BorderSize = 0;
            btnAddMonth.Click += (s, e) => AddNewMonth();
            panelSummary.Controls.Add(btnAddMonth);

            int yOffset = 46;
            foreach (var month in months)
            {
                var card = BuildMonthCard(month, CardWidth);
                card.Location = new Point(8, yOffset);
                panelSummary.Controls.Add(card);
                yOffset += card.Height + 8;
            }
        }

        // BuildMonthCard формирует визуальную карточку месяца
        private Panel BuildMonthCard(DateTime month, int width)
        {
            var e1 = _storage.Find(month, PaymentType.FirstHalf);
            var e2 = _storage.Find(month, PaymentType.SecondHalf);
            var e3 = _storage.Find(month, PaymentType.Cash);

            DateTime exp1 = GetExpectedDate(month, PaymentType.FirstHalf);
            DateTime exp2 = GetExpectedDate(month, PaymentType.SecondHalf);
            DateTime exp3 = GetExpectedDate(month, PaymentType.Cash);

            decimal total = 0;
            if (e1 != null) total += e1.Amount + e1.Extras.Sum(x => x.Amount);
            if (e2 != null) total += e2.Amount + e2.Extras.Sum(x => x.Amount);
            if (e3 != null) total += e3.Amount + e3.Extras.Sum(x => x.Amount);

            bool allPaid = (e1 != null && e2 != null && e3 != null);

            int extraCount = 0;
            if (e1?.Extras != null) extraCount += e1.Extras.Count;
            if (e2?.Extras != null) extraCount += e2.Extras.Count;
            if (e3?.Extras != null) extraCount += e3.Extras.Count;

            const int HeaderH = 28;
            const int RowH = 34;
            const int GapH = 4;
            const int ExtraH = 20;
            const int TotalH = 26;

            int contentH = (RowH + GapH) * 3 + extraCount * (ExtraH + 2) + GapH;
            int cardHeight = HeaderH + contentH + TotalH;

            var card = new Panel
            {
                Width = width,
                Height = cardHeight,
                BackColor = ColorBg,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Заголовок с кнопкой удаления
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = HeaderH,
                BackColor = ColorHeader
            };
            card.Controls.Add(headerPanel);

            var lblHeader = new Label
            {
                Text = $"За {MonthName(month)} {month.Year}",
                Location = new Point(8, 0),
                Size = new Size(width - 128, HeaderH),
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            headerPanel.Controls.Add(lblHeader);

            // Маленькая кнопка удаления месяца в заголовке
            var btnDelMonth = new Button
            {
                Text = "✕",
                Location = new Point(width - HeaderH - 20, 2),
                Size = new Size(HeaderH + 14, HeaderH - 5),
                BackColor = Color.FromArgb(180, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDelMonth.FlatAppearance.BorderSize = 0;
            var capturedMonth = month;
            btnDelMonth.Click += (s, e) => DeleteMonth(capturedMonth);
            headerPanel.Controls.Add(btnDelMonth);

            // Строки выплат
            int rowY = HeaderH + GapH;

            rowY = AddPaymentRow(card, rowY, width - 2, exp1, e1,
                $"1-я половина {MonthGenitiv(month)}",
                month, PaymentType.FirstHalf, GapH, ExtraH);

            rowY = AddPaymentRow(card, rowY, width - 2, exp2, e2,
                $"2-я половина {MonthGenitiv(month)}",
                month, PaymentType.SecondHalf, GapH, ExtraH);

            rowY = AddPaymentRow(card, rowY, width - 2, exp3, e3,
                "Наличными",
                month, PaymentType.Cash, GapH, ExtraH);

            // Итого
            string totalStr = allPaid
                ? $"Итого: {FormatMoney(total)} ₽"
                : total > 0
                    ? $"Получено: {FormatMoney(total)} ₽ (ожидается ещё)"
                    : "Ожидается...";

            var lblTotal = new Label
            {
                Text = totalStr,
                Dock = DockStyle.Bottom,
                Height = TotalH,
                BackColor = allPaid ? ColorPaid : ColorTotal,
                ForeColor = ColorHeader,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 8, 0)
            };
            card.Controls.Add(lblTotal);

            return card;
        }

        // AddPaymentRow строка выплаты в карточке
        private int AddPaymentRow(
            Panel card,
            int startY,
            int rowWidth,
            DateTime expectedDate,
            SalaryEntry entry,
            string baseDescription,
            DateTime forMonth,
            PaymentType type,
            int gapH,
            int extraH)
        {
            bool isPaid = (entry != null);
            Color rowColor = isPaid ? ColorPaid : ColorUnpaid;
            const int RowH = 34;

            string cardPart = isPaid
                ? (string.IsNullOrWhiteSpace(entry.CardName) ? "?" : entry.CardName)
                : DefaultCard(type);
            string fullDesc = $"{baseDescription}  →  {cardPart}";

            string dateText = isPaid
                ? entry.Date.ToString("dd.MM.yyyy")
                : $"~{expectedDate:dd.MM.yyyy}";

            // Панель строки
            var rowPanel = new Panel
            {
                Location = new Point(0, startY),
                Size = new Size(rowWidth, RowH),
                BackColor = rowColor
            };
            card.Controls.Add(rowPanel);

            // Дата
            rowPanel.Controls.Add(new Label
            {
                Text = dateText,
                Location = new Point(4, 0),
                Size = new Size(92, RowH),
                BackColor = Color.Transparent,
                ForeColor = isPaid ? Color.FromArgb(0, 100, 0) : Color.FromArgb(130, 85, 0),
                Font = new Font("Segoe UI", 8, isPaid ? FontStyle.Regular : FontStyle.Italic),
                TextAlign = ContentAlignment.MiddleCenter
            });

            // Разделитель дата/описание
            rowPanel.Controls.Add(new Panel
            {
                Location = new Point(97, 4),
                Size = new Size(1, RowH - 8),
                BackColor = Color.FromArgb(180, 180, 180)
            });

            // Описание
            rowPanel.Controls.Add(new Label
            {
                Text = fullDesc,
                Location = new Point(102, 0),
                Size = new Size(rowWidth - 102 - 106 - 50, RowH),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(35, 35, 75),
                Font = new Font("Segoe UI", 8),
                TextAlign = ContentAlignment.MiddleLeft
            });

            // Сумма
            decimal rowTotal = 0;
            if (isPaid)
            {
                rowTotal = entry.Amount;
                if (entry.Extras != null) rowTotal += entry.Extras.Sum(x => x.Amount);
            }

            rowPanel.Controls.Add(new Label
            {
                Text = isPaid ? $"{FormatMoney(rowTotal)} ₽" : "?",
                Location = new Point(rowWidth - 106 - 48, 0),
                Size = new Size(102, RowH),
                BackColor = Color.Transparent,
                ForeColor = isPaid ? Color.FromArgb(0, 120, 0) : Color.Gray,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            });

            // Кнопка внести/изменить
            var btn = new Button
            {
                Text = isPaid ? "✎" : "＋",
                Location = new Point(rowWidth - 46, 4),
                Size = new Size(42, RowH - 8),
                BackColor = isPaid ? Color.FromArgb(160, 210, 160) : ColorAccent,
                ForeColor = isPaid ? Color.FromArgb(0, 80, 0) : Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;

            var cm = forMonth; var ct = type; var ce = entry; var cd = expectedDate;
            btn.Click += (s, e) => ShowEntryDialog(cm, ct, ce, cd);
            rowPanel.Controls.Add(btn);

            int nextY = startY + RowH;

            // Строки доп. выплат
            if (isPaid && entry.Extras != null && entry.Extras.Count > 0)
            {
                foreach (var ex in entry.Extras)
                {
                    nextY += 2;
                    var extraPanel = new Panel
                    {
                        Location = new Point(0, nextY),
                        Size = new Size(rowWidth, extraH),
                        BackColor = ColorExtraBg
                    };
                    extraPanel.Controls.Add(new Label
                    {
                        Text = $" + {ex.Label}: {FormatMoney(ex.Amount)} ₽",
                        Location = new Point(0, 0),
                        Size = new Size(rowWidth - 4, extraH),
                        BackColor = Color.Transparent,
                        ForeColor = Color.FromArgb(0, 110, 0),
                        Font = new Font("Segoe UI", 7.5f, FontStyle.Italic),
                        TextAlign = ContentAlignment.MiddleLeft
                    });
                    card.Controls.Add(extraPanel);
                    nextY += extraH;
                }
            }

            nextY += gapH;
            return nextY;
        }

        // ShowEntryDialog создаёт диалог ввода/редактирования
        private void ShowEntryDialog(
            DateTime forMonth,
            PaymentType type,
            SalaryEntry existing,
            DateTime suggestedDate)
        {
            var dlg = new Form
            {
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(245, 247, 255),
                AutoScroll = false,
                KeyPreview = true,
                Text = "Расчёт ЗП — " + (existing == null ? "внести выплату" : "изменить выплату")
            };

            const int DlgW = 400;
            const int LblW = 130;
            const int CtrlX = 148;
            const int CtrlW = 222;
            int y = 10;

            // Заголовок
            string typeLabel = type switch
            {
                PaymentType.FirstHalf => $"1-я половина {MonthGenitiv(forMonth)}",
                PaymentType.SecondHalf => $"2-я половина {MonthGenitiv(forMonth)}",
                _ => $"Наличными за {MonthGenitiv(forMonth)}",
            };
            dlg.Controls.Add(new Label
            {
                Text = $"За {MonthName(forMonth)} {forMonth.Year}  —  {typeLabel}",
                Location = new Point(12, y),
                Size = new Size(DlgW - 24, 24),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = ColorHeader
            });
            y += 32;

            // Дата
            AddDlgLabel(dlg, "Дата получения:", 12, y, LblW);
            var dtpDate = new DateTimePicker
            {
                Location = new Point(CtrlX, y),
                Size = new Size(CtrlW, 24),
                Format = DateTimePickerFormat.Short,
                CustomFormat = "dd.MM.yyyy",
                Value = existing?.Date ?? suggestedDate
            };
            dlg.Controls.Add(dtpDate);
            y += 32;

            // Сумма
            AddDlgLabel(dlg, "Сумма (₽):", 12, y, LblW);
            var txtAmount = new TextBox
            {
                Location = new Point(CtrlX, y),
                Size = new Size(CtrlW, 24),
                Text = existing != null
                    ? existing.Amount.ToString("F2", RuCulture)
                    : string.Empty
            };
            dlg.Controls.Add(txtAmount);
            y += 32;

            // Карта
            AddDlgLabel(dlg, "Получено на:", 12, y, LblW);
            var cmbCard = new ComboBox
            {
                Location = new Point(CtrlX, y),
                Size = new Size(CtrlW, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            string[] cardOptions = ["Альфа-Банк", "ВТБ", "Райффайзен", "Наличные", "Другое..."];
            cmbCard.Items.AddRange(cardOptions);

            if (existing != null && !string.IsNullOrWhiteSpace(existing.CardName))
            {
                int idx = cmbCard.Items.IndexOf(existing.CardName);
                if (idx >= 0)
                    cmbCard.SelectedIndex = idx;
                else
                {
                    cmbCard.Items.Insert(cmbCard.Items.Count - 1, existing.CardName);
                    cmbCard.SelectedItem = existing.CardName;
                }
            }
            else
            {
                cmbCard.SelectedIndex = type switch
                {
                    PaymentType.FirstHalf => 0,
                    PaymentType.SecondHalf => 1,
                    _ => 2,
                };
            }
            dlg.Controls.Add(cmbCard);
            y += 32;

            // Поле «Другое...»
            var lblCustom = new Label
            {
                Text = "Название:",
                Location = new Point(12, y + 3),
                Size = new Size(LblW, 22),
                TextAlign = ContentAlignment.MiddleLeft,
                Visible = false
            };
            dlg.Controls.Add(lblCustom);

            var txtCardCustom = new TextBox
            {
                Location = new Point(CtrlX, y),
                Size = new Size(CtrlW, 24),
                Visible = false,
                Text = (existing != null
                            && !string.IsNullOrEmpty(existing.CardName)
                            && !((IList<string>)cardOptions).Contains(existing.CardName))
                    ? existing.CardName
                    : string.Empty
            };
            SetPlaceholder(txtCardCustom, "Название карты");
            dlg.Controls.Add(txtCardCustom);

            bool initCustom = cmbCard.SelectedItem?.ToString() == "Другое...";
            lblCustom.Visible = initCustom;
            txtCardCustom.Visible = initCustom;
            if (initCustom) y += 32;

            // Блок доп. выплат
            int extrasTopY = y;

            var lblExtrasHeader = new Label
            {
                Text = "Дополнительные выплаты:",
                Location = new Point(12, extrasTopY),
                Size = new Size(DlgW - 24, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = ColorHeader
            };
            dlg.Controls.Add(lblExtrasHeader);

            var btnAddExtra = new Button
            {
                Text = "+ Добавить",
                Location = new Point(12, extrasTopY + 22),
                Size = new Size(110, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(210, 230, 255),
                ForeColor = ColorHeader,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnAddExtra.FlatAppearance.BorderSize = 0;
            dlg.Controls.Add(btnAddExtra);

            // Кнопки внизу
            var btnSave = new Button
            {
                Text = "💾 Сохранить",
                Size = new Size(130, 34),
                BackColor = ColorAccent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            dlg.Controls.Add(btnSave);

            Button btnDelete = null;
            if (existing != null)
            {
                btnDelete = new Button
                {
                    Text = "🗑 Удалить",
                    Size = new Size(110, 34),
                    BackColor = Color.FromArgb(220, 80, 80),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                };
                btnDelete.FlatAppearance.BorderSize = 0;
                dlg.Controls.Add(btnDelete);
            }

            var btnCancel = new Button
            {
                Text = "Отмена",
                Size = new Size(80, 34),
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            dlg.Controls.Add(btnCancel);
            dlg.CancelButton = btnCancel;

            var extraRows = new List<(TextBox lblBox, TextBox amtBox, Button delBtn)>();

            // rebuildLayout пересчитывает layout диалога
            void rebuildLayout()
            {
                int baseY = 10 + 32 + 32 + 32;
                if (txtCardCustom.Visible) baseY += 32;

                extrasTopY = baseY;
                lblExtrasHeader.Location = new Point(12, extrasTopY);
                int ey = extrasTopY + 22;

                for (int i = 0; i < extraRows.Count; i++)
                {
                    extraRows[i].lblBox.Location = new Point(12, ey);
                    extraRows[i].amtBox.Location = new Point(230, ey);
                    extraRows[i].delBtn.Location = new Point(346, ey);
                    ey += 30;
                }

                btnAddExtra.Location = new Point(12, ey);
                ey += btnAddExtra.Height + 10;

                btnSave.Location = new Point(12, ey);
                if (btnDelete != null)
                    btnDelete.Location = new Point(150, ey);
                btnCancel.Location = new Point(btnDelete != null ? 268 : 150, ey);

                ey += btnSave.Height + 12;
                dlg.ClientSize = new Size(DlgW, ey);
            }

            // Переключение «Другое...»
            cmbCard.SelectedIndexChanged += (s, ev) =>
            {
                bool isOther = cmbCard.SelectedItem?.ToString() == "Другое...";
                lblCustom.Visible = isOther;
                txtCardCustom.Visible = isOther;
                rebuildLayout();
            };

            // Добавление строки доп. выплаты
            TextBox lastLblBox = null;

            // AddExtraRow создаёт элементы управления для дополнительной выплаты
            void addExtraRow(string labelText, decimal amount)
            {
                var tbLabel = new TextBox { Size = new Size(212, 24), Text = labelText };
                if (string.IsNullOrEmpty(labelText))
                    SetPlaceholder(tbLabel, "Больничный / Командировка");

                var tbAmt = new TextBox
                {
                    Size = new Size(110, 24),
                    Text = amount > 0 ? amount.ToString("F2", RuCulture) : string.Empty
                };
                if (amount <= 0)
                    SetPlaceholder(tbAmt, "Сумма ₽");

                var btnDel = new Button
                {
                    Text = "✕",
                    Size = new Size(26, 24),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(220, 80, 80),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 8, FontStyle.Bold)
                };
                btnDel.FlatAppearance.BorderSize = 0;

                var row = (tbLabel, tbAmt, btnDel);
                extraRows.Add(row);
                dlg.Controls.Add(tbLabel);
                dlg.Controls.Add(tbAmt);
                dlg.Controls.Add(btnDel);

                var capturedRow = row;
                btnDel.Click += (s, ev) =>
                {
                    extraRows.Remove(capturedRow);
                    dlg.Controls.Remove(capturedRow.tbLabel);
                    dlg.Controls.Remove(capturedRow.tbAmt);
                    dlg.Controls.Remove(capturedRow.btnDel);
                    rebuildLayout();
                };

                lastLblBox = tbLabel;
                rebuildLayout();
            }

            btnAddExtra.Click += (s, ev) =>
            {
                addExtraRow("", 0);
            };

            if (existing?.Extras != null)
                foreach (var ex in existing.Extras)
                    addExtraRow(ex.Label, ex.Amount);

            rebuildLayout();

            // Горячие клавиши диалога
            dlg.KeyDown += (s, ev) =>
            {
                if (ev.KeyCode == Keys.Enter && dlg.ActiveControl is not Button)
                {
                    ev.Handled = true;
                    btnSave.PerformClick();
                }
                else if (ev.KeyCode == Keys.Delete
                      && dlg.ActiveControl is not TextBox
                      && dlg.ActiveControl is not DateTimePicker)
                {
                    if (btnDelete != null) { ev.Handled = true; btnDelete.PerformClick(); }
                }
                else if ((ev.KeyCode == Keys.Oemplus || ev.KeyCode == Keys.Add)
                      && dlg.ActiveControl is not TextBox)
                {
                    ev.Handled = true;
                    btnAddExtra.PerformClick();
                }
            };

            // Сохранить
            btnSave.Click += (s, ev) =>
            {
                string raw = txtAmount.Text.Trim().Replace(" ", "").Replace(",", ".");
                if (!decimal.TryParse(raw, NumberStyles.Any,
                        CultureInfo.InvariantCulture, out decimal amount) || amount <= 0)
                {
                    MessageBox.Show("Введите корректную сумму больше нуля.",
                        "Расчёт ЗП — ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtAmount.Focus();
                    return;
                }

                string cardName;
                if (cmbCard.SelectedItem?.ToString() == "Другое...")
                {
                    cardName = txtCardCustom.ForeColor == Color.Gray
                        ? string.Empty : txtCardCustom.Text.Trim();
                    if (string.IsNullOrEmpty(cardName))
                    {
                        MessageBox.Show("Укажите название карты или способ получения.",
                            "Расчёт ЗП — ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtCardCustom.Focus();
                        return;
                    }
                }
                else
                    cardName = cmbCard.SelectedItem?.ToString() ?? "";

                var extras = new List<ExtraPayment>();
                foreach (var (lblBox, amtBox, delBtn) in extraRows)
                {
                    string lbl = lblBox.ForeColor == Color.Gray
                        ? string.Empty : lblBox.Text.Trim();
                    string rawAmt = amtBox.ForeColor == Color.Gray
                        ? string.Empty
                        : amtBox.Text.Trim().Replace(" ", "").Replace(",", ".");

                    if (string.IsNullOrEmpty(lbl) && string.IsNullOrEmpty(rawAmt)) continue;

                    if (string.IsNullOrEmpty(lbl))
                    {
                        MessageBox.Show("Укажите название дополнительной выплаты.",
                            "Расчёт ЗП — ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        lblBox.Focus();
                        return;
                    }
                    if (!decimal.TryParse(rawAmt, NumberStyles.Any,
                            CultureInfo.InvariantCulture, out decimal extraAmt) || extraAmt <= 0)
                    {
                        MessageBox.Show($"Укажите корректную сумму для «{lbl}».",
                            "Расчёт ЗП — ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        amtBox.Focus();
                        return;
                    }
                    extras.Add(new ExtraPayment { Label = lbl, Amount = extraAmt });
                }

                _storage.AddOrUpdate(dtpDate.Value.Date, amount, type, forMonth, cardName, extras);
                _storage.Save();
                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            };

            // Удалить запись
            if (btnDelete != null)
            {
                btnDelete.Click += (s, ev) =>
                {
                    if (MessageBox.Show("Удалить эту запись?", "Расчёт ЗП",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        _storage.Remove(forMonth, type);
                        _storage.Save();
                        dlg.DialogResult = DialogResult.Abort;
                        dlg.Close();
                    }
                };
            }

            dlg.ShowDialog(this);
            dlg.Dispose();
            RefreshAll();
        }

        // AddDlgLabel добавляет Label в диалог
        private void AddDlgLabel(Form dlg, string text, int x, int y, int w)
        {
            dlg.Controls.Add(new Label
            {
                Text = text,
                Location = new Point(x, y + 3),
                Size = new Size(w, 22),
                TextAlign = ContentAlignment.MiddleLeft
            });
        }

        // DefaultCard карта по умолчанию для подсказки
        private string DefaultCard(PaymentType _) => "?";

        // SetPlaceholder эмулирует PlaceholderText для .NET Framework 4.8
        private static void SetPlaceholder(TextBox tb, string placeholder)
        {
            Color normalColor = SystemColors.WindowText;
            Color hintColor = Color.Gray;

            if (string.IsNullOrEmpty(tb.Text))
            {
                tb.Text = placeholder;
                tb.ForeColor = hintColor;
            }

            tb.Enter += (s, e) =>
            {
                if (tb.ForeColor == hintColor && tb.Text == placeholder)
                {
                    tb.Text = string.Empty;
                    tb.ForeColor = normalColor;
                }
            };

            tb.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(tb.Text))
                {
                    tb.Text = placeholder;
                    tb.ForeColor = hintColor;
                }
            };
        }

        // GetExpectedDate вычисляет дату ожидания выплаты
        private DateTime GetExpectedDate(DateTime forMonth, PaymentType type)
        {
            switch (type)
            {
                case PaymentType.FirstHalf:
                    return SafeDate(forMonth.Year, forMonth.Month, 27);
                case PaymentType.SecondHalf:
                    {
                        var next = forMonth.AddMonths(1);
                        return SafeDate(next.Year, next.Month, 14);
                    }
                case PaymentType.Cash:
                    {
                        var after = forMonth.AddMonths(1);
                        return SafeDate(after.Year, after.Month, 29);
                    }
                default: return forMonth;
            }
        }

        // SafeDate безопасно создаёт даты
        private DateTime SafeDate(int year, int month, int day)
        {
            int maxDay = DateTime.DaysInMonth(year, month);
            return new DateTime(year, month, Math.Min(day, maxDay));
        }

        private static readonly string[] MonthNames =
        [
            "Январь","Февраль","Март","Апрель","Май","Июнь",
            "Июль","Август","Сентябрь","Октябрь","Ноябрь","Декабрь"
        ];

        private static readonly string[] MonthNamesGenitiv =
        [
            "Января","Февраля","Марта","Апреля","Мая","Июня",
            "Июля","Августа","Сентября","Октября","Ноября","Декабря"
        ];

        // MonthName возвращает название месяца
        private string MonthName(DateTime d) => MonthNames[d.Month - 1];

        // MonthGenitiv возвращает название месяца в родительном падеже
        private string MonthGenitiv(DateTime d) => MonthNamesGenitiv[d.Month - 1];

        // FormatMoney возвращает сумму в отформатированном виде
        private string FormatMoney(decimal value)
        {
            return value == Math.Floor(value)
            ? value.ToString("N0", RuCulture)
            : value.ToString("N2", RuCulture);
        }
    }
}