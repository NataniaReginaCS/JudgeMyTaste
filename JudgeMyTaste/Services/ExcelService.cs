using System.ComponentModel.DataAnnotations;
using ClosedXML.Excel;
using JudgeMyTaste.Data;
using JudgeMyTaste.Models;
using Microsoft.EntityFrameworkCore;

namespace JudgeMyTaste.Services;

public record ImportResult(bool Success, int Imported, List<string> Errors)
{
    public static ImportResult Fail(params string[] errors) => new(false, 0, errors.ToList());
}

public class ExcelService
{
    // Columns the Excel file must contain (Id is excluded: the database generates it).
    private static readonly string[] Columns = { "Name", "Genre", "Reason" };
    private const int MaxErrorsShown = 10;

    private readonly JudgeMyTasteContext _context;

    public ExcelService(JudgeMyTasteContext context)
    {
        _context = context;
    }

    // Sheet name = table name in the database (read from the EF model, i.e. what migrations created).
    private string SheetName =>
        _context.Model.FindEntityType(typeof(FavoriteBand))?.GetTableName() ?? "FavoriteBands";

    // ---------- EXPORT ----------
    public async Task<byte[]> ExportAsync()
    {
        var bands = await _context.FavoriteBands
            .AsNoTracking()
            .OrderBy(b => b.Id)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SheetName);

        for (int i = 0; i < Columns.Length; i++)
            sheet.Cell(1, i + 1).SetValue(Columns[i]);

        var header = sheet.Range(1, 1, 1, Columns.Length);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightGray;

        int row = 2;
        foreach (var b in bands)
        {
            // SetValue keeps everything as plain text (no formula/number guessing).
            sheet.Cell(row, 1).SetValue(b.Name);
            sheet.Cell(row, 2).SetValue(b.Genre ?? string.Empty);
            sheet.Cell(row, 3).SetValue(b.Reason ?? string.Empty);
            row++;
        }

        sheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    // ---------- IMPORT ----------
    public async Task<ImportResult> ImportAsync(Stream stream)
    {
        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(stream);
        }
        catch (Exception)
        {
            return ImportResult.Fail("The file is not a valid .xlsx workbook.");
        }

        using (workbook)
        {
            // 1. Table check: a sheet named like the database table must exist.
            if (!workbook.TryGetWorksheet(SheetName, out var sheet))
            {
                var found = string.Join(", ", workbook.Worksheets.Select(w => $"'{w.Name}'"));
                return ImportResult.Fail(
                    $"Table mismatch: expected a sheet named '{SheetName}', but found {found}.");
            }

            // 2. Column check: header row must contain exactly the table's columns.
            int lastCol = sheet.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0;
            var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var duplicates = new List<string>();

            for (int c = 1; c <= lastCol; c++)
            {
                var name = sheet.Cell(1, c).GetString().Trim();
                if (name.Length == 0) continue;
                if (!headerIndex.TryAdd(name, c)) duplicates.Add(name);
            }

            var missing = Columns.Where(c => !headerIndex.ContainsKey(c)).ToList();
            var unexpected = headerIndex.Keys
                .Where(h => !Columns.Contains(h, StringComparer.OrdinalIgnoreCase)).ToList();

            if (missing.Count > 0 || unexpected.Count > 0 || duplicates.Count > 0)
            {
                var problems = new List<string> { "Column mismatch with table '" + SheetName + "'." };
                if (missing.Count > 0) problems.Add("Missing columns: " + string.Join(", ", missing));
                if (unexpected.Count > 0) problems.Add("Unexpected columns: " + string.Join(", ", unexpected));
                if (duplicates.Count > 0) problems.Add("Duplicate columns: " + string.Join(", ", duplicates));
                problems.Add("Expected columns: " + string.Join(", ", Columns));
                return new ImportResult(false, 0, problems);
            }

            // 3. Read and validate every data row (same rules as the form: [Required], [StringLength]).
            int lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
            var toImport = new List<FavoriteBand>();
            var rowErrors = new List<string>();
            int totalRowErrors = 0;

            for (int r = 2; r <= lastRow; r++)
            {
                var name = sheet.Cell(r, headerIndex["Name"]).GetString().Trim();
                var genre = sheet.Cell(r, headerIndex["Genre"]).GetString().Trim();
                var reason = sheet.Cell(r, headerIndex["Reason"]).GetString().Trim();

                if (name.Length == 0 && genre.Length == 0 && reason.Length == 0) continue; // blank row

                var band = new FavoriteBand
                {
                    Name = name,
                    Genre = genre.Length == 0 ? null : genre,
                    Reason = reason.Length == 0 ? null : reason
                };

                var results = new List<ValidationResult>();
                if (!Validator.TryValidateObject(band, new ValidationContext(band), results, true))
                {
                    foreach (var res in results)
                    {
                        totalRowErrors++;
                        if (rowErrors.Count < MaxErrorsShown)
                            rowErrors.Add($"Row {r}: {res.ErrorMessage}");
                    }
                }
                else
                {
                    toImport.Add(band);
                }
            }

            if (totalRowErrors > 0)
            {
                if (totalRowErrors > rowErrors.Count)
                    rowErrors.Add($"...and {totalRowErrors - rowErrors.Count} more. Nothing was imported.");
                else
                    rowErrors.Add("Nothing was imported.");
                return new ImportResult(false, 0, rowErrors);
            }

            if (toImport.Count == 0)
                return ImportResult.Fail("The sheet has no data rows to import.");

            // 4. Everything is valid: save all rows in one SaveChanges (all-or-nothing).
            _context.FavoriteBands.AddRange(toImport);
            await _context.SaveChangesAsync();
            return new ImportResult(true, toImport.Count, new List<string>());
        }
    }
}
