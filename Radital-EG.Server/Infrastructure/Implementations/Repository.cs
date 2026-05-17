using Domain;
using Infrastructure;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Linq.Expressions;

namespace Infrastructure.Implementations
{
    public class Repository<T> : IRepository<T>
        where T : IdentifiableEntity
    {
        ILogger logger;
        ApplicationDbContext dbContext;
        DbSet<T> dbSet;
        private bool disposed = false;

        public Repository(ApplicationDbContext _dbContext, ILogger<Repository<T>> _logger)
        {
            dbContext = _dbContext;
            logger = _logger;
            dbContext.Database.EnsureCreated();
            dbSet = dbContext.Set<T>();
        }

        #region CRUD Operations
        public async Task InsertAsync(T entity)
        {
            if (await dbContext.Database.CanConnectAsync())
            {
                try
                {
                    await dbSet.AddAsync(entity);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Object could not be inserted. Repository is of type #{objectType}", typeof(T).Name);
                    throw;
                }
                logger.LogInformation("Object has been inserted into the database with id:#{entityId}. Repository is of type #{objectType}.", entity.Id.ToString(), typeof(T).Name);
            }
            else
            {
                logger.LogCritical("Unable to reach database!");
                throw new Exception("Database couldn't be reached");
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            if (await dbContext.Database.CanConnectAsync())
            {
                try
                {
                    T? entityToBeDeleted = await dbSet.FindAsync(id);
                    if (entityToBeDeleted == null)
                    {
                        logger.LogWarning("Unable to find the requested object to be deleted in the database. Id: #{Id} does not exist in current records", id.ToString());
                    }
                    else
                    {
                        Delete(entityToBeDeleted);
                        logger.LogInformation("Object with id:#{entityId} has been deleted from the database. Repository is of type #{objectType}.", id.ToString(), typeof(T).Name);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Object could not be deleted. Repository is of type #{objectType}", typeof(T).Name);
                    throw;
                }
            }
            else
            {
                logger.LogCritical("Unable to reach database!");
                throw new Exception("Database couldn't be reached");
            }

        }
        public virtual void Delete(T entityToDelete)
        {
            if (dbContext.Entry(entityToDelete).State == EntityState.Detached)
            {
                dbSet.Attach(entityToDelete);
            }
            dbSet.Remove(entityToDelete);
        }


        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> filter = null, 
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            params Expression<Func<T, object>>[] includes)
        {
            if (await dbContext.Database.CanConnectAsync())
            {
                try
                {
                    IQueryable<T> query = dbSet;
                    if (filter != null)
                        query = query.Where(filter);
                    if (orderBy != null)
                        query = orderBy(query);
                    if (includes != null)
                    {
                        foreach (var include in includes)
                        {
                            query = query.Include(include);
                        }
                    }

                    return await query?.ToListAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Couldn't fetch items. Repository is of type #{objectType}", typeof(T).Name);
                    throw;
                }
            }
            else
            {
                logger.LogCritical("Unable to reach database!");
                throw new Exception("Database couldn't be reached");
            }
        }

        public async Task<IEnumerable<T>> GetAllAsync(Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            params Expression<Func<T, object>>[] includes)
        {
            if (await dbContext.Database.CanConnectAsync())
            {
                try
                {
                    IQueryable<T> query = dbSet;
                    if (includes != null)
                    {
                        foreach (var include in includes)
                        {
                            query = query.Include(include);
                        }
                    }
                    if (orderBy != null)
                        query = orderBy(query);
                    return await query.ToListAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Couldn't fetch all items. Repository is of type #{objectType}", typeof(T).Name);
                    throw;
                }
            }
            else
            {
                logger.LogCritical("Unable to reach database!");
                throw new Exception("Database couldn't be reached");
            }
        }

        public async Task<T> GetByIdAsync(Guid id, params Expression<Func<T, object>>[] includes)
        {
            if (await dbContext.Database.CanConnectAsync())
            {
                try
                {
                    IQueryable<T> query = dbSet;
                    if (includes != null)
                    {
                        foreach(var include in includes)
                        {
                            query = query.Include(include);
                        }
                    }
                        
                    return await query.FirstOrDefaultAsync(entity => entity.Id.Equals(id));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Id could not be searched. Repository is of type #{objectType}", typeof(T).Name);
                    throw;
                }
            }
            else
            {
                logger.LogCritical("Unable to reach database!");
                throw new Exception("Database couldn't be reached");
            }
        }

        public async Task<T> UpdateAsync(T entity)
        {
            if (await dbContext.Database.CanConnectAsync())
            {
                if(await GetByIdAsync(entity.Id) == null)
                {
                    logger.LogWarning("The object associated with the Id #{objectId} could not be found for the updating process. Repository is of type #{objectType}", entity.Id, typeof(T).Name);
                    throw new Exception($"The object associated with the Id {entity.Id} could not be found for the updating process. Repository is of type {typeof(T).Name}");
                }

                try
                {
                    dbSet.Attach(entity);
                    dbContext.Entry(entity).State = EntityState.Modified;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Object could not be updated. Repository is of type #{objectType}", typeof(T).Name);
                    throw;
                }
                logger.LogInformation("Object has been updated into the database with id:#{entityId}. Repository is of type #{objectType}.", entity.Id.ToString(), typeof(T).Name);
                return entity;
            }
            else
            {
                logger.LogCritical("Unable to reach database!");
                throw new Exception("Database couldn't be reached");
            }
        }

        public async Task<T> GetByIdNestedSearchAsync(Guid id, int maxLevel = 4)
        {
            if (await dbContext.Database.CanConnectAsync())
            {
                try
                {
                    IQueryable<T> query = dbSet;
                    IEnumerable<string> allNavigationProperties = await GetNestedNavigationPropertyNames(maxLevel);
                    foreach (string navigationProperty in allNavigationProperties)
                        query = query.Include(navigationProperty);

                    return await query.FirstOrDefaultAsync(entity => entity.Id.Equals(id));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Id could not be searched. Repository is of type #{objectType}", typeof(T).Name);
                    throw;
                }
            }
            else
            {
                logger.LogCritical("Unable to reach database!");
                throw new Exception("Database couldn't be reached");
            }
        }

        public async Task<IEnumerable<T>> FindNestedSearchAsync(Expression<Func<T, bool>> filter = null, int maxLevel = 4, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null)
        {
            if (await dbContext.Database.CanConnectAsync())
            {
                try
                {
                    IQueryable<T> query = dbSet;
                    if (filter != null)
                        query = query.Where(filter);
                    if (orderBy != null)
                        query = orderBy(query);
                    IEnumerable<string> allNavigationProperties = await GetNestedNavigationPropertyNames(maxLevel);
                    foreach (string navigationProperty in allNavigationProperties)
                        query = query.Include(navigationProperty);

                    return await query?.ToListAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Couldn't fetch items. Repository is of type #{objectType}", typeof(T).Name);
                    throw;
                }
            }
            else
            {
                logger.LogCritical("Unable to reach database!");
                throw new Exception("Database couldn't be reached");
            }
        }

        public async Task<IEnumerable<T>> GetAllNestedSearchAsync(int maxLevel = 4, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null)
        {
            if (await dbContext.Database.CanConnectAsync())
            {
                try
                {
                    IQueryable<T> query = dbSet;
                    IEnumerable<string> allNavigationProperties = await GetNestedNavigationPropertyNames(maxLevel);
                    foreach (string navigationProperty in allNavigationProperties)
                        query = query.Include(navigationProperty);
                    if (orderBy != null)
                        query = orderBy(query);
                    List<T> result;
                    result = await query.ToListAsync();
                    return result;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Couldn't fetch all items. Repository is of type #{objectType}", typeof(T).Name);
                    throw;
                }
            }
            else
            {
                logger.LogCritical("Unable to reach database!");
                throw new Exception("Database couldn't be reached");
            }
        }

        private IEnumerable<string> GetNavigationPropertyNames()
        {
            var entityType = dbContext.Model.FindEntityType(typeof(T));
            if (entityType == null)
                return Enumerable.Empty<string>();

            return entityType.GetNavigations()
                .Select(nav => nav.Name)
                .ToList();
        }


        private async Task<IEnumerable<string>> GetNestedNavigationPropertyNames(int maxDepth = 2)
        {
            var result = new List<string>();
            var entityType = dbContext.Model.FindEntityType(typeof(T));

            if (entityType == null)
                return result;

            await Task.Run(() =>
                GetNestedNavigationsRecursive(entityType, "", maxDepth, ref result, new HashSet<Type>())
            );
            return result;
        }

        private void GetNestedNavigationsRecursive(
            Microsoft.EntityFrameworkCore.Metadata.IEntityType entityType,
            string currentPath,
            int remainingDepth,
            ref List<string> result,
            HashSet<Type> visitedTypes)
        {
            if (remainingDepth <= 0 || visitedTypes.Contains(entityType.ClrType))
                return;

            visitedTypes.Add(entityType.ClrType);

            var navigations = entityType.GetNavigations();

            foreach (var navigation in navigations)
            {
                var navigationPath = string.IsNullOrEmpty(currentPath)
                    ? navigation.Name
                    : $"{currentPath}.{navigation.Name}";

                result.Add(navigationPath);

                // Recursively get nested navigations
                var targetEntityType = navigation.TargetEntityType;
                GetNestedNavigationsRecursive(targetEntityType, navigationPath, remainingDepth - 1, ref result, new HashSet<Type>(visitedTypes));
            }
        }

        #endregion

        #region IUnitOfWorkImplementation

        public async Task<int> CommitAsync(Guid changeMakerID)
        {
            int numberOfChanges = await dbContext.SaveChangesAsync();
            return numberOfChanges;
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    dbContext.Dispose();
                }
            }
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
