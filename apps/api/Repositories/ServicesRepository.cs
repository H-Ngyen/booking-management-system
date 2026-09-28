using API.Data;
using API.Entities;
using API.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories;

public class ServicesRepository(DataContext context) : BaseRepository<Service>(context), IServicesRepository
{
    public async Task<Service?> CreateAsync(Service entity)
    {
        _dbContext.Services.Add(entity);
        await SaveChanges();
        return entity;
    }

    public async Task<(IEnumerable<Service>?, int)> GetAllMatchAsync(string? searchPhrase, int pageSize, int pageNumber, bool includeInactive)
    {
        string? searchPhraseLower = searchPhrase?.ToLower();

        IQueryable<Service>? baseQuery = _dbContext.Services
            .Where(s => (includeInactive || s.IsActive) &&
                (searchPhraseLower == null ||
                s.Name.ToLower().Contains(searchPhraseLower) || 
                (s.Description != null && s.Description.ToLower().Contains(searchPhraseLower))));

        int totalCount = await baseQuery.CountAsync();

        // long arithmetic: pageSize * (pageNumber - 1) overflows int32 for huge
        // page numbers (negative Skip => DB 2201X => 500). Clamp instead.
        long offset = (long)pageSize * (pageNumber - 1);
        int skip = offset > int.MaxValue ? int.MaxValue : (int)offset;

        IEnumerable<Service>? services = await baseQuery
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        return (services, totalCount);
    }

    public async Task<Service?> GetById(int id)
        => await TrackingQuery.FirstOrDefaultAsync(s => s.Id == id);

    public async Task SaveChanges() => await _dbContext.SaveChangesAsync();
}