namespace JudgeMyTaste.Models;

public record AppUser(int Id, string Username, string PasswordHash, string? FullName, string Role);
