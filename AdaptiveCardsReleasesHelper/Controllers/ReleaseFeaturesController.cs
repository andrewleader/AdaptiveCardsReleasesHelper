using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AdaptiveCards;
using AdaptiveCardsReleasesHelper.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;

namespace AdaptiveCardsReleasesHelper.Controllers
{
    [Produces("application/json")]
    [Route("api/ReleaseFeatures")]
    public class ReleaseFeaturesController : Controller
    {
        [HttpGet]
        public async Task<ContentResult> Get(bool refreshCard = false, bool refresh = false)
        {
            switch (Request.Headers["Accept"])
            {
                case "application/vnd.microsoft.card.adaptive":
                    return new ContentResult()
                    {
                        Content = await ReleaseFeaturesToCardHelper.GetCardAsync(refresh: refresh, refreshCard: refreshCard),
                        ContentType = "application/vnd.microsoft.card.adaptive"
                    };

                case "application/json":
                    return new ContentResult()
                    {
                        Content = JsonConvert.SerializeObject(await ReleaseFeaturesHelper.GetReleasesAsync(refresh: refresh)),
                        ContentType = "application/json"
                    };

                default:
                    return new ContentResult()
                    {
                        Content = "<html><body><pre>" + await ReleaseFeaturesToCardHelper.GetCardAsync(refresh: refresh, refreshCard: refreshCard) + "</pre></body></html>",
                        ContentType = "text/html"
                    };
            }
        }

        [HttpGet]
        [Route("carduri")]
        public string GetCardUri()
        {
            return ReleaseFeaturesToCardHelper.GetCardUri();
        }
    }
}