using AdaptiveCards;
using AdaptiveCardsReleasesHelper.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AdaptiveCardsReleasesHelper.Helpers
{
    public static class ReleaseFeaturesToCardHelper
    {
        public static Task<string> GetCardAsync(bool refreshCard = false)
        {
            return BlobHelper.GetCachedOrRefresh("releasescard.json", ActuallyGetCardAsync, cacheDurationInMinutes: refreshCard ? 0 : 5);
        }

        private static async Task<string> ActuallyGetCardAsync()
        {
            var releases = await ReleaseFeaturesHelper.GetReleasesAsync();

            return CreateCardFromReleases(releases);
        }

        private static string CreateCardFromReleases(List<Release> releases)
        {
            AdaptiveCard card = new AdaptiveCard()
            {
                Version = "1.0"
            };

            foreach (var release in releases)
            {
                card.Body.Add(new AdaptiveTextBlock()
                {
                    Text = release.Title + " release",
                    Size = AdaptiveTextSize.Medium,
                    Weight = AdaptiveTextWeight.Bolder
                });

                foreach (var feature in release.Requests)
                {
                    card.Body.Add(new AdaptiveTextBlock()
                    {
                        Text = $"[{feature.Title}](https://github.com/microsoft/adaptivecards/issues/{feature.IssueNumber})",
                        Wrap = true
                    });
                }
            }

            return card.ToJson();
        }
    }
}
