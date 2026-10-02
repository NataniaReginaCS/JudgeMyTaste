using JudgeMyTaste.Data;
using JudgeMyTaste.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JudgeMyTaste.Controllers;

public class FavoriteBandsController : Controller
{
    private readonly JudgeMyTasteContext _context;

    public FavoriteBandsController(JudgeMyTasteContext context)
    {
        _context = context;
    }

    // READ: daftar semua band
    public async Task<IActionResult> Index()
    {
        var bands = await _context.FavoriteBands.AsNoTracking().ToListAsync();
        return View(bands);
    }

    // CREATE: tampilkan form kosong
    public IActionResult Create() => View();

    // CREATE: terima data form
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Genre,Reason")] FavoriteBand band)
    {
        if (!ModelState.IsValid) return View(band);

        _context.Add(band);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // UPDATE: tampilkan form berisi data lama
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();

        var band = await _context.FavoriteBands.FindAsync(id);
        if (band is null) return NotFound();

        return View(band);
    }

    // UPDATE: simpan perubahan
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Genre,Reason")] FavoriteBand band)
    {
        if (id != band.Id) return NotFound();
        if (!ModelState.IsValid) return View(band);

        try
        {
            _context.Update(band);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.FavoriteBands.AnyAsync(b => b.Id == id))
                return NotFound();
            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    // DELETE: halaman konfirmasi
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return NotFound();

        var band = await _context.FavoriteBands.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id);
        if (band is null) return NotFound();

        return View(band);
    }

    // DELETE: eksekusi hapus
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var band = await _context.FavoriteBands.FindAsync(id);
        if (band is not null) _context.FavoriteBands.Remove(band);

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}