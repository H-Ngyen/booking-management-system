using API.Common;
using API.Constraints;
using API.DTOs;
using API.Entities;

namespace API.Interfaces.Repositories;

public interface IServicesRepository
{
    Task<(IEnumerable<Service>?, int)> GetAllMatchAsync(string? searchPhrase, int pageSize, int pageNumber, bool includeInactive);
    Task<Service?> GetById(int id);
    Task<Service?> CreateAsync(Service entity);
    Task SaveChanges();
}