using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Services.Redis
{
    public static class RedisConnection
    {
        private static readonly object _lock = new object();
        private static ConnectionMultiplexer _mux;
        private static Task _connectTask;

        private static DateTime _nextRetryUtc = DateTime.MinValue;
        private static string _lastError;

        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

        public static string LastError => _lastError;

        public static bool IsEnabled =>
            string.Equals(ConfigurationManager.AppSettings["Redis.Enabled"], "true", StringComparison.OrdinalIgnoreCase);

        public static string ConnectionString
        {
            get
            {
                var conn = ConfigurationManager.AppSettings["Redis.ConnectionString"];
                if (string.IsNullOrWhiteSpace(conn))
                    conn = "127.0.0.1:6379,abortConnect=false"; // ưu tiên IP để né DNS delay
                return conn;
            }
        }

        // Gọi ở Application_Start để “ấm” sẵn Redis (không bắt buộc)
        public static void Warmup()
        {
            if (!IsEnabled) return;
            EnsureConnecting();
        }

        // QUAN TRỌNG: TryGetDatabase KHÔNG connect sync nữa.
        // Nếu chưa có mux -> return false ngay (nhanh), đồng thời schedule connect nền.
        public static bool TryGetDatabase(out IDatabase db)
        {
            db = null;

            if (!IsEnabled) return false;

            try
            {
                var mux = _mux;
                if (mux != null && mux.IsConnected)
                {
                    db = mux.GetDatabase();
                    return db != null;
                }

                if (DateTime.UtcNow < _nextRetryUtc)
                    return false;

                EnsureConnecting(); // connect nền
                return false;
            }
            catch (Exception ex)
            {
                ReportFailure(ex);
                return false;
            }
        }

        private static void EnsureConnecting()
        {
            if (!IsEnabled) return;
            if (DateTime.UtcNow < _nextRetryUtc) return;

            // nếu đang connect rồi thì thôi
            var t = _connectTask;
            if (t != null && !t.IsCompleted) return;

            lock (_lock)
            {
                var mux = _mux;
                if (mux != null && mux.IsConnected) return;

                t = _connectTask;
                if (t != null && !t.IsCompleted) return;

                _connectTask = Task.Run(async () =>
                {
                    try
                    {
                        var opt = ConfigurationOptions.Parse(ConnectionString);
                        opt.AbortOnConnectFail = false;
                        opt.ConnectRetry = 1;
                        opt.ConnectTimeout = 800; // đừng để lâu
                        opt.SyncTimeout = 800;
                        opt.KeepAlive = 15;

                        var newMux = await ConnectionMultiplexer.ConnectAsync(opt).ConfigureAwait(false);

                        newMux.ConnectionFailed += (_, e) =>
                        {
                            _lastError = "ConnectionFailed: " + e.Exception?.Message;
                            _nextRetryUtc = DateTime.UtcNow.Add(RetryDelay);
                        };
                        newMux.ConnectionRestored += (_, __) =>
                        {
                            _lastError = null;
                            _nextRetryUtc = DateTime.MinValue;
                        };

                        _mux = newMux;
                        _lastError = null;
                        _nextRetryUtc = DateTime.MinValue;
                    }
                    catch (Exception ex)
                    {
                        ReportFailure(ex);
                    }
                });
            }
        }

        public static void ReportFailure(Exception ex)
        {
            _lastError = ex?.GetType().Name + ": " + ex?.Message;
            _nextRetryUtc = DateTime.UtcNow.Add(RetryDelay);
        }
    }
}
