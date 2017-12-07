using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlySQL
{
    public static class JSON
    {
        public static dynamic Parse(string source)
        {
            return JsonConvert.DeserializeObject(source);
        }
    }
}
