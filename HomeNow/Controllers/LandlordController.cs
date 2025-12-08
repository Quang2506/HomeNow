using System.Threading.Tasks;
using System.Web.Mvc;
using Core.Models;
using Services.Implementations;
using Services.Interfaces;

namespace HomeNow.Controllers
{
    public class LandlordController : Controller
    {
        private readonly ILandlordRequestService _service;

        public LandlordController()
        {
            _service = new LandlordRequestService();
        }

        [HttpGet]
        public ActionResult Request()
        {
            return View(new LandlordRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Request(LandlordRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.OwnerName) ||
                string.IsNullOrWhiteSpace(model.Phone) ||
                string.IsNullOrWhiteSpace(model.Address))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ Họ tên, Số điện thoại và Địa chỉ.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _service.CreateAsync(model);
            TempData["Success"] = "Cảm ơn bạn! NhàNow sẽ liên hệ lại trong thời gian sớm nhất.";
            return RedirectToAction("Request");
        }
    }
}
