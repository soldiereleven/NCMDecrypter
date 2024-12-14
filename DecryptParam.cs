using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NCMDecrypter
{
    public class DecryptParam
    {
        public string? OutputPath { get; set; }
        public string? OutputName {  get; set; }
        public bool? FixMetadata { get; set; } = true;
    }
}
