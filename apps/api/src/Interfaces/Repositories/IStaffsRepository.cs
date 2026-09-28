using API.Entities;

namespace API.Interfaces.Repositories;

public interface IStaffsRepository
{
    Task<(IEnumerable<Staff>?, int)> GetAllMatchAsync(string? searchPhrase, int pageSize, int pageNumber, bool includeInactive);
    Task<Staff?> GetById(int id);
    Task<Staff?> CreateAsync(Staff entity);
    Task<bool> ExistsByEmailAsync(string email, int? excludeId = null);
    Task<bool> ExistsById(int id, int? excludeId = null);
    Task SaveChanges();
}
