using Microsoft.AspNetCore.Mvc;

namespace FGFB.Controllers
{
    public class EventsController : Controller
    {
        [HttpGet]
        public IActionResult ChampionshipDraftWeekend()
        {
            return View("~/Views/Events/ChampionshipDraftWeekend.cshtml");
        }
        [HttpGet]
        public IActionResult ChampionshipDraftWeekend2()
        {
            return View("~/Views/Events/ChampionshipDraftWeekend2.cshtml");
        }

        [HttpGet]
        public IActionResult ChampionshipDraftWeekendSignup()
        {
            return View("~/Views/Events/ChampionshipDraftWeekendSignup.cshtml");
        }
    }
}
