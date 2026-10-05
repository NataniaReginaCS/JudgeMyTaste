using JudgeMyTaste.Models;
using JudgeMyTaste.Services;
using Microsoft.AspNetCore.Mvc;

namespace JudgeMyTaste.Controllers;

[ApiController]
[Route("api/musik")]
public class MusikApiController : ControllerBase
{
    private readonly MusikRepository _repo;

    public MusikApiController(MusikRepository repo)
    {
        _repo = repo;
    }

    // GET /api/musik 
    [HttpGet]
    public async Task<ActionResult<List<Musik>>> GetAll()
    {
        return Ok(await _repo.GetAllAsync());
    }

    // GET /api/musik/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Musik>> GetById(int id)
    {
        var item = await _repo.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    // POST /api/musik 
    [HttpPost]
    public async Task<ActionResult<Musik>> Create(MusikRequest request)
    {
        var id = await _repo.CreateAsync(request.Name, request.Genre);
        var created = await _repo.GetByIdAsync(id);
        return CreatedAtAction(nameof(GetById), new { id }, created);
    }

    // PUT /api/musik/{id} 
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, MusikRequest request)
    {
        var affected = await _repo.UpdateAsync(id, request.Name, request.Genre);
        return affected == 0 ? NotFound() : NoContent();
    }

    // DELETE /api/musik/{id} 
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var affected = await _repo.DeleteAsync(id);
        return affected == 0 ? NotFound() : NoContent();
    }
}
