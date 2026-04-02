using Contentful.Core.Configuration;
using FGFB.Models;
using FGFB.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace FGFB.Pages
{
    public class ArticlesModel : PageModel
    {

        private readonly ContentfulService _contentfulService;

        public ArticlesModel(ContentfulService contentfulService)
        {
            _contentfulService = contentfulService;
        }

        public List<Item> Items { get; private set; } = new();
        public Includes? Includes { get; private set; }

        public async Task OnGetAsync()
        {
            var data = await _contentfulService.GetContentfulEntriesAsync();

            if (data != null)
            {
                Items = data.Items ?? new List<Item>();
                Includes = data.Includes;
            }
        }

        //public async Task<ContentfulResponse> GetContentfulEntriesAsync()
        //{
        //    using var httpClient = new HttpClient();

        //    var options = new ContentfulOptions
        //    {
        //        DeliveryApiKey = "CNCPeStp5hELxc6tifknROVpGWAAIr93W31eNr3Pfro",
        //        SpaceId = "3hrbdcg88kgt"
        //    };

        //    var url =
        //        $"https://cdn.contentful.com/spaces/{options.SpaceId}/entries?access_token={options.DeliveryApiKey}&content_type=blogPost";

        //    var json = await httpClient.GetStringAsync(url);

        //    var result = JsonSerializer.Deserialize<ContentfulResponse>(json,
        //        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        //    return result!;
        //}
    }
    }
