using System.ComponentModel.DataAnnotations;

namespace JudgeMyTaste.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Username wajib diisi."), StringLength(50)]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password wajib diisi.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ingat saya")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Username wajib diisi.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username 3-50 karakter.")]
    [RegularExpression(@"^[a-zA-Z0-9_.-]+$", ErrorMessage = "Username hanya boleh huruf, angka, titik, strip, dan underscore.")]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Nama lengkap")]
    public string? FullName { get; set; }

    [Required(ErrorMessage = "Password wajib diisi.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password minimal 8 karakter.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Konfirmasi password wajib diisi.")]
    [Compare(nameof(Password), ErrorMessage = "Konfirmasi password tidak sama.")]
    [DataType(DataType.Password)]
    [Display(Name = "Konfirmasi password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
