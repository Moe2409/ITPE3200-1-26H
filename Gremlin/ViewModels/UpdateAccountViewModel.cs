using System.ComponentModel.DataAnnotations;
namespace Gremlin.ViewModels;
public class UpdateAccountViewModel
{
    [Required]
    [Display(Name = "Display Name")]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}