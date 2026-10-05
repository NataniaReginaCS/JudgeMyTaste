using System.ComponentModel.DataAnnotations;
using ClosedXML.Excel;
using JudgeMyTaste.Data;
using JudgeMyTaste.Models;
using Microsoft.EntityFrameworkCore;

namespace JudgeMyTaste.Services;

public record MusikImportResult(bool Success, int Imported, List<string> Errors)
{
    public static MusikImportResult Fail(params string[] errors) => new(false, 0, errors.ToList());
}

public class MusikExcelService
{
    public static readonly string[] Columns = { "Name", "Genre" };
    private const int MaxErrorsShown = 10;

    private readonly JudgeMyTasteContext _context;

    public MusikExcelService(JudgeMyTasteContext context)
    {
        _context = context;
    }

    public string SheetName =>
        _context.Model.FindEntityType(typeof(Musik))?.GetTableName() ?? "Musik";

    // ---------- EXPORT ----------
    public async Task<byte[]> ExportAsync()
    {
        var rows = await GetAllAsync();
        return BuildWorkbook(rows);
    }

    public async Task<List<Musik>> GetAllAsync()
    {
        return await _context.Set<Musik>()
            .FromSql($"EXEC dbo.usp_Musik_GetAll")
            .AsNoTracking()
            .ToListAsync();
    }

    public byte[] BuildTemplate() => BuildWorkbook(Array.Empty<Musik>());

    private byte[] BuildWorkbook(IEnumerable<Musik> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SheetName);

        for (int i = 0; i < Columns.Length; i++)
            sheet.Cell(1, i + 1).SetValue(Columns[i]);

        var header = sheet.Range(1, 1, 1, Columns.Length);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightGray;

        int row = 2;
        foreach (var m in rows)
        {
            sheet.Cell(row, 1).SetValue(m.Name);
            sheet.Cell(row, 2).SetValue(m.Genre ?? string.Empty);
            row++;
        }

        sheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    // ---------- IMPORT ----------
    public async Task<MusikImportResult> ImportAsync(Stream stream)
    {
        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(stream);
        }
        catch (Exception)
        {
            return MusikImportResult.Fail("File bukan workbook .xlsx yang valid.");
        }

        using (workbook)
        {
            if (!workbook.TryGetWorksheet(SheetName, out var sheet))
            {
                var found = string.Join(", ", workbook.Worksheets.Select(w => $"'{w.Name}'"));
                return MusikImportResult.Fail(
                    $"Tabel tidak cocok: sheet yang diharapkan bernama '{SheetName}', tetapi yang ditemukan {found}.");
            }

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
                var problems = new List<string> { $"Kolom tidak cocok dengan tabel '{SheetName}'." };
                if (missing.Count > 0) problems.Add("Kolom yang hilang: " + string.Join(", ", missing));
                if (unexpected.Count > 0) problems.Add("Kolom yang tidak dikenal: " + string.Join(", ", unexpected));
                if (duplicates.Count > 0) problems.Add("Kolom dobel: " + string.Join(", ", duplicates));
                problems.Add("Kolom yang diharapkan: " + string.Join(", ", Columns));
                return new MusikImportResult(false, 0, problems);
            }

            int lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
            var toImport = new List<Musik>();
            var rowErrors = new List<string>();
            int totalRowErrors = 0;

            for (int r = 2; r <= lastRow; r++)
            {
                var name = sheet.Cell(r, headerIndex["Name"]).GetString().Trim();
                var genre = sheet.Cell(r, headerIndex["Genre"]).GetString().Trim();

                if (name.Length == 0 && genre.Length == 0) continue; 

                var musik = new Musik
                {
                    Name = name,
                    Genre = genre.Length == 0 ? null : genre
                };

                var results = new List<ValidationResult>();
                if (!Validator.TryValidateObject(musik, new ValidationContext(musik), results, true))
                {
                    foreach (var res in results)
                    {
                        totalRowErrors++;
                        if (rowErrors.Count < MaxErrorsShown)
                            rowErrors.Add($"Baris {r}: {res.ErrorMessage}");
                    }
                }
                else
                {
                    toImport.Add(musik);
                }
            }

            if (totalRowErrors > 0)
            {
                if (totalRowErrors > rowErrors.Count)
                    rowErrors.Add($"...dan {totalRowErrors - rowErrors.Count} error lainnya. Tidak ada data yang diimport.");
                else
                    rowErrors.Add("Tidak ada data yang diimport.");
                return new MusikImportResult(false, 0, rowErrors);
            }

            if (toImport.Count == 0)
                return MusikImportResult.Fail("Sheet tidak berisi baris data untuk diimport.");

            try
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                foreach (var m in toImport)
                {
                    await _context.Database.ExecuteSqlAsync(
                        $"EXEC dbo.usp_Musik_Insert @Name = {m.Name}, @Genre = {m.Genre}");
                }
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                return MusikImportResult.Fail(
                    "Gagal menyimpan ke database. Tidak ada data yang diimport. Detail: "
                    + ex.GetBaseException().Message);
            }

            return new MusikImportResult(true, toImport.Count, new List<string>());
        }
    }
}
