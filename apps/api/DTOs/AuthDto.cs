using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

// Request
public class LoginRequest
{
    [Required(ErrorMessage = "Tên đăng nhập là bắt buộc.")]
    [RegularExpression(
        @"^[a-zA-Z0-9_.]{3,30}$",
        ErrorMessage = "Tên đăng nhập chỉ được chứa chữ cái, số, dấu chấm và dấu gạch dưới, từ 3-30 ký tự."
    )]
    public required string UserName { get; set; }

    [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
    public required string Password { get; set; }
}


// Response
