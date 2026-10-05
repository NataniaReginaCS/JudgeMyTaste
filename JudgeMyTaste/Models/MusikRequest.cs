using System.ComponentModel.DataAnnotations;

namespace JudgeMyTaste.Models;

public class MusikRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Genre { get; set; }
}
