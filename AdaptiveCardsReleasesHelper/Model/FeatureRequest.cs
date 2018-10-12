using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AdaptiveCardsReleasesHelper.Model
{
    public class FeatureRequest : BaseIssue
    {
        /// <summary>
        /// The spec that is solving this feature request
        /// </summary>
        public Spec Spec { get; set; }

        public Proposal[] Proposals { get; set; }
    }
}
