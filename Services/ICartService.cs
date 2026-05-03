using SecureMarketMvc.Models;

namespace SecureMarketMvc.Services;

public interface ICartService
{
    Task<IReadOnlyList<CartItem>> GetItemsAsync(CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
    Task AddAsync(int productId, int quantity, CancellationToken cancellationToken = default);
    Task UpdateAsync(int productId, int quantity, CancellationToken cancellationToken = default);
    Task RemoveAsync(int productId, CancellationToken cancellationToken = default);
    Task ClearCurrentCartAsync(CancellationToken cancellationToken = default);
    Task MergeAnonymousCartIntoUserAsync(string userId, CancellationToken cancellationToken = default);
}
