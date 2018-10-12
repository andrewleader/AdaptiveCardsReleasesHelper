using AdaptiveCardsReleasesHelper.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AdaptiveCardsReleasesHelper.Helpers
{
    public static class ReleaseFeaturesHelper
    {
        public static Task<List<Release>> GetReleasesAsync(bool refresh = false)
        {
            return BlobHelper.GetCachedOrRefresh("releases.json", ActuallyGetReleasesAsync, cacheDurationInMinutes: refresh ? 0 : 10);
        }

        private static async Task<List<Release>> ActuallyGetReleasesAsync()
        {
            GitHubIssue[] allRequests;
            GitHubIssue[] allProposals;
            GitHubIssue[] allSpecs;

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
                client.DefaultRequestHeaders.Add("User-Agent", "andrewleader");

                // Get all requests
                allRequests = await GetObjectAsync<GitHubIssue[]>(client, "https://api.github.com/repos/microsoft/adaptivecards/issues?labels=Request&state=all");
                allProposals = await GetObjectAsync<GitHubIssue[]>(client, "https://api.github.com/repos/microsoft/adaptivecards/issues?labels=Proposal&state=all");
                allSpecs = await GetObjectAsync<GitHubIssue[]>(client, "https://api.github.com/repos/microsoft/adaptivecards/issues?labels=Spec&state=all");
            }

            using (HttpClient client = new HttpClient())
            {
                string zenhubAuthToken = Startup.ZENHUB_AUTH_TOKEN;
                client.DefaultRequestHeaders.Add("X-Authentication-Token", zenhubAuthToken);

                // Get the sorting of everything
                var zenHubBoard = await GetObjectAsync<ZenHubBoard>(client, "https://api.zenhub.io/p1/repositories/75978731/board");

                // Get dependencies
                //var zenHubDependencies = await GetObjectAsync<ZenHubDependencies>(client, "https://api.zenhub.io/p1/repositories/75978731/dependencies");

                // Get the releases
                List<Release> releases = await GetObjectAsync<List<Release>>(client, "https://api.zenhub.io/p1/repositories/75978731/reports/releases");

                // Remove the Backlog release
                releases.RemoveAll(i => i.ReleaseId == "5ab051bdd6ef2a7302b6f293");



                // Sort and reverse, so latest releases are first
                releases.Sort();
                releases.Reverse();



                foreach (var release in releases)
                {
                    await UpdateReleaseAsync(client, allRequests, allSpecs, allProposals, release);
                }

                return releases;
            }
        }

        private static async Task<T> GetObjectAsync<T>(HttpClient client, string url)
        {
            var response = await client.GetAsync(url);

            var responseStr = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseStr);
        }

        private static async Task UpdateReleaseAsync(HttpClient client, GitHubIssue[] allRequests, GitHubIssue[] allSpecs, GitHubIssue[] allProposals, Release release)
        {
            var response = await client.GetAsync($"https://api.zenhub.io/p1/reports/release/{release.ReleaseId}/issues");

            ZenHubIssue[] issues = JsonConvert.DeserializeObject<ZenHubIssue[]>(await response.Content.ReadAsStringAsync());

            release.Requests = new List<FeatureRequest>();

            foreach (var issue in issues)
            {
                var req = allRequests.FirstOrDefault(i => i.IssueNumber == issue.IssueNumber);
                if (req != null)
                {
                    var featureRequest = new FeatureRequest()
                    {
                        Title = TrimStart(req.Title, "Request: "),
                        IssueNumber = req.IssueNumber
                    };

                    //var dependency = dependencies.Dependencies.FirstOrDefault(i => i.Blocked.IssueNumber == featureRequest.IssueNumber);
                    //if (dependency != null)
                    //{
                        //var specIssue = allSpecs.FirstOrDefault(i => i.IssueNumber == dependency.Blocking.IssueNumber);
                        var specIssue = allSpecs.FirstOrDefault(i => DoesIssueReference(i, featureRequest.IssueNumber));
                        if (specIssue != null)
                        {
                            featureRequest.Spec = new Spec()
                            {
                                Title = TrimStart(TrimStart(specIssue.Title, "Spec: "), "Spec draft: "),
                                IssueNumber = specIssue.IssueNumber
                            };

                            if (specIssue.Labels.Any(i => i.Name == "Spec-Approved"))
                            {
                                featureRequest.Spec.SpecStatus = SpecStatus.Approved;
                            }
                            else if (specIssue.Labels.Any(i => i.Name == "Spec-Ready for Review"))
                            {
                                featureRequest.Spec.SpecStatus = SpecStatus.ReadyForReview;
                            }
                            else if (specIssue.Labels.Any(i => i.Name == "Spec-Has Concerns"))
                            {
                                featureRequest.Spec.SpecStatus = SpecStatus.HasConcerns;
                            }
                            else
                            {
                                featureRequest.Spec.SpecStatus = SpecStatus.Draft;
                            }
                        }
                    //}

                    // If there's no spec, then look for proposals
                    if (featureRequest.Spec == null)
                    {
                        featureRequest.Proposals = allProposals.Where(i => DoesIssueReference(i, featureRequest.IssueNumber)).Select(i => new Proposal()
                        {
                            Title = TrimStart(i.Title, "Proposal: "),
                            IssueNumber = i.IssueNumber,
                            SpecStatus = GetSpecStatusFromProposal(i)
                        }).ToArray();
                    }

                    release.Requests.Add(featureRequest);
                }
            }
        }

        private static bool DoesIssueReference(GitHubIssue issue, int issueNumber)
        {
            return issue.Body.Contains($"#{issueNumber}");
        }

        private static SpecStatus GetSpecStatusFromProposal(GitHubIssue issue)
        {
            if (issue.Labels.Any(i => i.Name == "Proposal-Ready for Review"))
            {
                return SpecStatus.ReadyForReview;
            }
            else if (issue.Labels.Any(i => i.Name == "Proposal-Has Concerns"))
            {
                return SpecStatus.HasConcerns;
            }
            else
            {
                return SpecStatus.Draft;
            }
        }

        private static string TrimStart(string str, string toTrim)
        {
            if (str.StartsWith(toTrim, StringComparison.CurrentCultureIgnoreCase))
            {
                return str.Substring(toTrim.Length);
            }

            return str;
        }

        public class ZenHubIssue
        {
            [JsonProperty(PropertyName = "issue_number")]
            public int IssueNumber { get; set; }

            public long Position { get; set; }
        }

        public class GitHubIssue
        {
            [JsonProperty(PropertyName = "number")]
            public int IssueNumber { get; set; }

            [JsonProperty(PropertyName = "title")]
            public string Title { get; set; }

            public string Body { get; set; }

            [JsonProperty(PropertyName = "labels")]
            public GitHubIssueLabel[] Labels { get; set; }
        }

        public class GitHubIssueLabel
        {
            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }
        }

        public class ZenHubBoard
        {
            [JsonProperty(PropertyName = "pipelines")]
            public ZenHubPipeline[] Pipelines { get; set; }
        }

        public class ZenHubPipeline
        {
            public string Id { get; set; }

            public string Name { get; set; }

            public ZenHubIssue[] Issues { get; set; }
        }

        public class ZenHubDependencies
        {
            public ZenHubDependency[] Dependencies { get; set; }
        }

        public class ZenHubDependency
        {
            public ZenHubIssue Blocking { get; set; }

            public ZenHubIssue Blocked { get; set; }
        }
    }
}
