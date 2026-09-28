using API.Common;
using API.Constraints;
using API.DTOs;
using API.Entities;
using API.Exceptions;
using API.Interfaces;
using API.Interfaces.Authorization;
using API.Interfaces.Repositories;
using API.Interfaces.Services;
using AutoMapper;

namespace API.Services;

public class ServicesService(IServicesRepository servicesRepository,
    IMapper mapper,
    IUserContext userContext,
    IUserRepository userRepository,
    IServicesAuthorization servicesAuthorization) : IServicesService
{
    public async Task<int> Create(CreateNewServiceRequest request)
    {
        CurrentUser currentUser = userContext.GetCurrentUser();
        User user = await userRepository.GetUserById(currentUser.Id) ?? throw new ForbidException();

        if(!servicesAuthorization.Authorize(user, ResourceOperation.Create))
            throw new ForbidException();

        Service newService = CreateNewService(request);
        newService = await servicesRepository.CreateAsync(newService) ?? throw new BadRequestException();
        return newService.Id;
    }

    public async Task<PagedResult<ServiceDto>> GetAllMatch(GetAllMatchServiceRequest request)
    {
        CurrentUser currentUser = userContext.GetCurrentUser();
        User user = await userRepository.GetUserById(currentUser.Id) ?? throw new ForbidException();

        if(!servicesAuthorization.Authorize(user, ResourceOperation.Read))
            throw new ForbidException();

        bool isAdmin = currentUser.Role == nameof(UserRole.Admin);
        (IEnumerable<Service>? services, int totalCount) = await servicesRepository.GetAllMatchAsync(
            request.SearchPhrase,
            request.PageSize,
            request.PageNumber,
            isAdmin);

        var serviceDtos = mapper.Map<IEnumerable<ServiceDto>>(services);
        var result = new PagedResult<ServiceDto>(serviceDtos, totalCount, request.PageSize, request.PageNumber);

        return result;
    }

    public async Task Update(UpdateServiceRequest request)
    {
        CurrentUser currentUser = userContext.GetCurrentUser();
        User user = await userRepository.GetUserById(currentUser.Id) ?? throw new ForbidException();

        if(!servicesAuthorization.Authorize(user, ResourceOperation.Update))
            throw new ForbidException();

        Service service = await servicesRepository.GetById(request.Id) ?? throw new NotFoundException();
        
        mapper.Map(request, service);
        service.UpdatedAt = VnClock.Now;    
        
        await servicesRepository.SaveChanges();
    }

    private Service CreateNewService(CreateNewServiceRequest request)
    {
        Service newService = mapper.Map<Service>(request);
        newService.CreatedAt = VnClock.Now;
        newService.UpdatedAt = VnClock.Now;
        return newService;
    }
}