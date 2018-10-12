using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AdaptiveCardsReleasesHelper.Model
{
    public class Spec : BaseIssue
    {
        public SpecStatus SpecStatus { get; set; }
    }
}
