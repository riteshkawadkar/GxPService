using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GxPService.Dto
{
    public class MachineDto
    {
        public string HostName { get; set; }
        public string OSName { get; set; }
        public string OSVersion { get; set; }
        public string OSManufacturer { get; set; }
        public string OSConfiguration { get; set; }
        public string OSBuildType { get; set; }
        public string ProductId { get; set; }
    }
}
