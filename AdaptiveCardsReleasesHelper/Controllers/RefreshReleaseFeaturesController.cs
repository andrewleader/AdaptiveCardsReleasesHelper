using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace AdaptiveCardsReleasesHelper.Controllers
{
    [Produces("application/json")]
    [Route("api/RefreshReleaseFeatures")]
    public class RefreshReleaseFeaturesController : Controller
    {
        [HttpGet]
        public async Task<string> Get()
        {
            GitHubIssue[] allRequests;

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
                client.DefaultRequestHeaders.Add("User-Agent", "andrewleader");

                // Get all requests
                var response = await client.GetAsync("https://api.github.com/repos/microsoft/adaptivecards/issues?labels=Request&state=all");

                string responseStr = await response.Content.ReadAsStringAsync();
                allRequests = JsonConvert.DeserializeObject<GitHubIssue[]>(responseStr);
            }

            using (HttpClient client = new HttpClient())
            {
                string zenhubAuthToken = Startup.ZENHUB_AUTH_TOKEN;
                client.DefaultRequestHeaders.Add("X-Authentication-Token", zenhubAuthToken);

                // Get the releases
                var response = await client.GetAsync("https://api.zenhub.io/p1/repositories/75978731/reports/releases");

                var responseStr = await response.Content.ReadAsStringAsync();
                List<Release> releases = JsonConvert.DeserializeObject<List<Release>>(responseStr);

                // Remove the Backlog release
                releases.RemoveAll(i => i.ReleaseId == "5ab051bdd6ef2a7302b6f293");

                // Sort and reverse, so latest releases are first
                releases.Sort();
                releases.Reverse();

                foreach (var release in releases)
                {
                    await UpdateReleaseAsync(client, allRequests, release);
                }

                return JsonConvert.SerializeObject(releases);
            }
        }

        private static async Task UpdateReleaseAsync(HttpClient client, GitHubIssue[] allRequests, Release release)
        {
            var response = await client.GetAsync($"https://api.zenhub.io/p1/reports/release/{release.ReleaseId}/issues");

            ReleaseIssue[] issues = JsonConvert.DeserializeObject<ReleaseIssue[]>(await response.Content.ReadAsStringAsync());

            release.Requests = new List<Request>();

            foreach (var issue in issues)
            {
                var req = allRequests.FirstOrDefault(i => i.IssueNumber == issue.IssueNumber);
                if (req != null)
                {
                    release.Requests.Add(new Request()
                    {
                        Title = TrimStart(req.Title, "Request: "),
                        IssueNumber = req.IssueNumber
                    });
                }
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

        public class Release : IComparable<Release>
        {
            [JsonProperty(PropertyName = "release_id")]
            public string ReleaseId { get; set; }

            [JsonProperty(PropertyName = "title")]
            public string Title { get; set; }

            /// <summary>
            /// Our code generates this
            /// </summary>
            [JsonProperty(PropertyName = "requests")]
            public List<Request> Requests { get; set; }

            public int CompareTo(Release other)
            {
                if (Version.TryParse(Title, out Version thisVersion))
                {
                    if (Version.TryParse(other.Title, out Version otherVersion))
                    {
                        return thisVersion.CompareTo(otherVersion);
                    }
                }

                return 0;
            }
        }

        public class Request
        {
            [JsonProperty(PropertyName = "title")]
            public string Title { get; set; }

            /// <summary>
            /// The issue number
            /// </summary>
            [JsonProperty(PropertyName = "issue_number")]
            public int IssueNumber { get; set; }
        }

        public class ReleaseIssue
        {
            [JsonProperty(PropertyName = "issue_number")]
            public int IssueNumber { get; set; }
        }

        public class GitHubIssue
        {
            [JsonProperty(PropertyName = "number")]
            public int IssueNumber { get; set; }

            [JsonProperty(PropertyName = "title")]
            public string Title { get; set; }

            [JsonProperty(PropertyName = "labels")]
            public GitHubIssueLabel[] Labels { get; set; }
        }

        public class GitHubIssueLabel
        {
            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }
        }
    }
}