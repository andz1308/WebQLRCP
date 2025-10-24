using System.ComponentModel.DataAnnotations;

namespace WebCinema.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "H? t�n l� b?t bu?c")]
        [Display(Name = "H? v� t�n")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email l� b?t bu?c")]
        [EmailAddress(ErrorMessage = "Email kh�ng h?p l?")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "S? ?i?n tho?i l� b?t bu?c")]
        [Phone(ErrorMessage = "S? ?i?n tho?i kh�ng h?p l?")]
        [Display(Name = "S? ?i?n tho?i")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "M?t kh?u l� b?t bu?c")]
        [StringLength(100, ErrorMessage = "M?t kh?u ph?i c� �t nh?t {2} k� t?", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "M?t kh?u")]
        public string Password { get; set; }

        [Required(ErrorMessage = "X�c nh?n m?t kh?u l� b?t bu?c")]
        [DataType(DataType.Password)]
        [Display(Name = "X�c nh?n m?t kh?u")]
        [Compare("Password", ErrorMessage = "M?t kh?u v� x�c nh?n m?t kh?u kh�ng kh?p")]
        public string ConfirmPassword { get; set; }
    }
}
