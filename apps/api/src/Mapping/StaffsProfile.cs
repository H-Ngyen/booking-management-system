using API.DTOs;
using API.Entities;
using AutoMapper;

namespace API.Mapping;

public class StaffsProfile : Profile
{
    public StaffsProfile() {
        CreateMap<Staff, StaffDto>()
            .ReverseMap();
        CreateMap<CreateNewStaffRequest, Staff>();
        CreateMap<UpdateStaffRequest, Staff>();
    }
}
