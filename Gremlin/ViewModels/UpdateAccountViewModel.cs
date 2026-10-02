using System.ComponentModel.DataAnnotations;
namespace Gremlin.ViewModels;
public class UpdateAccountViewModel
{
    [Required]
    [Display(Name = "Name")]
    public string UserName { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    // Making new password is optional
    public string? Password { get; set; } = string.Empty;
}