using API.DTOs;
using API.Entities;
using AutoMapper;

namespace API.Mapping;

public class BookingsProfile : Profile
{
    public BookingsProfile() {
        CreateMap<Booking, BookingDto>();
    }
}
