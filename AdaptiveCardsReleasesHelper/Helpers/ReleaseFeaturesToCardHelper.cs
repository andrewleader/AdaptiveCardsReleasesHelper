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
        public static Task<string> GetCardAsync(bool refresh = false, bool refreshCard = false)
        {
            return BlobHelper.GetCachedOrRefresh("releasescard.json", delegate { return ActuallyGetCardAsync(refresh); }, cacheDurationInMinutes: refresh || refreshCard ? 0 : 5);
        }

        private static async Task<string> ActuallyGetCardAsync(bool refresh)
        {
            var releases = await ReleaseFeaturesHelper.GetReleasesAsync(refresh: refresh);

            return CreateCardFromReleases(releases);
        }

        private static string CreateCardFromReleases(List<Release> releases)
        {
            AdaptiveCard card = new AdaptiveCard()
            {
                Version = "1.0"
            };

            bool firstRelease = true;
            foreach (var release in releases)
            {
                card.Body.Add(new AdaptiveTextBlock()
                {
                    Text = release.Title + " release",
                    Size = AdaptiveTextSize.Medium,
                    Weight = AdaptiveTextWeight.Bolder,
                    Spacing = firstRelease ? AdaptiveSpacing.Default : AdaptiveSpacing.Large
                });
                firstRelease = false;

                bool firstFeatureInRelease = true;
                foreach (var feature in release.Requests)
                {
                    card.Body.Add(new AdaptiveTextBlock()
                    {
                        Text = $"[{feature.Title}](https://github.com/microsoft/adaptivecards/issues/{feature.IssueNumber})",
                        Wrap = true,
                        Weight = AdaptiveTextWeight.Bolder
                    });
                    firstFeatureInRelease = false;

                    if (feature.Spec != null)
                    {
                        card.Body.Add(new AdaptiveColumnSet()
                        {
                            Spacing = AdaptiveSpacing.Small,
                            Columns =
                            {
                                new AdaptiveColumn()
                                {
                                    Width = "auto",
                                    Items =
                                    {
                                        new AdaptiveContainer()
                                        {
                                            Style = AdaptiveContainerStyle.Emphasis,
                                            Items =
                                            {
                                                new AdaptiveTextBlock()
                                                {
                                                    Text = "Spec",
                                                    Size = AdaptiveTextSize.Small,
                                                    Weight = AdaptiveTextWeight.Bolder
                                                }
                                            }
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Width = "auto",
                                    Items =
                                    {
                                        new AdaptiveContainer()
                                        {
                                            Style = AdaptiveContainerStyle.Emphasis,
                                            Items =
                                            {
                                                new AdaptiveTextBlock()
                                                {
                                                    Text = feature.Spec.SpecStatus.ToString(),
                                                    Size = AdaptiveTextSize.Small,
                                                    Weight = AdaptiveTextWeight.Bolder,
                                                    Color = GetColorForStatus(feature.Spec.SpecStatus)
                                                }
                                            }
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Width = "stretch",
                                    VerticalContentAlignment = AdaptiveVerticalContentAlignment.Center,
                                    Items =
                                    {
                                        new AdaptiveTextBlock()
                                        {
                                            Text = $"[{feature.Spec.Title}](https://github.com/microsoft/adaptivecards/issues/{feature.Spec.IssueNumber})",
                                            Size = AdaptiveTextSize.Small
                                        }
                                    }
                                }
                            }
                        });
                    }

                    else if (feature.Proposals != null && feature.Proposals.Any())
                    {
                        foreach (var proposal in feature.Proposals)
                        {
                            card.Body.Add(new AdaptiveColumnSet()
                            {
                                Spacing = AdaptiveSpacing.Small,
                                Columns =
                                {
                                    new AdaptiveColumn()
                                    {
                                        Width = "auto",
                                        Items =
                                        {
                                            new AdaptiveContainer()
                                            {
                                                Style = AdaptiveContainerStyle.Emphasis,
                                                Items =
                                                {
                                                    new AdaptiveTextBlock()
                                                    {
                                                        Text = "Proposal",
                                                        Size = AdaptiveTextSize.Small,
                                                        Weight = AdaptiveTextWeight.Bolder
                                                    }
                                                }
                                            }
                                        }
                                    },
                                    new AdaptiveColumn()
                                    {
                                        Width = "auto",
                                        Items =
                                        {
                                            new AdaptiveContainer()
                                            {
                                                Style = AdaptiveContainerStyle.Emphasis,
                                                Items =
                                                {
                                                    new AdaptiveTextBlock()
                                                    {
                                                        Text = proposal.SpecStatus.ToString(),
                                                        Size = AdaptiveTextSize.Small,
                                                        Weight = AdaptiveTextWeight.Bolder,
                                                        Color = GetColorForStatus(proposal.SpecStatus)
                                                    }
                                                }
                                            }
                                        }
                                    },
                                    new AdaptiveColumn()
                                    {
                                        Width = "stretch",
                                        VerticalContentAlignment = AdaptiveVerticalContentAlignment.Center,
                                        Items =
                                        {
                                            new AdaptiveTextBlock()
                                            {
                                                Text = $"[{proposal.Title}](https://github.com/microsoft/adaptivecards/issues/{proposal.IssueNumber})",
                                                Size = AdaptiveTextSize.Small
                                            }
                                        }
                                    }
                                }
                            });
                        }
                    }

                    // Else proposal needed
                    else
                    {
                        card.Body.Add(new AdaptiveColumnSet()
                        {
                            Spacing = AdaptiveSpacing.Small,
                            Columns =
                            {
                                new AdaptiveColumn()
                                {
                                    Width = "auto",
                                    Items =
                                    {
                                        new AdaptiveContainer()
                                        {
                                            Style = AdaptiveContainerStyle.Emphasis,
                                            Items =
                                            {
                                                new AdaptiveTextBlock()
                                                {
                                                    Text = "Proposal needed",
                                                    Size = AdaptiveTextSize.Small,
                                                    Weight = AdaptiveTextWeight.Bolder,
                                                    Color = AdaptiveTextColor.Warning
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        });
                    }
                }
            }

            return card.ToJson();
        }

        private static AdaptiveTextColor GetColorForStatus(SpecStatus status)
        {
            switch (status)
            {
                case SpecStatus.Approved:
                    return AdaptiveTextColor.Good;

                case SpecStatus.Draft:
                    return AdaptiveTextColor.Warning;

                case SpecStatus.HasConcerns:
                    return AdaptiveTextColor.Attention;

                case SpecStatus.ReadyForReview:
                    return AdaptiveTextColor.Accent;

                default:
                    return AdaptiveTextColor.Default;
            }
        }

        public static string GetCardUri()
        {
            return BlobHelper.GetUri("releasescard.json");
        }
    }
}
