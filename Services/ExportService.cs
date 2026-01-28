using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Markdig;
using myjournal.Models;
using PdfColors = QuestPDF.Helpers.Colors;

namespace myjournal.Services;

/// <summary>
/// Interface for export operations
/// </summary>
public interface IExportService
{
    Task<string> ExportToPdfAsync(List<JournalEntry> entries, string fileName);
    Task<string> ExportDateRangeToPdfAsync(DateTime startDate, DateTime endDate, string fileName);
}

/// <summary>
/// Service for exporting journal entries to PDF
/// </summary>
public class ExportService : IExportService
{
    private readonly IJournalService _journalService;

    public ExportService(IJournalService journalService)
    {
        _journalService = journalService;

        // Set QuestPDF license (Community license for open source)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<string> ExportToPdfAsync(List<JournalEntry> entries, string fileName)
    {
        var exportPath = Path.Combine(FileSystem.AppDataDirectory, "exports");
        Directory.CreateDirectory(exportPath);

        var filePath = Path.Combine(exportPath, $"{fileName}.pdf");

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .Text("My Journal")
                    .FontSize(24)
                    .Bold()
                    .FontColor(PdfColors.Indigo.Medium);

                page.Content()
                    .PaddingVertical(10)
                    .Column(column =>
                    {
                        column.Spacing(15);

                        if (entries.Count == 0)
                        {
                            column.Item().Text("No entries found for the selected date range.")
                                .Italic()
                                .FontColor(PdfColors.Grey.Medium);
                        }
                        else
                        {
                            foreach (var entry in entries.OrderByDescending(e => e.EntryDate))
                            {
                                column.Item().Element(container => ComposeEntry(container, entry));
                            }
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Exported on ");
                        text.Span(DateTime.Now.ToString("MMMM dd, yyyy"));
                        text.Span(" - Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
            });
        });

        await Task.Run(() => document.GeneratePdf(filePath));
        return filePath;
    }

    public async Task<string> ExportDateRangeToPdfAsync(DateTime startDate, DateTime endDate, string fileName)
    {
        var entries = await _journalService.GetEntriesByDateRangeAsync(startDate, endDate);
        return await ExportToPdfAsync(entries, fileName);
    }

    private void ComposeEntry(QuestPDF.Infrastructure.IContainer container, JournalEntry entry)
    {
        container
            .Border(1)
            .BorderColor(PdfColors.Grey.Lighten2)
            .Background(PdfColors.Grey.Lighten5)
            .Padding(15)
            .Column(column =>
            {
                // Header with date and title
                column.Item().Row(row =>
                {
                    row.AutoItem()
                        .Background(PdfColors.Indigo.Medium)
                        .Padding(5)
                        .Text(entry.EntryDate.ToString("MMM dd"))
                        .FontSize(10)
                        .FontColor(PdfColors.White)
                        .Bold();

                    row.RelativeItem()
                        .PaddingLeft(10)
                        .AlignMiddle()
                        .Text(entry.Title)
                        .FontSize(14)
                        .Bold();
                });

                column.Item().PaddingTop(5).Row(row =>
                {
                    // Mood
                    row.AutoItem()
                        .Text($"{entry.PrimaryMood.GetEmoji()} {entry.PrimaryMood}")
                        .FontSize(10)
                        .FontColor(PdfColors.Grey.Darken1);

                    if (entry.SecondaryMood1.HasValue)
                    {
                        row.AutoItem()
                            .PaddingLeft(10)
                            .Text($"{entry.SecondaryMood1.Value.GetEmoji()} {entry.SecondaryMood1.Value}")
                            .FontSize(10)
                            .FontColor(PdfColors.Grey.Darken1);
                    }

                    if (entry.SecondaryMood2.HasValue)
                    {
                        row.AutoItem()
                            .PaddingLeft(10)
                            .Text($"{entry.SecondaryMood2.Value.GetEmoji()} {entry.SecondaryMood2.Value}")
                            .FontSize(10)
                            .FontColor(PdfColors.Grey.Darken1);
                    }
                });

                // Tags
                if (entry.Tags.Any())
                {
                    column.Item().PaddingTop(5).Row(row =>
                    {
                        foreach (var tag in entry.Tags)
                        {
                            row.AutoItem()
                                .PaddingRight(5)
                                .Background(PdfColors.Indigo.Lighten4)
                                .Padding(3)
                                .Text($"#{tag.Name}")
                                .FontSize(9);
                        }
                    });
                }

                // Content (convert markdown to plain text for PDF)
                column.Item()
                    .PaddingTop(10)
                    .Text(StripMarkdown(entry.Content))
                    .FontSize(11)
                    .LineHeight(1.4f);

                // Word count
                column.Item()
                    .PaddingTop(5)
                    .AlignRight()
                    .Text($"{entry.WordCount} words")
                    .FontSize(9)
                    .FontColor(PdfColors.Grey.Medium)
                    .Italic();
            });
    }

    private static string StripMarkdown(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        // Convert markdown to HTML then strip HTML tags for plain text
        var html = Markdown.ToHtml(markdown);

        // Simple HTML tag removal
        var plainText = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", " ");
        plainText = System.Text.RegularExpressions.Regex.Replace(plainText, @"\s+", " ");

        return System.Net.WebUtility.HtmlDecode(plainText.Trim());
    }
}
