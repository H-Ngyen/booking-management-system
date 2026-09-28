using API.DTOs;
using API.Entities;
using AutoMapper;

namespace API.Mapping;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, CurrentUser>();
        CreateMap<User, UserDto>();
    }
}