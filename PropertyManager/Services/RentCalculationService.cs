using PropertyManager.Data;
using PropertyManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PropertyManager.Services
{
    /// <summary>
    /// Core business logic for rent calculation, dynamic rent increases, and dues.
    /// Rent increase rules are read from each Agreement — NOT from global settings.
    /// </summary>
    public static class RentCalculationService
    {
        /// <summary>
        /// Calculates the effective base rent for a given month, 
        /// applying periodic increases based on months elapsed from StartDate.
        /// 
        /// Formula: After every IncreaseAfterMonths period,
        ///   NewRent = CurrentRent + (CurrentRent * IncreasePercentage / 100)
        /// </summary>
        public static decimal CalculateEffectiveRent(Agreement agreement, string month)
        {
            var monthDate = ParseMonth(month);
            var startDate = new DateTime(agreement.StartDate.Year, agreement.StartDate.Month, 1);
            var currentRent = agreement.BaseRent;

            if (agreement.IncreaseAfterMonths <= 0 || agreement.IncreasePercentage <= 0)
                return currentRent;

            // How many full months have passed since agreement start?
            int monthsElapsed = ((monthDate.Year - startDate.Year) * 12) + (monthDate.Month - startDate.Month);

            if (monthsElapsed <= 0)
                return currentRent;

            // How many increase periods have completed?
            int increaseCount = monthsElapsed / agreement.IncreaseAfterMonths;

            // Apply compound increases (rounded per revision, matching GetMonthlySummaries)
            decimal factor = 1 + (decimal)(agreement.IncreasePercentage / 100.0);
            for (int i = 0; i < increaseCount; i++)
            {
                currentRent = Math.Round(currentRent * factor, 2);
            }

            return currentRent;
        }

        /// <summary>
        /// TotalRent = EffectiveBaseRent + Electricity + Maintenance + Other expenses
        /// </summary>
        public static decimal CalculateTotalRent(Agreement agreement, string month)
        {
            var effectiveRent = CalculateEffectiveRent(agreement, month);
            var expense = agreement.Expenses?.FirstOrDefault(e => e.Month == month);

            if (expense != null)
            {
                effectiveRent += expense.Electricity + expense.Maintenance + expense.Other;
            }

            return effectiveRent;
        }

        /// <summary>
        /// Calculates dues for a single month: TotalRent - sum of payments for that month.
        /// </summary>
        public static decimal CalculateMonthlyDue(Agreement agreement, string month)
        {
            var totalRent = CalculateTotalRent(agreement, month);
            var paid = agreement.Payments?
                .Where(p => p.Month == month)
                .Sum(p => p.PaidAmount) ?? 0;

            return totalRent - paid;
        }

        /// <summary>
        /// Returns all months from StartDate to now (or EndDate) with dues for each.
        /// </summary>
        public static List<MonthlyRentSummary> GetMonthlySummaries(Agreement agreement)
        {
            var summaries = new List<MonthlyRentSummary>();
            var start = new DateTime(agreement.StartDate.Year, agreement.StartDate.Month, 1);
            var end = DateTime.Now < agreement.EndDate ? DateTime.Now : agreement.EndDate;
            var endMonth = new DateTime(end.Year, end.Month, 1);

            // Pre-index expenses and payments by month so each lookup is O(1)
            // instead of scanning the whole collection for every month.
            var expensesByMonth = (agreement.Expenses ?? new List<Expense>())
                .GroupBy(e => e.Month)
                .ToDictionary(g => g.Key, g => g.First());
            var paidByMonth = (agreement.Payments ?? new List<Payment>())
                .GroupBy(p => p.Month)
                .ToDictionary(g => g.Key, g => g.Sum(p => p.PaidAmount));

            string tenantName = agreement.Tenant?.Name ?? "N/A";
            string unitNumber = agreement.Unit?.UnitNumber ?? "N/A";
            string propertyName = agreement.Unit?.Property?.Name ?? "N/A";

            bool applyIncreases = agreement.IncreaseAfterMonths > 0 && agreement.IncreasePercentage > 0;
            decimal increaseFactor = 1 + (decimal)(agreement.IncreasePercentage / 100.0);

            // Progressive effective rent: apply the compound increase only when a
            // new period boundary is crossed — O(total months) overall.
            decimal effectiveRent = agreement.BaseRent;
            int monthsElapsed = 0;
            int nextIncreaseAt = applyIncreases ? agreement.IncreaseAfterMonths : int.MaxValue;

            for (var current = start; current <= endMonth; current = current.AddMonths(1), monthsElapsed++)
            {
                while (applyIncreases && monthsElapsed >= nextIncreaseAt)
                {
                    effectiveRent = Math.Round(effectiveRent * increaseFactor, 2);
                    nextIncreaseAt += agreement.IncreaseAfterMonths;
                }

                string month = current.ToString("yyyy-MM");
                expensesByMonth.TryGetValue(month, out var expense);
                var totalExpenses = expense != null ? expense.Electricity + expense.Maintenance + expense.Other : 0;
                var totalRent = effectiveRent + totalExpenses;
                paidByMonth.TryGetValue(month, out var paid);

                summaries.Add(new MonthlyRentSummary
                {
                    Month = month,
                    TenantName = tenantName,
                    UnitNumber = unitNumber,
                    PropertyName = propertyName,
                    BaseRent = effectiveRent,
                    Electricity = expense?.Electricity ?? 0,
                    Maintenance = expense?.Maintenance ?? 0,
                    OtherExpenses = expense?.Other ?? 0,
                    TotalRent = totalRent,
                    PaidAmount = paid,
                    PendingAmount = totalRent - paid
                });
            }

            return summaries;
        }

        /// <summary>
        /// Total outstanding balance across all months for an agreement.
        /// </summary>
        public static decimal GetTotalOutstandingBalance(Agreement agreement)
        {
            return GetMonthlySummaries(agreement).Sum(s => Math.Max(0, s.PendingAmount));
        }

        private static DateTime ParseMonth(string month)
        {
            var parts = month.Split('-');
            return new DateTime(int.Parse(parts[0]), int.Parse(parts[1]), 1);
        }
    }

    /// <summary>
    /// DTO for monthly rent breakdown.
    /// </summary>
    public class MonthlyRentSummary
    {
        public string Month { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public string UnitNumber { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public decimal BaseRent { get; set; }
        public decimal Electricity { get; set; }
        public decimal Maintenance { get; set; }
        public decimal OtherExpenses { get; set; }
        public decimal TotalRent { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal PendingAmount { get; set; }
    }
}
