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

            // Apply compound increases
            for (int i = 0; i < increaseCount; i++)
            {
                currentRent += currentRent * (decimal)(agreement.IncreasePercentage / 100.0);
            }

            return Math.Round(currentRent, 2);
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

            for (var current = start; current <= endMonth; current = current.AddMonths(1))
            {
                string month = current.ToString("yyyy-MM");
                var effectiveRent = CalculateEffectiveRent(agreement, month);
                var expense = agreement.Expenses?.FirstOrDefault(e => e.Month == month);
                var totalExpenses = expense != null ? expense.Electricity + expense.Maintenance + expense.Other : 0;
                var totalRent = effectiveRent + totalExpenses;
                var paid = agreement.Payments?
                    .Where(p => p.Month == month)
                    .Sum(p => p.PaidAmount) ?? 0;

                summaries.Add(new MonthlyRentSummary
                {
                    Month = month,
                    TenantName = agreement.Tenant?.Name ?? "N/A",
                    UnitNumber = agreement.Unit?.UnitNumber ?? "N/A",
                    PropertyName = agreement.Unit?.Property?.Name ?? "N/A",
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
