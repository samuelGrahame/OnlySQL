using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace OnlySQL
{
    public static class XML
    {
        public static dynamic Parse(string source)
        {
            var x = new XmlDocument();
            x.LoadXml(source);
            return JsonConvert.DeserializeObject(JsonConvert.SerializeXmlNode(x));            
        }
    }
}
