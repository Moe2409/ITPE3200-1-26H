using System.ComponentModel.DataAnnotations;
namespace Gremlin.ViewModels;
public class UpdateAccountViewModel
{
    [Required]
    [Display(Name = "Name")]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string Password { get; set; } = string.Empty;
}