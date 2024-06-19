using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GxPService.Dto
{
    public class ExecutableInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public List<string> OSVersions { get; set; }
        public List<string> Architectures { get; set; }
        public string Hash { get; set; }
        public string SecretCode { get; set; }
    }
}
