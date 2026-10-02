using System.ComponentModel.DataAnnotations;

namespace JudgeMyTaste.Models;

public class FavoriteBand
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "Band/Artis")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Genre")]
    public string? Genre { get; set; }

    [StringLength(200)]
    [Display(Name = "Alasan suka")]
    public string? Reason { get; set; }
}