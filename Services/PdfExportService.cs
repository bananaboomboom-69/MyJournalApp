using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using MyJournalApp.Models;

namespace MyJournalApp.Services
{
    /// <summary>
    /// Service for exporting journal entries to PDF format using QuestPDF.
    /// </summary>
    public class PdfExportService
    {
        private static PdfExportService? _instance;
        public static PdfExportService Instance => _instance ??= new PdfExportService();

        private readonly DatabaseService _database;

        private PdfExportService()
        {
            _database = DatabaseService.Instance;

            // Configure QuestPDF for free community license
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>
        /// Exports journal entries within the date range to a PDF file.
        /// Shows SaveFileDialog to let user choose save location.
        /// </summary>
        /// <param name="startDate">Start date (inclusive).</param>
        /// <param name="endDate">End date (inclusive).</param>
        /// <returns>True if export was successful, false if cancelled or failed.</returns>
        public bool ExportToPdf(DateTime startDate, DateTime endDate)
        {
            // Get entries in date range using existing search functionality
            var criteria = new SearchCriteria
            {
                StartDate = startDate,
                EndDate = endDate
            };

            var entries = _database.SearchEntries(criteria);

            if (entries.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "No journal entries found in the selected date range.",
                    "No Entries",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return false;
            }

            // Show save file dialog
            var saveDialog = new SaveFileDialog
            {
                Title = "Export Journal Entries to PDF",
                Filter = "PDF Files (*.pdf)|*.pdf",
                DefaultExt = "pdf",
                FileName = $"Journal_{startDate:yyyy-MM-dd}_to_{endDate:yyyy-MM-dd}.pdf"
            };

            if (saveDialog.ShowDialog() != true)
            {
                return false; // User cancelled
            }

            try
            {
                // Generate PDF document
                var document = CreatePdfDocument(entries, startDate, endDate);
                document.GeneratePdf(saveDialog.FileName);

                System.Windows.MessageBox.Show(
                    $"Successfully exported {entries.Count} entries to:\n{saveDialog.FileName}",
                    "Export Complete",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);

                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to export PDF: {ex.Message}",
                    "Export Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Exports specific entries to PDF (for manual entry selection).
        /// </summary>
        public bool ExportEntriesToPdf(List<JournalEntry> entries, string? suggestedFileName = null)
        {
            if (entries.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "No entries selected for export.",
                    "No Entries",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return false;
            }

            var saveDialog = new SaveFileDialog
            {
                Title = "Export Journal Entries to PDF",
                Filter = "PDF Files (*.pdf)|*.pdf",
                DefaultExt = "pdf",
                FileName = suggestedFileName ?? $"Journal_Export_{DateTime.Now:yyyy-MM-dd}.pdf"
            };

            if (saveDialog.ShowDialog() != true)
            {
                return false;
            }

            try
            {
                var minDate = entries.Min(e => e.EntryDate);
                var maxDate = entries.Max(e => e.EntryDate);

                var document = CreatePdfDocument(entries, minDate, maxDate);
                document.GeneratePdf(saveDialog.FileName);

                System.Windows.MessageBox.Show(
                    $"Successfully exported {entries.Count} entries.",
                    "Export Complete",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);

                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to export PDF: {ex.Message}",
                    "Export Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Creates the QuestPDF document with journal entries.
        /// </summary>
        private Document CreatePdfDocument(List<JournalEntry> entries, DateTime startDate, DateTime endDate)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    // Header
                    page.Header().Element(ComposeHeader);

                    // Content
                    page.Content().Element(content => ComposeContent(content, entries));

                    // Footer with page numbers
                    page.Footer().Element(ComposeFooter);
                });
            });

            void ComposeHeader(IContainer container)
            {
                container.Column(column =>
                {
                    column.Item().Text("My Journal")
                        .FontSize(24)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);

                    column.Item().Text($"Entries from {startDate:MMMM d, yyyy} to {endDate:MMMM d, yyyy}")
                        .FontSize(12)
                        .FontColor(Colors.Grey.Darken1);

                    column.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    column.Item().PaddingBottom(10);
                });
            }

            void ComposeContent(IContainer container, List<JournalEntry> journalEntries)
            {
                container.Column(column =>
                {
                    foreach (var entry in journalEntries.OrderByDescending(e => e.EntryDate))
                    {
                        column.Item().Element(c => ComposeEntry(c, entry));
                        column.Item().PaddingVertical(10).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten3);
                    }
                });
            }

            void ComposeEntry(IContainer container, JournalEntry entry)
            {
                container.Column(column =>
                {
                    // Date header
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text(entry.EntryDate.ToString("dddd, MMMM d, yyyy"))
                            .FontSize(14)
                            .Bold()
                            .FontColor(Colors.Blue.Darken1);
                    });

                    // Title
                    if (!string.IsNullOrWhiteSpace(entry.Title))
                    {
                        column.Item().PaddingTop(5).Text(entry.Title)
                            .FontSize(13)
                            .SemiBold();
                    }

                    // Moods
                    column.Item().PaddingTop(5).Row(row =>
                    {
                        row.AutoItem().Text("Mood: ").FontSize(10).FontColor(Colors.Grey.Darken1);
                        row.AutoItem().Text($"{GetMoodEmoji(entry.PrimaryMood)} {entry.PrimaryMood}")
                            .FontSize(10)
                            .FontColor(GetMoodColor(entry.PrimaryMood));

                        var secondaryMoods = entry.GetSecondaryMoods();
                        if (secondaryMoods.Count > 0)
                        {
                            row.AutoItem().Text("  +  ").FontSize(10).FontColor(Colors.Grey.Lighten1);
                            row.AutoItem().Text(string.Join(", ", secondaryMoods.Select(m => $"{GetMoodEmoji(m)} {m}")))
                                .FontSize(10)
                                .FontColor(Colors.Grey.Darken1);
                        }
                    });

                    // Tags
                    var tags = entry.GetTagsList();
                    if (tags.Count > 0)
                    {
                        column.Item().PaddingTop(3).Row(row =>
                        {
                            row.AutoItem().Text("Tags: ").FontSize(10).FontColor(Colors.Grey.Darken1);
                            row.AutoItem().Text(string.Join("  ", tags.Select(t => $"#{t}")))
                                .FontSize(10)
                                .FontColor(Colors.Blue.Medium);
                        });
                    }

                    // Content
                    column.Item().PaddingTop(8).Text(entry.Content)
                        .FontSize(11)
                        .LineHeight(1.4f);

                    // Word count
                    column.Item().PaddingTop(5).Text($"{entry.WordCount} words")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Lighten1);
                });
            }

            void ComposeFooter(IContainer container)
            {
                container.Row(row =>
                {
                    row.RelativeItem().Text($"Generated on {DateTime.Now:MMMM d, yyyy 'at' h:mm tt}")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Medium);

                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.Span("Page ").FontSize(9).FontColor(Colors.Grey.Medium);
                        text.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Medium);
                        text.Span(" of ").FontSize(9).FontColor(Colors.Grey.Medium);
                        text.TotalPages().FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });
            }
        }

        /// <summary>
        /// Gets an emoji for the mood type.
        /// </summary>
        private static string GetMoodEmoji(MoodType mood) => mood switch
        {
            MoodType.Happy => "😊",
            MoodType.Calm => "😌",
            MoodType.Anxious => "😰",
            MoodType.Productive => "⚡",
            MoodType.Sad => "😔",
            MoodType.Neutral => "😐",
            _ => "📝"
        };

        /// <summary>
        /// Gets a color for the mood type.
        /// </summary>
        private static string GetMoodColor(MoodType mood) => mood switch
        {
            MoodType.Happy => Colors.Yellow.Darken2,
            MoodType.Calm => Colors.Blue.Medium,
            MoodType.Anxious => Colors.Orange.Medium,
            MoodType.Productive => Colors.Green.Medium,
            MoodType.Sad => Colors.Purple.Medium,
            MoodType.Neutral => Colors.Grey.Medium,
            _ => Colors.Grey.Darken1
        };
    }
}
