using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Data;
using PropertyManager.Models;
using PropertyManager.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Printing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace PropertyManager.ViewModels
{
    /// <summary>
    /// DTO for defaulter display.
    /// </summary>
    public class DefaulterInfo
    {
        public string TenantName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string UnitNumber { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public decimal TotalDue { get; set; }
        public int UnpaidMonths { get; set; }
    }

    public partial class ReportViewModel : ObservableObject
    {
        [ObservableProperty] private ObservableCollection<MonthlyRentSummary> _monthlySummary = new();
        [ObservableProperty] private ObservableCollection<MonthlyRentSummary> _filteredSummary = new();
        [ObservableProperty] private ObservableCollection<DefaulterInfo> _defaulters = new();
        [ObservableProperty] private ObservableCollection<Agreement> _agreements = new();
        [ObservableProperty] private ObservableCollection<Tenant> _allTenants = new();
        [ObservableProperty] private Agreement? _selectedAgreement;
        [ObservableProperty] private Tenant? _selectedTenant;

        // ── Filter Fields ──
        [ObservableProperty] private string _filterPeriod = "All Time";
        [ObservableProperty] private DateTime _filterFromDate = DateTime.Today.AddYears(-1);
        [ObservableProperty] private DateTime _filterToDate = DateTime.Today;
        [ObservableProperty] private bool _isCustomDateRange;

        // ── Summary Totals ──
        [ObservableProperty] private decimal _totalRentExpected;
        [ObservableProperty] private decimal _totalCollected;
        [ObservableProperty] private decimal _totalPending;

        // ── Filtered Totals ──
        [ObservableProperty] private decimal _filteredRentExpected;
        [ObservableProperty] private decimal _filteredCollected;
        [ObservableProperty] private decimal _filteredPending;

        public string[] FilterPeriods => new[] { "All Time", "This Month", "Last 3 Months", "Last 6 Months", "This Year", "Custom Range" };

        public ReportViewModel()
        {
            LoadDataCommand.Execute(null);
        }

        partial void OnSelectedAgreementChanged(Agreement? value) => ApplyFilters();
        partial void OnSelectedTenantChanged(Tenant? value) => ApplyFilters();
        partial void OnFilterPeriodChanged(string value)
        {
            IsCustomDateRange = value == "Custom Range";
            ApplyFilters();
        }
        partial void OnFilterFromDateChanged(DateTime value) { if (IsCustomDateRange) ApplyFilters(); }
        partial void OnFilterToDateChanged(DateTime value) { if (IsCustomDateRange) ApplyFilters(); }

        [RelayCommand]
        private async Task LoadData()
        {
            await Task.Run(() =>
            {
                using var db = new AppDbContext();

                var allAgreements = db.Agreements
                    .Include(a => a.Tenant)
                    .Include(a => a.Unit)
                        .ThenInclude(u => u!.Property)
                    .Include(a => a.Expenses)
                    .Include(a => a.Payments)
                    .ToList();

                var tenants = db.Tenants.OrderBy(t => t.Name).ToList();

                // Calculate defaulters and totals
                var defaulterList = new ObservableCollection<DefaulterInfo>();
                decimal totalExpected = 0, totalCollected = 0, totalPending = 0;
                var allSummaries = new ObservableCollection<MonthlyRentSummary>();

                foreach (var agreement in allAgreements)
                {
                    var summaries = RentCalculationService.GetMonthlySummaries(agreement);
                    var outstanding = summaries.Where(s => s.PendingAmount > 0).ToList();

                    totalExpected += summaries.Sum(s => s.TotalRent);
                    totalCollected += summaries.Sum(s => s.PaidAmount);
                    totalPending += outstanding.Sum(s => s.PendingAmount);

                    foreach (var s in summaries) allSummaries.Add(s);

                    if (outstanding.Any())
                    {
                        defaulterList.Add(new DefaulterInfo
                        {
                            TenantName = agreement.Tenant?.Name ?? "N/A",
                            Phone = agreement.Tenant?.Phone ?? "N/A",
                            UnitNumber = agreement.Unit?.UnitNumber ?? "N/A",
                            PropertyName = agreement.Unit?.Property?.Name ?? "N/A",
                            TotalDue = outstanding.Sum(s => s.PendingAmount),
                            UnpaidMonths = outstanding.Count
                        });
                    }
                }

                App.Current.Dispatcher.Invoke(() =>
                {
                    Agreements = new ObservableCollection<Agreement>(allAgreements);
                    AllTenants = new ObservableCollection<Tenant>(tenants);
                    MonthlySummary = allSummaries;
                    Defaulters = defaulterList;
                    TotalRentExpected = totalExpected;
                    TotalCollected = totalCollected;
                    TotalPending = totalPending;

                    ApplyFilters();
                });
            });
        }

        [RelayCommand]
        private void ApplyFilters()
        {
            var filtered = MonthlySummary.AsEnumerable();

            // Tenant filter
            if (SelectedTenant != null)
            {
                filtered = filtered.Where(s => s.TenantName == SelectedTenant.Name);
            }

            // Agreement filter
            if (SelectedAgreement != null)
            {
                filtered = filtered.Where(s =>
                    s.TenantName == (SelectedAgreement.Tenant?.Name ?? "") &&
                    s.UnitNumber == (SelectedAgreement.Unit?.UnitNumber ?? ""));
            }

            // Period filter
            DateTime fromDate = DateTime.MinValue;
            DateTime toDate = DateTime.MaxValue;

            switch (FilterPeriod)
            {
                case "This Month":
                    fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    toDate = fromDate.AddMonths(1).AddDays(-1);
                    break;
                case "Last 3 Months":
                    fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-2);
                    toDate = DateTime.Now;
                    break;
                case "Last 6 Months":
                    fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-5);
                    toDate = DateTime.Now;
                    break;
                case "This Year":
                    fromDate = new DateTime(DateTime.Now.Year, 1, 1);
                    toDate = new DateTime(DateTime.Now.Year, 12, 31);
                    break;
                case "Custom Range":
                    fromDate = FilterFromDate;
                    toDate = FilterToDate;
                    break;
            }

            if (FilterPeriod != "All Time")
            {
                filtered = filtered.Where(s =>
                {
                    var parts = s.Month.Split('-');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int y) && int.TryParse(parts[1], out int m))
                    {
                        var monthDate = new DateTime(y, m, 1);
                        return monthDate >= fromDate && monthDate <= toDate;
                    }
                    return true;
                });
            }

            var result = filtered.OrderByDescending(s => s.Month).ToList();
            FilteredSummary = new ObservableCollection<MonthlyRentSummary>(result);

            // Filtered totals
            FilteredRentExpected = result.Sum(s => s.TotalRent);
            FilteredCollected = result.Sum(s => s.PaidAmount);
            FilteredPending = result.Sum(s => Math.Max(0, s.PendingAmount));
        }

        [RelayCommand]
        private void ClearFilters()
        {
            SelectedTenant = null;
            SelectedAgreement = null;
            FilterPeriod = "All Time";
            FilterFromDate = DateTime.Today.AddYears(-1);
            FilterToDate = DateTime.Today;
            ApplyFilters();
        }

        [RelayCommand]
        private void PrintReport()
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() != true) return;

                // Build the FlowDocument for printing
                var doc = new FlowDocument
                {
                    PagePadding = new Thickness(40, 30, 40, 30),
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 11,
                    ColumnWidth = 999
                };

                // ── Header ──
                doc.Blocks.Add(new Paragraph(new Run("🏠 Property & Rent Management System"))
                {
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 4)
                });

                doc.Blocks.Add(new Paragraph(new Run("Financial Report"))
                {
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    TextAlignment = TextAlignment.Center,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(0, 0, 0, 4)
                });

                // Filter info
                string filterInfo = $"Period: {FilterPeriod}";
                if (SelectedTenant != null) filterInfo += $"  |  Tenant: {SelectedTenant.Name}";
                if (IsCustomDateRange) filterInfo += $"  |  From: {FilterFromDate:dd-MMM-yyyy} To: {FilterToDate:dd-MMM-yyyy}";
                filterInfo += $"  |  Generated: {DateTime.Now:dd-MMM-yyyy HH:mm}";

                doc.Blocks.Add(new Paragraph(new Run(filterInfo))
                {
                    FontSize = 9,
                    Foreground = Brushes.Gray,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 14)
                });

                // ── Summary Section ──
                doc.Blocks.Add(new Paragraph(new Run("── Financial Summary ──"))
                {
                    FontSize = 12, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 6)
                });

                var summaryTable = new Table { CellSpacing = 0 };
                summaryTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
                summaryTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
                summaryTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

                var summaryGroup = new TableRowGroup();
                var summaryHeaderRow = new TableRow { Background = Brushes.LightGray };
                summaryHeaderRow.Cells.Add(CreateCell("Total Rent Expected", true));
                summaryHeaderRow.Cells.Add(CreateCell("Total Collected", true));
                summaryHeaderRow.Cells.Add(CreateCell("Total Pending", true));
                summaryGroup.Rows.Add(summaryHeaderRow);

                var summaryRow = new TableRow();
                summaryRow.Cells.Add(CreateCell($"Rs. {FilteredRentExpected:N0}"));
                summaryRow.Cells.Add(CreateCell($"Rs. {FilteredCollected:N0}"));
                summaryRow.Cells.Add(CreateCell($"Rs. {FilteredPending:N0}"));
                summaryGroup.Rows.Add(summaryRow);
                summaryTable.RowGroups.Add(summaryGroup);
                doc.Blocks.Add(summaryTable);

                // ── Transaction Detail Table ──
                doc.Blocks.Add(new Paragraph(new Run("── Monthly Transaction Details ──"))
                {
                    FontSize = 12, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 14, 0, 6)
                });

                var table = new Table { CellSpacing = 0, FontSize = 9 };
                string[] headers = { "Month", "Tenant", "Property", "Unit", "Base Rent", "Electricity", "Maint.", "Other", "Total", "Paid", "Pending" };
                foreach (var _ in headers)
                    table.Columns.Add(new TableColumn());

                var headerGroup = new TableRowGroup();
                var headerRow = new TableRow { Background = Brushes.DarkSlateGray };
                foreach (var h in headers)
                {
                    var cell = CreateCell(h, true);
                    cell.Foreground = Brushes.White;
                    headerRow.Cells.Add(cell);
                }
                headerGroup.Rows.Add(headerRow);
                table.RowGroups.Add(headerGroup);

                var dataGroup = new TableRowGroup();
                int rowNum = 0;
                foreach (var item in FilteredSummary)
                {
                    var row = new TableRow();
                    if (rowNum % 2 == 1)
                        row.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));

                    row.Cells.Add(CreateCell(item.Month));
                    row.Cells.Add(CreateCell(item.TenantName));
                    row.Cells.Add(CreateCell(item.PropertyName));
                    row.Cells.Add(CreateCell(item.UnitNumber));
                    row.Cells.Add(CreateCell($"{item.BaseRent:N0}"));
                    row.Cells.Add(CreateCell($"{item.Electricity:N0}"));
                    row.Cells.Add(CreateCell($"{item.Maintenance:N0}"));
                    row.Cells.Add(CreateCell($"{item.OtherExpenses:N0}"));
                    row.Cells.Add(CreateCell($"{item.TotalRent:N0}"));
                    row.Cells.Add(CreateCell($"{item.PaidAmount:N0}"));

                    var pendingCell = CreateCell($"{item.PendingAmount:N0}");
                    if (item.PendingAmount > 0)
                    {
                        pendingCell.Foreground = Brushes.Red;
                        pendingCell.FontWeight = FontWeights.Bold;
                    }
                    row.Cells.Add(pendingCell);

                    dataGroup.Rows.Add(row);
                    rowNum++;
                }
                table.RowGroups.Add(dataGroup);

                // Totals row
                var totalsGroup = new TableRowGroup();
                var totalsRow = new TableRow { Background = Brushes.LightGray };
                totalsRow.Cells.Add(CreateCell("TOTAL", true));
                totalsRow.Cells.Add(CreateCell("")); // tenant
                totalsRow.Cells.Add(CreateCell("")); // property
                totalsRow.Cells.Add(CreateCell("")); // unit
                totalsRow.Cells.Add(CreateCell("")); // base rent
                totalsRow.Cells.Add(CreateCell("")); // electricity
                totalsRow.Cells.Add(CreateCell("")); // maint
                totalsRow.Cells.Add(CreateCell("")); // other
                totalsRow.Cells.Add(CreateCell($"{FilteredRentExpected:N0}", true));
                totalsRow.Cells.Add(CreateCell($"{FilteredCollected:N0}", true));
                var totalPendingCell = CreateCell($"{FilteredPending:N0}", true);
                totalPendingCell.Foreground = Brushes.Red;
                totalsRow.Cells.Add(totalPendingCell);
                totalsGroup.Rows.Add(totalsRow);
                table.RowGroups.Add(totalsGroup);

                doc.Blocks.Add(table);



                // ── Footer ──
                doc.Blocks.Add(new Paragraph(new Run("This report is system-generated by Property Manager."))
                {
                    FontSize = 8,
                    Foreground = Brushes.Gray,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 16, 0, 0)
                });

                // Print
                var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
                paginator.PageSize = new Size(printDialog.PrintableAreaWidth, printDialog.PrintableAreaHeight);
                printDialog.PrintDocument(paginator, "Property Manager — Report");

                MessageBox.Show("Report printed successfully!", "Print", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Print failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static TableCell CreateCell(string text, bool bold = false)
        {
            var run = new Run(text);
            if (bold) run.FontWeight = FontWeights.Bold;
            return new TableCell(new Paragraph(run) { Margin = new Thickness(4, 3, 4, 3) })
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0, 0, 0, 0.5)
            };
        }

        [RelayCommand]
        private void PrintDefaultersReport()
        {
            try
            {
                if (!Defaulters.Any())
                {
                    MessageBox.Show("No defaulters to print.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() != true) return;

                var doc = new FlowDocument
                {
                    PagePadding = new Thickness(40, 30, 40, 30),
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 11,
                    ColumnWidth = 999
                };

                // Header
                doc.Blocks.Add(new Paragraph(new Run("🏠 Property & Rent Management System"))
                {
                    FontSize = 18, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 4)
                });
                doc.Blocks.Add(new Paragraph(new Run("Defaulters Report"))
                {
                    FontSize = 14, FontWeight = FontWeights.SemiBold, TextAlignment = TextAlignment.Center,
                    Foreground = Brushes.DarkRed, Margin = new Thickness(0, 0, 0, 4)
                });
                doc.Blocks.Add(new Paragraph(new Run($"Generated: {DateTime.Now:dd-MMM-yyyy HH:mm}  |  Total Defaulters: {Defaulters.Count}"))
                {
                    FontSize = 9, Foreground = Brushes.Gray, TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 14)
                });

                // Summary
                doc.Blocks.Add(new Paragraph(new Run($"Total Outstanding Dues: Rs. {Defaulters.Sum(d => d.TotalDue):N0}"))
                {
                    FontSize = 14, FontWeight = FontWeights.Bold, Foreground = Brushes.DarkRed,
                    Margin = new Thickness(0, 0, 0, 12)
                });

                // Defaulters table
                var defTable = new Table { CellSpacing = 0, FontSize = 10 };
                string[] defHeaders = { "#", "Tenant Name", "Phone", "Property", "Unit", "Unpaid Months", "Total Due (Rs.)" };
                foreach (var _ in defHeaders)
                    defTable.Columns.Add(new TableColumn());

                var defHeaderGroup = new TableRowGroup();
                var defHeaderRow = new TableRow { Background = Brushes.DarkSlateGray };
                foreach (var h in defHeaders)
                {
                    var cell = CreateCell(h, true);
                    cell.Foreground = Brushes.White;
                    defHeaderRow.Cells.Add(cell);
                }
                defHeaderGroup.Rows.Add(defHeaderRow);
                defTable.RowGroups.Add(defHeaderGroup);

                var defDataGroup = new TableRowGroup();
                int num = 1;
                foreach (var d in Defaulters.OrderByDescending(d => d.TotalDue))
                {
                    var row = new TableRow();
                    if (num % 2 == 0)
                        row.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));

                    row.Cells.Add(CreateCell(num.ToString()));
                    row.Cells.Add(CreateCell(d.TenantName));
                    row.Cells.Add(CreateCell(d.Phone));
                    row.Cells.Add(CreateCell(d.PropertyName));
                    row.Cells.Add(CreateCell(d.UnitNumber));
                    row.Cells.Add(CreateCell(d.UnpaidMonths.ToString()));

                    var dueCell = CreateCell($"{d.TotalDue:N0}");
                    dueCell.Foreground = Brushes.Red;
                    dueCell.FontWeight = FontWeights.Bold;
                    row.Cells.Add(dueCell);
                    defDataGroup.Rows.Add(row);
                    num++;
                }
                defTable.RowGroups.Add(defDataGroup);

                // Totals row
                var totRow = new TableRow { Background = Brushes.LightGray };
                totRow.Cells.Add(CreateCell(""));
                totRow.Cells.Add(CreateCell("TOTAL", true));
                totRow.Cells.Add(CreateCell(""));
                totRow.Cells.Add(CreateCell(""));
                totRow.Cells.Add(CreateCell(""));
                totRow.Cells.Add(CreateCell(Defaulters.Sum(d => d.UnpaidMonths).ToString(), true));
                var totCell = CreateCell($"{Defaulters.Sum(d => d.TotalDue):N0}", true);
                totCell.Foreground = Brushes.Red;
                totRow.Cells.Add(totCell);
                defDataGroup.Rows.Add(totRow);

                doc.Blocks.Add(defTable);

                // Footer
                doc.Blocks.Add(new Paragraph(new Run("This report is system-generated by Property Manager."))
                {
                    FontSize = 8, Foreground = Brushes.Gray, TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 16, 0, 0)
                });

                var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
                paginator.PageSize = new Size(printDialog.PrintableAreaWidth, printDialog.PrintableAreaHeight);
                printDialog.PrintDocument(paginator, "Property Manager — Defaulters Report");

                MessageBox.Show("Defaulters report printed successfully!", "Print", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Print failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
