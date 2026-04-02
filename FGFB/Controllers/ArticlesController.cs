using Microsoft.AspNetCore.Mvc;
using FGFB.Services;
using FGFB.Models;
using System.Linq;
using System.Threading.Tasks;


namespace FGFB.Controllers
{
    // Avoid clashing with the Razor Page at /Articles by using a distinct route prefix
    [Route("ArticlesApi")]
    public class ArticlesController : Controller
    {
        private readonly ContentfulService _contentfulService;

        public ArticlesController(ContentfulService contentfulService)
        {
            _contentfulService = contentfulService;
        }

        [HttpGet("/Articles/{slug}")]
        public async Task<IActionResult> Detail(string slug)
        {
            var data = await _contentfulService.GetContentfulEntriesAsync();

            var post = data?.Items?
                .FirstOrDefault(x => string.Equals(
                    x.Fields?.Slug,
                    slug,
                    StringComparison.OrdinalIgnoreCase));

            if (post == null)
            {
                return NotFound();
            }

            ViewBag.Includes = data?.Includes;
            return View("Detail", post);
        }
    }
}