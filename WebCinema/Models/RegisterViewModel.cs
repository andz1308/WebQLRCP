using System.ComponentModel.DataAnnotations;

namespace WebCinema.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "H? tên là b?t bu?c")]
        [Display(Name = "H? và tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email là b?t bu?c")]
        [EmailAddress(ErrorMessage = "Email không h?p l?")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "S? ?i?n tho?i là b?t bu?c")]
        [Phone(ErrorMessage = "S? ?i?n tho?i không h?p l?")]
        [Display(Name = "S? ?i?n tho?i")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "M?t kh?u là b?t bu?c")]
        [StringLength(100, ErrorMessage = "M?t kh?u ph?i có ít nh?t {2} ký t?", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "M?t kh?u")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Xác nh?n m?t kh?u là b?t bu?c")]
        [DataType(DataType.Password)]
        [Display(Name = "Xác nh?n m?t kh?u")]
        [Compare("Password", ErrorMessage = "M?t kh?u và xác nh?n m?t kh?u không kh?p")]
        public string ConfirmPassword { get; set; }
    }
}
