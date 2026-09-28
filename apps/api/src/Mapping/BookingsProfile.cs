using API.DTOs;
using API.Entities;
using AutoMapper;

namespace API.Mapping;

public class BookingsProfile : Profile
{
    public BookingsProfile() {
        CreateMap<Booking, BookingDto>()
            .ForMember(d => d.ServiceName, o => o.MapFrom(s => s.Service.Name))
            .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.Customer.UserName))
            .ForMember(d => d.StaffName, o => o.MapFrom(s => s.Staff.FullName));
    }
}
