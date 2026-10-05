using JudgeMyTaste.Data;
using JudgeMyTaste.Models;
using JudgeMyTaste.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JudgeMyTaste.Controllers;

[Authorize(Roles = "Admin")]
public class MusikController : Controller
{
    private const string XlsxContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly JudgeMyTasteContext _context;
    private readonly MusikExcelService _excel;

    public MusikController(JudgeMyTasteContext context, MusikExcelService excel)
    {
        _context = context;
        _excel = excel;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _excel.GetAllAsync(); 

        ViewData["SheetName"] = _excel.SheetName;
        ViewData["Columns"] = MusikExcelService.Columns;
        return View(items);
    }

    public async Task<IActionResult> Export()
    {
        var bytes = await _excel.ExportAsync();
        return File(bytes, XlsxContentType, $"Musik_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }

    public IActionResult Template()
    {
        return File(_excel.BuildTemplate(), XlsxContentType, "Musik_template.xlsx");
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile? file)
    {
        const long maxBytes = 5 * 1024 * 1024; 

        if (file is null || file.Length == 0)
        {
            TempData["ImportErrors"] = "Pilih file .xlsx terlebih dahulu.";
            return RedirectToAction(nameof(Index));
        }

        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            TempData["ImportErrors"] = "Hanya file .xlsx yang didukung.";
            return RedirectToAction(nameof(Index));
        }

        if (file.Length > maxBytes)
        {
            TempData["ImportErrors"] = "Ukuran file terlalu besar (maksimal 5 MB).";
            return RedirectToAction(nameof(Index));
        }

        using var stream = file.OpenReadStream();
        var result = await _excel.ImportAsync(stream);

        if (result.Success)
            TempData["ImportSuccess"] = $"{result.Imported} lagu berhasil diimport.";
        else
            TempData["ImportErrors"] = string.Join("\n", result.Errors);

        return RedirectToAction(nameof(Index));
    }
}
