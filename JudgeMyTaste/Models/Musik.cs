using System.ComponentModel.DataAnnotations;

namespace JudgeMyTaste.Models
{
    public class Musik
    {
        public int Id { get; set; }
        [Required, StringLength(100)]
        [Display(Name = "Judul Lagu")]
        public string Name { get; set; } = string.Empty;
        [StringLength(50)]
        [Display(Name = "Genre")]
        public string? Genre { get; set; }

    }
}
