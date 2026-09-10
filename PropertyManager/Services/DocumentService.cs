using PropertyManager.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace PropertyManager.Services
{
    /// <summary>
    /// Builds and prints formal documents (rent agreement / "mohada" and rent
    /// receipts) as FlowDocuments via the standard Windows print dialog.
    /// </summary>
    public static class DocumentService
    {
        private static readonly FontFamily BodyFont = new FontFamily("Segoe UI");

        // Theme colours used inside printed documents (match the app palette).
        private static readonly Brush Maroon = new SolidColorBrush(Color.FromRgb(0x8A, 0x2E, 0x3B));
        private static readonly Brush Gold = new SolidColorBrush(Color.FromRgb(0xB9, 0x79, 0x1F));
        private static readonly Brush Ink = new SolidColorBrush(Color.FromRgb(0x2C, 0x26, 0x20));
        private static readonly Brush Muted = new SolidColorBrush(Color.FromRgb(0x8A, 0x7D, 0x6B));

        // ─────────────────────────────────────────────────────────────
        //  RENT AGREEMENT  (Mohada / معاہدہ)
        // ─────────────────────────────────────────────────────────────
        public static void PrintAgreementDeed(Agreement agreement)
        {
            if (agreement == null)
            {
                MessageBox.Show("Select an agreement to print.", "Print Agreement",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() != true) return;

            var doc = NewDocument();

            string tenantName = agreement.Tenant?.Name ?? "____________________";
            string tenantCnic = string.IsNullOrWhiteSpace(agreement.Tenant?.CNIC) ? "____________________" : agreement.Tenant!.CNIC;
            string tenantPhone = string.IsNullOrWhiteSpace(agreement.Tenant?.Phone) ? "____________________" : agreement.Tenant!.Phone;
            string unitNo = agreement.Unit?.UnitNumber ?? "________";
            string propName = agreement.Unit?.Property?.Name ?? "____________________";
            string propLoc = string.IsNullOrWhiteSpace(agreement.Unit?.Property?.Location) ? "____________________" : agreement.Unit!.Property!.Location;
            string propType = agreement.Unit?.Property?.Type ?? "";
            int months = MonthsBetween(agreement.StartDate, agreement.EndDate);

            // ── Header ──
            doc.Blocks.Add(Centered("🏠  Property & Rent Management System", 17, FontWeights.Bold, Maroon, 0, 0, 0, 2));
            doc.Blocks.Add(Centered("RENT AGREEMENT  ·  کرایہ نامہ (معاہدہ)", 14, FontWeights.SemiBold, Gold, 0, 0, 0, 2));
            doc.Blocks.Add(Divider());

            // ── Preamble ──
            var intro = new Paragraph { Margin = new Thickness(0, 8, 0, 10), TextAlignment = TextAlignment.Justify };
            intro.Inlines.Add(new Run($"This Rent Agreement (“Mohada”) is made and executed on this "));
            intro.Inlines.Add(Bold(DateTime.Now.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture)));
            intro.Inlines.Add(new Run(" between the Owner/Landlord (the “First Party”) and the Tenant named below (the “Second Party”), on the following terms and conditions which are binding on both parties."));
            doc.Blocks.Add(intro);

            // ── Parties ──
            doc.Blocks.Add(SectionHeading("1.  Parties"));
            var parties = TwoColTable();
            AddRow(parties, "Owner / Landlord", "M/s. ____________________________");
            AddRow(parties, "Tenant Name", tenantName);
            AddRow(parties, "Tenant CNIC", tenantCnic);
            AddRow(parties, "Tenant Phone", tenantPhone);
            doc.Blocks.Add(parties);

            // ── Premises ──
            doc.Blocks.Add(SectionHeading("2.  Premises Let Out"));
            var premises = TwoColTable();
            AddRow(premises, "Property", propName + (string.IsNullOrEmpty(propType) ? "" : $"  ({propType})"));
            AddRow(premises, "Location", propLoc);
            AddRow(premises, "Unit / Shop No.", unitNo);
            doc.Blocks.Add(premises);

            // ── Term ──
            doc.Blocks.Add(SectionHeading("3.  Term of Tenancy"));
            var term = TwoColTable();
            AddRow(term, "Commencement Date", agreement.StartDate.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture));
            AddRow(term, "Expiry Date", agreement.EndDate.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture));
            AddRow(term, "Duration", $"{months} month(s)");
            doc.Blocks.Add(term);

            // ── Rent ──
            doc.Blocks.Add(SectionHeading("4.  Rent & Revision"));
            var rent = TwoColTable();
            AddRow(rent, "Monthly Rent", $"Rs. {agreement.BaseRent:N0}  ({AmountInWords(agreement.BaseRent)})");
            string revision = agreement.IncreasePercentage > 0 && agreement.IncreaseAfterMonths > 0
                ? $"Increase of {agreement.IncreasePercentage:0.##}% after every {agreement.IncreaseAfterMonths} month(s) ({agreement.IncreaseType})"
                : "No periodic increase agreed.";
            AddRow(rent, "Rent Revision", revision);
            doc.Blocks.Add(rent);

            // ── Terms & Conditions ──
            doc.Blocks.Add(SectionHeading("5.  Terms & Conditions"));
            string[] clauses =
            {
                "The Second Party (Tenant) shall pay the monthly rent in advance on or before the 5th day of each calendar month.",
                "The rent shall be revised automatically as stated in clause 4 above, and the revised amount shall become payable from the effective month.",
                "Utility charges including electricity, gas, water and maintenance shall be borne by the Second Party unless otherwise agreed in writing.",
                "The Second Party shall use the premises for lawful purposes only and shall not sublet or transfer possession without the written consent of the First Party.",
                "The Second Party shall keep the premises in good condition and shall be responsible for any damage caused during the tenancy.",
                "Either party may terminate this agreement by giving one (1) month prior written notice to the other party.",
                "On expiry or termination, the Second Party shall hand over vacant and peaceful possession of the premises to the First Party.",
                "Any dispute arising out of this agreement shall be settled amicably, failing which it shall be subject to the jurisdiction of the local courts."
            };
            var list = new List { MarkerStyle = TextMarkerStyle.Decimal, Margin = new Thickness(16, 2, 0, 8) };
            foreach (var c in clauses)
                list.ListItems.Add(new ListItem(new Paragraph(new Run(c)) { Margin = new Thickness(0, 0, 0, 4), TextAlignment = TextAlignment.Justify, FontSize = 10.5 }));
            doc.Blocks.Add(list);

            // ── Signatures ──
            doc.Blocks.Add(new Paragraph(new Run("IN WITNESS WHEREOF the parties have signed this agreement on the date first written above."))
            { Margin = new Thickness(0, 10, 0, 24), FontSize = 10.5 });

            var sign = new Table { CellSpacing = 0, Margin = new Thickness(0, 6, 0, 0) };
            sign.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            sign.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            var sg = new TableRowGroup();
            var sr = new TableRow();
            sr.Cells.Add(SignatureCell("_____________________________", "Owner / Landlord (First Party)"));
            sr.Cells.Add(SignatureCell("_____________________________", $"Tenant (Second Party){(tenantName.StartsWith("_") ? "" : "\n" + tenantName)}"));
            sg.Rows.Add(sr);
            var sr2 = new TableRow();
            sr2.Cells.Add(SignatureCell("_____________________________", "Witness 1"));
            sr2.Cells.Add(SignatureCell("_____________________________", "Witness 2"));
            sg.Rows.Add(sr2);
            sign.RowGroups.Add(sg);
            doc.Blocks.Add(sign);

            doc.Blocks.Add(Divider());
            doc.Blocks.Add(Centered($"Generated by Property Manager on {DateTime.Now:dd-MMM-yyyy HH:mm}", 8, FontWeights.Normal, Muted, 6, 0, 0, 0));

            PrintDoc(printDialog, doc, "Rent Agreement");
            MessageBox.Show("Agreement sent to printer.", "Print Agreement", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ─────────────────────────────────────────────────────────────
        //  RENT RECEIPT
        // ─────────────────────────────────────────────────────────────
        public static void PrintRentReceipt(Agreement agreement, Payment payment)
        {
            if (agreement == null || payment == null)
            {
                MessageBox.Show("Select a payment to print a receipt.", "Rent Receipt",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() != true) return;

            var doc = NewDocument();
            doc.PagePadding = new Thickness(50, 40, 50, 40);

            doc.Blocks.Add(Centered("🏠  Property & Rent Management System", 16, FontWeights.Bold, Maroon, 0, 0, 0, 2));
            doc.Blocks.Add(Centered("RENT RECEIPT", 13, FontWeights.SemiBold, Gold, 0, 0, 0, 2));
            doc.Blocks.Add(Divider());

            var receiptNo = $"RCPT-{payment.Id:0000}-{payment.PaymentDate:yyyyMMdd}";
            var meta = new Paragraph { Margin = new Thickness(0, 6, 0, 10) };
            meta.Inlines.Add(new Run($"Receipt No: ") { Foreground = Muted });
            meta.Inlines.Add(Bold(receiptNo));
            meta.Inlines.Add(new Run("        Date: ") { Foreground = Muted });
            meta.Inlines.Add(Bold(payment.PaymentDate.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture)));
            doc.Blocks.Add(meta);

            var t = TwoColTable();
            AddRow(t, "Received From", agreement.Tenant?.Name ?? "N/A");
            AddRow(t, "Property / Unit", $"{agreement.Unit?.Property?.Name ?? "N/A"} — {agreement.Unit?.UnitNumber ?? "N/A"}");
            AddRow(t, "Rent Month", payment.Month);
            AddRow(t, "Amount Paid", $"Rs. {payment.PaidAmount:N0}  ({AmountInWords(payment.PaidAmount)})");
            doc.Blocks.Add(t);

            doc.Blocks.Add(new Paragraph(new Run($"Rs. {payment.PaidAmount:N0}"))
            {
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = Maroon,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 8, 0, 24)
            });

            doc.Blocks.Add(new Paragraph(new Run("_____________________________"))
            { TextAlignment = TextAlignment.Right, Margin = new Thickness(0, 20, 0, 0) });
            doc.Blocks.Add(new Paragraph(new Run("Authorised Signature"))
            { TextAlignment = TextAlignment.Right, FontSize = 9, Foreground = Muted });

            doc.Blocks.Add(Divider());
            doc.Blocks.Add(Centered("This is a system-generated receipt from Property Manager.", 8, FontWeights.Normal, Muted, 6, 0, 0, 0));

            PrintDoc(printDialog, doc, "Rent Receipt");
            MessageBox.Show("Receipt sent to printer.", "Rent Receipt", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ─────────────────────────────────────────────────────────────
        //  Amount → words  (Pakistani numbering: crore / lakh / thousand)
        // ─────────────────────────────────────────────────────────────
        public static string AmountInWords(decimal amount)
        {
            long rupees = (long)Math.Floor(amount);
            if (rupees == 0) return "Rupees Zero Only";

            var sb = new StringBuilder("Rupees ");
            sb.Append(ConvertGroup(rupees));
            sb.Append(" Only");
            return sb.ToString();
        }

        private static string ConvertGroup(long n)
        {
            if (n == 0) return "Zero";
            var sb = new StringBuilder();

            long crore = n / 10000000; n %= 10000000;
            long lakh = n / 100000; n %= 100000;
            long thousand = n / 1000; n %= 1000;
            long hundred = n / 100; n %= 100;

            if (crore > 0) sb.Append(TwoDigits(crore)).Append(" Crore ");
            if (lakh > 0) sb.Append(TwoDigits(lakh)).Append(" Lakh ");
            if (thousand > 0) sb.Append(TwoDigits(thousand)).Append(" Thousand ");
            if (hundred > 0) sb.Append(Ones[hundred]).Append(" Hundred ");
            if (n > 0)
            {
                if (sb.Length > 0) sb.Append("and ");
                sb.Append(TwoDigits(n));
            }
            return sb.ToString().Trim();
        }

        private static readonly string[] Ones =
        {
            "Zero","One","Two","Three","Four","Five","Six","Seven","Eight","Nine","Ten",
            "Eleven","Twelve","Thirteen","Fourteen","Fifteen","Sixteen","Seventeen","Eighteen","Nineteen"
        };
        private static readonly string[] Tens =
        {
            "","","Twenty","Thirty","Forty","Fifty","Sixty","Seventy","Eighty","Ninety"
        };

        private static string TwoDigits(long n)
        {
            if (n < 20) return Ones[n];
            string res = Tens[n / 10];
            if (n % 10 > 0) res += " " + Ones[n % 10];
            return res;
        }

        // ─────────────────────────────────────────────────────────────
        //  FlowDocument helpers
        // ─────────────────────────────────────────────────────────────
        private static FlowDocument NewDocument() => new FlowDocument
        {
            PagePadding = new Thickness(45, 35, 45, 35),
            FontFamily = BodyFont,
            FontSize = 11,
            Foreground = Ink,
            ColumnWidth = 9999
        };

        private static int MonthsBetween(DateTime a, DateTime b)
        {
            int m = ((b.Year - a.Year) * 12) + (b.Month - a.Month);
            return Math.Max(0, m);
        }

        private static Paragraph Centered(string text, double size, FontWeight weight, Brush brush,
            double top, double right, double bottom, double left)
            => new Paragraph(new Run(text))
            {
                FontSize = size,
                FontWeight = weight,
                Foreground = brush,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(left, top, right, bottom)
            };

        private static Run Bold(string text) => new Run(text) { FontWeight = FontWeights.SemiBold };

        private static BlockUIContainer Divider() => new BlockUIContainer(
            new System.Windows.Shapes.Rectangle
            {
                Height = 1,
                Fill = new SolidColorBrush(Color.FromRgb(0xE4, 0xD9, 0xC4))
            })
        { Margin = new Thickness(0, 4, 0, 4) };

        private static Paragraph SectionHeading(string text) => new Paragraph(new Run(text))
        {
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = Maroon,
            Margin = new Thickness(0, 10, 0, 4)
        };

        private static Table TwoColTable()
        {
            var t = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 4) };
            t.Columns.Add(new TableColumn { Width = new GridLength(1.6, GridUnitType.Star) });
            t.Columns.Add(new TableColumn { Width = new GridLength(3.4, GridUnitType.Star) });
            t.RowGroups.Add(new TableRowGroup());
            return t;
        }

        private static void AddRow(Table t, string label, string value)
        {
            var row = new TableRow();
            row.Cells.Add(new TableCell(new Paragraph(new Run(label) { Foreground = Muted }) { Margin = new Thickness(2) })
            { BorderBrush = new SolidColorBrush(Color.FromRgb(0xEC, 0xE4, 0xD5)), BorderThickness = new Thickness(0, 0, 0, 0.5) });
            row.Cells.Add(new TableCell(new Paragraph(new Run(value) { FontWeight = FontWeights.SemiBold }) { Margin = new Thickness(2) })
            { BorderBrush = new SolidColorBrush(Color.FromRgb(0xEC, 0xE4, 0xD5)), BorderThickness = new Thickness(0, 0, 0, 0.5) });
            t.RowGroups[0].Rows.Add(row);
        }

        private static TableCell SignatureCell(string line, string caption)
        {
            var cell = new TableCell { Padding = new Thickness(0, 10, 20, 0) };
            cell.Blocks.Add(new Paragraph(new Run(line)) { Margin = new Thickness(0, 0, 0, 2) });
            foreach (var part in caption.Split('\n'))
                cell.Blocks.Add(new Paragraph(new Run(part) { Foreground = Muted }) { FontSize = 9, Margin = new Thickness(0) });
            return cell;
        }

        private static void PrintDoc(PrintDialog dlg, FlowDocument doc, string title)
        {
            var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
            paginator.PageSize = new Size(dlg.PrintableAreaWidth, dlg.PrintableAreaHeight);
            dlg.PrintDocument(paginator, $"Property Manager — {title}");
        }
    }
}
