using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

//Request
public class GetSchedulesRequest
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}

public class CreateScheduleRequest : IValidatableObject
{
    [Required(ErrorMessage = "Ngày làm việc là bắt buộc (YYYY-MM-DD).")]
    public DateOnly? WorkDate { get; set; }

    [Required(ErrorMessage = "Giờ bắt đầu là bắt buộc (HH:mm).")]
    public TimeOnly? StartTime { get; set; }

    [Required(ErrorMessage = "Giờ kết thúc là bắt buộc (HH:mm).")]
    public TimeOnly? EndTime { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartTime.HasValue &&
            EndTime.HasValue &&
            StartTime >= EndTime)
        {
            yield return new ValidationResult("Giờ bắt đầu phải nhỏ hơn giờ kết thúc.");
        }
    }
}

// Response
public class WorkScheduleDto
{
    public int Id { get; set; }
    public int StaffId { get; set; }
    public required string WorkDate { get; set; }
    public required string StartTime { get; set; }
    public required string EndTime { get; set; }
}
