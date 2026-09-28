using API.Data;
using API.Entities;
using API.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories;

public class StaffsRepository(DataContext context) : BaseRepository<Staff>(context), IStaffsRepository
{
    public async Task<Staff?> CreateAsync(Staff entity)
    {
        _dbContext.Staffs.Add(entity);
        await SaveChanges();
        return entity;
    }

    public async Task<(IEnumerable<Staff>?, int)> GetAllMatchAsync(string? searchPhrase, int pageSize, int pageNumber, bool includeInactive)
    {
        string? searchPhraseLower = searchPhrase?.ToLower();

        IQueryable<Staff>? baseQuery = _dbContext.Staffs
            .Where(s => (includeInactive || s.IsActive) &&
                (searchPhraseLower == null ||
                s.FullName.ToLower().Contains(searchPhraseLower) ||
                s.Email.ToLower().Contains(searchPhraseLower)));

        int totalCount = await baseQuery.CountAsync();

        // long arithmetic: pageSize * (pageNumber - 1) overflows int32 for huge
        // page numbers (negative Skip => DB 2201X => 500). Clamp instead.
        long offset = (long)pageSize * (pageNumber - 1);
        int skip = offset > int.MaxValue ? int.MaxValue : (int)offset;

        IEnumerable<Staff>? staffs = await baseQuery
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        return (staffs, totalCount);
    }

    public async Task<Staff?> GetById(int id)
        => await TrackingQuery.FirstOrDefaultAsync(s => s.Id == id);

    public async Task<bool> ExistsByEmailAsync(string email, int? excludeId = null)
        => await NoTrackingQuery.AnyAsync(s => s.Email == email && (excludeId == null || s.Id != excludeId));

    public async Task<bool> ExistsById(int id, int? excludeId = null)
        => await NoTrackingQuery.AnyAsync(s => s.Id == id && (excludeId == null || s.Id != excludeId));
    public async Task SaveChanges() => await _dbContext.SaveChangesAsync();

}
