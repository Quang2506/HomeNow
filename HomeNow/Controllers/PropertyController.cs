using System.Threading.Tasks;
using System.Web.Mvc;
using Services.Implementations;
using Services.Interfaces;

namespace HomeNow.Controllers
{
    public class PropertyController : Controller
    {
        private readonly IPropertyService _propertyService;

        public PropertyController()
        {
            _propertyService = new PropertyService();
        }

        // /Property/List?mode=rent&cityId=...&priceRange=...&propertyType=...&keyword=...
        public async Task<ActionResult> List(
            string mode = "rent",
            int? cityId = null,
            string priceRange = null,
            string propertyType = null,
            string keyword = null)
        {
            var uiLang = System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
            var langCode = uiLang.Length >= 2
                ? uiLang.Substring(0, 2).ToLower()
                : "vi";

            var list = await _propertyService.SearchAsync(
                langCode,
                mode,
                cityId,
                priceRange,
                propertyType,
                keyword);   // <-- THAM SỐ THỨ 6

            return View(list);
        }
    }
}
