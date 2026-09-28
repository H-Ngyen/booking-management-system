using API.DTOs;
using API.Entities;
using AutoMapper;

namespace API.Mapping;

public class WorkSchedulesProfile : Profile
{
    public WorkSchedulesProfile() {
        CreateMap<WorkSchedule, WorkScheduleDto>()
            .ForMember(d => d.WorkDate, o => o.MapFrom(s => s.WorkDate.ToString("yyyy-MM-dd")))
            .ForMember(d => d.StartTime, o => o.MapFrom(s => s.StartTime.ToString("HH:mm")))
            .ForMember(d => d.EndTime, o => o.MapFrom(s => s.EndTime.ToString("HH:mm")));
    }
}
