using Core.Models;
using Data;
using HomeNow.Services.Implementations;
using HomeNow.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace HomeNow.Controllers
{
    public class PropertyController : Controller
    {
        private readonly IPropertyService _propertyService;
        private readonly IVrService _vrService;

        // Constructor mặc định (không dùng DI container)
        public PropertyController()
        {
            var db = new AppDbContext();
            _propertyService = new PropertyService(db);
            _vrService = new VrService(db);
        }

        // Nếu bạn có DI container thì thêm constructor khác nhận vào IPropertyService, IVrService.

        // ================== MÀN DANH SÁCH NHÀ ==================

        // GET: /Property/List?lang=vi
        public async Task<ActionResult> List(string lang = "vi")
        {
            lang = (lang ?? "vi").ToLower();

            var all = await _propertyService.GetAllWithTranslationsAsync();

            var list = all.Select(p =>
            {
                var t = p.Translations
                        ?.FirstOrDefault(x => x.LangCode == lang);

                return new PropertyListViewModel
                {
                    Id = p.Id,
                    Title = t?.DisplayTitle ?? p.DisplayTitle ?? p.Title,
                    Address = t?.Address ?? p.Address,
                    AreaName = t?.AreaName ?? p.AreaName,
                    CommunityName = t?.CommunityName ?? p.CommunityName,
                    RoomType = t?.RoomType ?? p.RoomType,
                    Orientation = t?.Orientation ?? p.Orientation,
                    District = t?.District ?? p.District,

                    CoverImageUrl = p.CoverImageUrl,
                    Price = p.PricePerMonth,
                    PriceUnit = p.PriceUnit,
                    AreaM2 = p.AreaM2,
                    IsVrAvailable = p.IsVrAvailable
                };
            }).ToList();

            ViewBag.Lang = lang;
            return View(list);
        }

        // ================== MÀN VR CHO 1 NHÀ ==================

        // GET: /Property/Vr/1?lang=vi
        public async Task<ActionResult> Vr(int id, string lang = "vi")
        {
            lang = (lang ?? "vi").ToLower();

            var scenes = await _vrService.GetScenesForPropertyAsync(id);
            if (scenes == null || !scenes.Any())
            {
                return Content("Không có dữ liệu VR cho căn nhà này.");
            }

            var vm = new VrViewModel
            {
                PropertyId = id,
                Lang = lang,
                Scenes = scenes
            };

            return View(vm);
        }

        // ================== VIEWMODEL NỘI BỘ ==================

        public class PropertyListViewModel
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string Address { get; set; }
            public string AreaName { get; set; }
            public string CommunityName { get; set; }
            public string District { get; set; }
            public string RoomType { get; set; }
            public string Orientation { get; set; }

            public string CoverImageUrl { get; set; }
            public decimal? Price { get; set; }
            public string PriceUnit { get; set; }
            public decimal? AreaM2 { get; set; }

            public bool IsVrAvailable { get; set; }
        }

        public class VrViewModel
        {
            public int PropertyId { get; set; }
            public string Lang { get; set; }
            public IEnumerable<VrScene> Scenes { get; set; }
        }
    }
}
