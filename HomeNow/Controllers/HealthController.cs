using System.Web.Mvc;
using Services.Redis;
using StackExchange.Redis;

public class HealthController : Controller
{
    [HttpGet]
    public ActionResult Redis()
    {
        IDatabase db;
        var ok = RedisConnection.TryGetDatabase(out db);

        return Content($"Redis Enabled={RedisConnection.IsEnabled}, Available={ok}");
    }

    [HttpGet]
    public ActionResult RedisFull()
    {
        IDatabase db;
        var ok = RedisConnection.TryGetDatabase(out db);

        if (!ok || db == null)
            return Json(new { enabled = RedisConnection.IsEnabled, available = false, error = RedisConnection.LastError }, JsonRequestBehavior.AllowGet);

        try
        {
            var pong = db.Ping();
            db.StringSet("hn:health:test", "1", System.TimeSpan.FromSeconds(10));
            var v = db.StringGet("hn:health:test");

            return Json(new
            {
                enabled = RedisConnection.IsEnabled,
                available = true,
                pingOk = true,
                pingMs = (int)pong.TotalMilliseconds,
                setGetOk = v == "1",
                error = (string)null
            }, JsonRequestBehavior.AllowGet);
        }
        catch (System.Exception ex)
        {
            RedisConnection.ReportFailure(ex);
            return Json(new { enabled = RedisConnection.IsEnabled, available = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
        }
    }
}
