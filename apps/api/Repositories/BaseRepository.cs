using API.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories;

public abstract class BaseRepository<T> (DataContext dbContext) where T : class
{
    protected DataContext _dbContext => dbContext; 
    protected IQueryable<T> NoTrackingQuery => _dbContext.Set<T>().AsNoTracking();
    protected IQueryable<T> TrackingQuery => _dbContext.Set<T>();
}