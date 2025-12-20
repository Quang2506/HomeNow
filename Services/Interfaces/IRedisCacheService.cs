using System;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IRedisCacheService
    {
        bool IsEnabled { get; }
        bool IsAvailable { get; }

        Task<string> GetStringAsync(string key);
        Task<bool> SetStringAsync(string key, string value, TimeSpan? ttl = null);

        Task<bool> DeleteAsync(string key);

        // (Giữ lại để tương thích, dù controller mới không cần KeyExists nữa)
        Task<bool> KeyExistsAsync(string key);

        // Set<int> cho favorites
        Task<long> GetSetLengthAsync(string key);
        Task<int[]> GetSetMembersIntAsync(string key);
        Task<bool> AddToSetAsync(string key, int value);
        Task<bool> RemoveFromSetAsync(string key, int value);
        Task<bool> ReplaceSetAsync(string key, int[] values);
    }
}
