// Copyright (c) 2026 Otto
// Лицензия: MIT (см. LICENSE)

using Newtonsoft.Json;

namespace SalaryApp
{
    // SalaryEntry хранит информацию об одной выплате
    public class SalaryEntry
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public PaymentType Type { get; set; }
        public DateTime ForMonth { get; set; }
        public string CardName { get; set; } = "";
        public List<ExtraPayment> Extras { get; set; } = [];
    }

    // ExtraPayment содержит данные о дополнительной выплате
    public class ExtraPayment
    {
        public string Label { get; set; } = "";
        public decimal Amount { get; set; }
    }

    // PaymentType определяет тип выплаты зарплаты
    public enum PaymentType
    {
        FirstHalf = 0,
        SecondHalf = 1,
        Cash = 2
    }

    // SalaryStorage управляет хранением данных о выплатах
    public class SalaryStorage
    {
        // FilePath указывает путь к файлу данных в памяти устройства
        public static readonly string FilePath =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "salary_data.json");

        public List<SalaryEntry> Entries { get; set; } = [];
        public DateTime StartMonth { get; set; } = DateTime.MinValue;
        public List<DateTime> ExtraMonths { get; set; } = [];

        // Load считывает данные из JSON-файла
        public static SalaryStorage Load()
        {
            if (!File.Exists(FilePath))
            {
                var fresh = new SalaryStorage();
                fresh.Save();
                return fresh;
            }
            try
            {
                string json = File.ReadAllText(FilePath, System.Text.Encoding.UTF8);
                return JsonConvert.DeserializeObject<SalaryStorage>(json)
                       ?? new SalaryStorage();
            }
            catch
            {
                return new SalaryStorage();
            }
        }

        // Save записывает данные в JSON-файл
        public void Save()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(FilePath, json, System.Text.Encoding.UTF8);
        }

        // Find возвращает запись для указанного месяца и типа выплаты
        public SalaryEntry? Find(DateTime forMonth, PaymentType type)
        {
            return Entries.Find(e =>
                e.ForMonth.Year == forMonth.Year &&
                e.ForMonth.Month == forMonth.Month &&
                e.Type == type);
        }

        // AddOrUpdate сохраняет или обновляет запись в списке
        public void AddOrUpdate(DateTime date, decimal amount, PaymentType type,
            DateTime forMonth, string cardName, List<ExtraPayment> extras)
        {
            var existing = Find(forMonth, type);
            if (existing != null)
            {
                existing.Date = date;
                existing.Amount = amount;
                existing.CardName = cardName;
                existing.Extras = extras ?? [];
            }
            else
            {
                Entries.Add(new SalaryEntry
                {
                    Date = date,
                    Amount = amount,
                    Type = type,
                    ForMonth = new DateTime(forMonth.Year, forMonth.Month, 1),
                    CardName = cardName,
                    Extras = extras ?? []
                });
            }
        }

        // Remove удаляет запись, так как она больше не требуется
        public bool Remove(DateTime forMonth, PaymentType type)
        {
            var entry = Find(forMonth, type);
            if (entry != null) { Entries.Remove(entry); return true; }
            return false;
        }

        // AddExtraMonth добавляет месяц в список вручную, так как пользователь его запросил
        public void AddExtraMonth(DateTime month)
        {
            var m = new DateTime(month.Year, month.Month, 1);
            if (!ExtraMonths.Exists(x => x.Year == m.Year && x.Month == m.Month))
                ExtraMonths.Add(m);
        }
    }
}