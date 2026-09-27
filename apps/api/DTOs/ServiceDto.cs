using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

//Request
public class GetAllMatchServiceRequest
{
    public string? SearchPhrase { get; set; }

    [Range(1, 100, ErrorMessage = "PageSize phải từ 1 đến 100.")]
    public int PageSize { get; set; } = 20;

    [Range(1, int.MaxValue, ErrorMessage = "PageNumber phải lớn hơn 0.")]
    public int PageNumber { get; set; } = 1;
}

public class CreateNewServiceRequest
{
    [Required(ErrorMessage = "Tên dịch vụ là bắt buộc.")]
    [StringLength(200, ErrorMessage = "Tên dịch vụ tối đa 200 ký tự.")]
    public required string Name { get; set; }

    [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự.")]
    public string? Description { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Thời lượng phải lớn hơn 0 (phút).")]
    public int DurationMinutes { get; set; }

    [Range(0, (double)decimal.MaxValue, ErrorMessage = "Giá không được âm.")]
    public decimal Price { get; set; }
}

public class UpdateServiceRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Id không hợp lệ.")]
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên dịch vụ là bắt buộc.")]
    [StringLength(200, ErrorMessage = "Tên dịch vụ tối đa 200 ký tự.")]
    public required string Name { get; set; }

    [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự.")]
    public string? Description { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Thời lượng phải lớn hơn 0 (phút).")]
    public int DurationMinutes { get; set; }

    [Range(0, (double)decimal.MaxValue, ErrorMessage = "Giá không được âm.")]
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}

// Response
public class ServiceDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}
