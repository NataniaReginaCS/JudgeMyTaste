using JudgeMyTaste.Models;
using Microsoft.EntityFrameworkCore;

namespace JudgeMyTaste.Data;

public class JudgeMyTasteContext : DbContext
{
    public JudgeMyTasteContext(DbContextOptions<JudgeMyTasteContext> options)
        : base(options) { }

    public DbSet<FavoriteBand> FavoriteBands => Set<FavoriteBand>();
}