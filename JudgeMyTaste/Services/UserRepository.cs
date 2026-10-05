using System.Data;
using JudgeMyTaste.Data;
using JudgeMyTaste.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace JudgeMyTaste.Services;

public class UserRepository
{
    private readonly JudgeMyTasteContext _context;

    public UserRepository(JudgeMyTasteContext context)
    {
        _context = context;
    }

    public async Task<AppUser?> GetByUsernameAsync(string username)
    {
        await _context.Database.OpenConnectionAsync();
        try
        {
            var connection = _context.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.usp_User_GetByUsername";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@Username", username));

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new AppUser(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.GetString(4));
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }
    }

    public async Task<int> CreateAsync(string username, string passwordHash, string? fullName)
    {
        await _context.Database.OpenConnectionAsync();
        try
        {
            var connection = _context.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.usp_User_Create";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@Username", username));
            command.Parameters.Add(new SqlParameter("@PasswordHash", passwordHash));
            command.Parameters.Add(new SqlParameter("@FullName", (object?)fullName ?? DBNull.Value));

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            return 0;
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }
    }
}
