namespace Infrastructure.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        Task<int> CommitAsync(Guid changeMakerID);
    }
}
