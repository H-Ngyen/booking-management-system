using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

//Request
public class GetAllMatchStaffRequest
{
    public string? SearchPhrase { get; set; }

    [Range(1, 100, ErrorMessage = "PageSize phải từ 1 đến 100.")]
    public int PageSize { get; set; } = 20;

    [Range(1, int.MaxValue, ErrorMessage = "PageNumber phải lớn hơn 0.")]
    public int PageNumber { get; set; } = 1;
}

public class CreateNewStaffRequest
{
    [Required(ErrorMessage = "Tên nhân viên là bắt buộc.")]
    [StringLength(200, ErrorMessage = "Tên nhân viên tối đa 200 ký tự.")]
    public required string FullName { get; set; }

    [Required(ErrorMessage = "Email là bắt buộc.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(256, ErrorMessage = "Email tối đa 256 ký tự.")]
    public required string Email { get; set; }
}

public class UpdateStaffRequest
{
    [Range(0, int.MaxValue, ErrorMessage = "Id không hợp lệ")]
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên nhân viên là bắt buộc.")]
    [StringLength(200, ErrorMessage = "Tên nhân viên tối đa 200 ký tự.")]
    public required string FullName { get; set; }

    [Required(ErrorMessage = "Email là bắt buộc.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(256, ErrorMessage = "Email tối đa 256 ký tự.")]
    public required string Email { get; set; }

    public bool IsActive { get; set; }
}

// Response
public class StaffDto
{
    public int Id { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public bool IsActive { get; set; }
}
