using System.Data;
using JudgeMyTaste.Data;
using JudgeMyTaste.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace JudgeMyTaste.Services;

public class MusikRepository
{
    private readonly JudgeMyTasteContext _context;

    public MusikRepository(JudgeMyTasteContext context)
    {
        _context = context;
    }

    // READ 
    public async Task<List<Musik>> GetAllAsync()
    {
        return await _context.Set<Musik>()
            .FromSql($"EXEC dbo.usp_Musik_GetAll")
            .AsNoTracking()
            .ToListAsync();
    }

    
    public async Task<Musik?> GetByIdAsync(int id)
    {
        var rows = await _context.Set<Musik>()
            .FromSql($"EXEC dbo.usp_Musik_GetById @Id = {id}")
            .AsNoTracking()
            .ToListAsync();

        return rows.FirstOrDefault();
    }

    // CREATE
    public Task<int> CreateAsync(string name, string? genre) =>
        ExecuteScalarAsync("dbo.usp_Musik_Insert",
            new SqlParameter("@Name", name),
            new SqlParameter("@Genre", (object?)genre ?? DBNull.Value));

    // UPDATE
    public Task<int> UpdateAsync(int id, string name, string? genre) =>
        ExecuteScalarAsync("dbo.usp_Musik_Update",
            new SqlParameter("@Id", id),
            new SqlParameter("@Name", name),
            new SqlParameter("@Genre", (object?)genre ?? DBNull.Value));

    // DELETE
    public Task<int> DeleteAsync(int id) =>
        ExecuteScalarAsync("dbo.usp_Musik_Delete",
            new SqlParameter("@Id", id));

    
    private async Task<int> ExecuteScalarAsync(string procedure, params SqlParameter[] parameters)
    {
        await _context.Database.OpenConnectionAsync();
        try
        {
            var connection = _context.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = procedure;
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddRange(parameters);
           
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }
    }
}
