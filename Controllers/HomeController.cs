using Microsoft.AspNetCore.Mvc;
using ParkingLot.Services;

namespace ParkingLot.Controllers
{
    public class HomeController : Controller
    {
        private readonly IParkingService _parkingService;

        public HomeController(IParkingService parkingService)
        {
            _parkingService = parkingService;
        }

        public async Task<IActionResult> Index()
        {
            var model = await _parkingService.GetAreaBDataAsync();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CheckIn([FromBody] TagRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.TagNumber))
                return Json(new { success = false, message = "Tag number is required." });

            var result = await _parkingService.CheckInAsync(request.TagNumber);
            if (!result.Success)
                return Json(new { success = false, message = result.Message });

            var areaB = await _parkingService.GetAreaBDataAsync();
            var html = await RenderPartialViewToString("_AreaB", areaB);
            return Json(new { success = true, message = result.Message, areaBHtml = html });
        }

        [HttpPost]
        public async Task<IActionResult> CheckOut([FromBody] TagRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.TagNumber))
                return Json(new { success = false, message = "Tag number is required." });

            var result = await _parkingService.CheckOutAsync(request.TagNumber);
            if (!result.Success)
                return Json(new { success = false, message = result.Message });

            var areaB = await _parkingService.GetAreaBDataAsync();
            var html = await RenderPartialViewToString("_AreaB", areaB);
            return Json(new { success = true, message = result.Message, amountCharged = result.AmountCharged, areaBHtml = html });
        }

        [HttpGet]
        public async Task<IActionResult> Stats()
        {
            var stats = await _parkingService.GetStatsAsync();
            return Json(stats);
        }

        private async Task<string> RenderPartialViewToString(string viewName, object model)
        {
            ViewData.Model = model;
            using var sw = new System.IO.StringWriter();
            var viewResult = HttpContext.RequestServices
                .GetRequiredService<Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine>()
                .FindView(ControllerContext, viewName, false);

            var viewContext = new Microsoft.AspNetCore.Mvc.Rendering.ViewContext(
                ControllerContext,
                viewResult.View!,
                ViewData,
                TempData,
                sw,
                new Microsoft.AspNetCore.Mvc.ViewFeatures.HtmlHelperOptions()
            );

            await viewResult.View!.RenderAsync(viewContext);
            return sw.GetStringBuilder().ToString();
        }
    }

    public class TagRequest
    {
        public string? TagNumber { get; set; }
    }
}
