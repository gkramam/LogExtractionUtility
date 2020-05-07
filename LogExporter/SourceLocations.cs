using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LogExtractionUtility
{
    public class SourceLocations : IConfigurationSectionHandler
    {
        public object Create(object parent, object configContext, XmlNode section)
        {
            List<SourceLocation> myConfigObject = new List<SourceLocation>();

            foreach (XmlNode childNode in section.ChildNodes)
            {
                string key = childNode.Attributes["key"].Value;
                string value = childNode.Attributes["value"].Value;

                myConfigObject.Add(new SourceLocation() {Key=key,Location=value});
            }
            return myConfigObject;
        }
    }

    public class SourceLocation
    {
        public string Key {get;set;}
        public string Location { get; set; }
    }
}
