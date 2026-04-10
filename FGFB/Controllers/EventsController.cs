using Microsoft.AspNetCore.Mvc;

namespace FGFB.Controllers
{
    public class EventsController : Controller
    {
        [HttpGet]
        public IActionResult ChampionshipDraftWeekend()
        {
            return View();
        }
    }
}
