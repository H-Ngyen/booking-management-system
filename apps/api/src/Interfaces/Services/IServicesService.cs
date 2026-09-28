using API.Common;
using API.DTOs;

namespace API.Interfaces.Services;

public interface IServicesService
{
    Task<PagedResult<ServiceDto>> GetAllMatch(GetAllMatchServiceRequest request);
    Task<int> Create(CreateNewServiceRequest request);
    Task Update(UpdateServiceRequest request);
}