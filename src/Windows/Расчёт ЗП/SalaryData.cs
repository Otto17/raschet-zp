// Copyright (c) 2026 Otto
// Лицензия: MIT (см. LICENSE)

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Расчёт_ЗП
{
    // SalaryEntry хранит информацию об одной выплате
    public class SalaryEntry
    {
        // Дата выплаты
        public DateTime Date { get; set; }

        // Сумма в рублях
        public decimal Amount { get; set; }

        // Тип выплаты
        public PaymentType Type { get; set; }

        // Месяц, ЗА который выплата
        public DateTime ForMonth { get; set; }

        // Карта или наличные
        public string CardName { get; set; } = "";

        // Дополнительные выплаты (больничный, командировка и т.д.)
        public List<ExtraPayment> Extras { get; set; } = [];
    }

    // ExtraPayment содержит данные о дополнительной выплате
    public class ExtraPayment
    {
        // Название доплаты
        public string Label { get; set; } = "";

        // Сумма доплаты
        public decimal Amount { get; set; }
    }

    // PaymentType определяет тип выплаты зарплаты
    public enum PaymentType
    {
        FirstHalf = 0,  // Первая половина месяца
        SecondHalf = 1, // Вторая половина (конец) месяца
        Cash = 2        // Наличными
    }

    // SalaryStorage управляет хранением данных о выплатах
    public class SalaryStorage
    {
        public static readonly string FilePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "salary_data.json");

        public List<SalaryEntry> Entries { get; set; } = [];

        // Первый месяц отображения
        public DateTime StartMonth { get; set; } = DateTime.MinValue;

        // Список месяцев, добавленных вручную пользователем (даже без записей)
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
                var storage = JsonConvert.DeserializeObject<SalaryStorage>(json);
                return storage ?? new SalaryStorage();
            }
            catch
            {
                return new SalaryStorage();
            }
        }

        // Save записывает данные в JSON-файл
        public void Save()
        {
            string json = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(FilePath, json, System.Text.Encoding.UTF8);
        }

        // Find возвращает запись для указанного месяца и типа выплаты
        public SalaryEntry Find(DateTime forMonth, PaymentType type)
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
            if (entry != null)
            {
                Entries.Remove(entry);
                return true;
            }
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