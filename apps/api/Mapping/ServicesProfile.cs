using API.DTOs;
using API.Entities;
using AutoMapper;

namespace API.Mapping;

public class ServicesProfile : Profile
{
    public ServicesProfile() {
        CreateMap<Service, ServiceDto>()
            .ReverseMap();
        CreateMap<CreateNewServiceRequest, Service>();
        CreateMap<UpdateServiceRequest, Service>();
    }
}