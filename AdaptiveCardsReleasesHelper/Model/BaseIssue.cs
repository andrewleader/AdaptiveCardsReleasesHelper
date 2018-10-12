using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AdaptiveCardsReleasesHelper.Model
{
    public class BaseIssue
    {
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        /// <summary>
        /// The issue number
        /// </summary>
        [JsonProperty(PropertyName = "issue_number")]
        public int IssueNumber { get; set; }
    }
}
