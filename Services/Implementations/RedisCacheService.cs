using System;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using Services.Interfaces;
using Services.Redis;

namespace Services.Implementations
{
    public class RedisCacheService : IRedisCacheService
    {
        public bool IsEnabled => RedisConnection.IsEnabled;

        private bool TryDb(out IDatabase db)
        {
            db = null;
            if (!IsEnabled) return false;
            return RedisConnection.TryGetDatabase(out db) && db != null;
        }

        public Task<bool> DeleteAsync(string key)
        {
            try
            {
                if (!TryDb(out var db)) return Task.FromResult(false);
                return db.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                RedisConnection.ReportFailure(ex);
                return Task.FromResult(false);
            }
        }

        public async Task<string> GetStringAsync(string key)
        {
            try
            {
                if (!TryDb(out var db)) return null;
                var v = await db.StringGetAsync(key).ConfigureAwait(false);
                return v.HasValue ? v.ToString() : null;
            }
            catch (Exception ex)
            {
                RedisConnection.ReportFailure(ex);
                return null;
            }
        }

        // FIX: không dùng StringSetAsync(expiration) để né mismatch Expiration/TimeSpan
        public async Task<bool> SetStringAsync(string key, string value, TimeSpan? ttl = null)
        {
            try
            {
                if (!TryDb(out var db)) return false;

                if (ttl.HasValue && ttl.Value > TimeSpan.Zero)
                {
                    var seconds = (int)Math.Ceiling(ttl.Value.TotalSeconds);
                    await db.ExecuteAsync("SET", key, value, "EX", seconds).ConfigureAwait(false);
                }
                else
                {
                    await db.ExecuteAsync("SET", key, value).ConfigureAwait(false);
                }
                return true;
            }
            catch (Exception ex)
            {
                RedisConnection.ReportFailure(ex);
                return false;
            }
        }

        public Task<long> GetSetLengthAsync(string key)
        {
            try
            {
                if (!TryDb(out var db)) return Task.FromResult(0L);
                return db.SetLengthAsync(key);
            }
            catch (Exception ex)
            {
                RedisConnection.ReportFailure(ex);
                return Task.FromResult(0L);
            }
        }

        public async Task<int[]> GetSetMembersIntAsync(string key)
        {
            try
            {
                if (!TryDb(out var db)) return Array.Empty<int>();

                var values = await db.SetMembersAsync(key).ConfigureAwait(false);
                if (values == null || values.Length == 0) return Array.Empty<int>();

                return values
                    .Select(v => int.TryParse(v.ToString(), out var n) ? (int?)n : null)
                    .Where(x => x.HasValue)
                    .Select(x => x.Value)
                    .ToArray();
            }
            catch (Exception ex)
            {
                RedisConnection.ReportFailure(ex);
                return Array.Empty<int>();
            }
        }

        public Task<bool> AddToSetAsync(string key, int value)
        {
            try
            {
                if (!TryDb(out var db)) return Task.FromResult(false);
                return db.SetAddAsync(key, value);
            }
            catch (Exception ex)
            {
                RedisConnection.ReportFailure(ex);
                return Task.FromResult(false);
            }
        }

        public Task<bool> RemoveFromSetAsync(string key, int value)
        {
            try
            {
                if (!TryDb(out var db)) return Task.FromResult(false);
                return db.SetRemoveAsync(key, value);
            }
            catch (Exception ex)
            {
                RedisConnection.ReportFailure(ex);
                return Task.FromResult(false);
            }
        }

        public async Task<bool> ReplaceSetAsync(string key, int[] values)
        {
            try
            {
                if (!TryDb(out var db)) return false;

                var tran = db.CreateTransaction();
                _ = tran.KeyDeleteAsync(key);

                if (values != null && values.Length > 0)
                {
                    RedisValue[] rv = values.Select(x => (RedisValue)x).ToArray();
                    _ = tran.SetAddAsync(key, rv);
                }

                return await tran.ExecuteAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                RedisConnection.ReportFailure(ex);
                return false;
            }
        }

        //  không cần KeyExists nữa -> giảm 1 RTT mỗi lần check
        public Task<bool> KeyExistsAsync(string key) => Task.FromResult(false);
        public bool IsAvailable => false; // tránh gọi connect trong getter
    }
}
