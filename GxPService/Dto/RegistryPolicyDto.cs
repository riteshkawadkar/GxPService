using System;
using System.Collections.Generic;

namespace GxPService.Dto
{
    public class RegistryPolicyDto
    {
        public List<AppliedPolicyDto> AppliedPolicies { get; set; }
        public List<RemovedPolicyDto> RemovedPolicies { get; set; }
        public DateTime ServerTimeStamp { get; set; }
    }

}
