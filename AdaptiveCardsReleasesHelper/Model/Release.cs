using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AdaptiveCardsReleasesHelper.Model
{
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
        public List<FeatureRequest> Requests { get; set; }

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
}
