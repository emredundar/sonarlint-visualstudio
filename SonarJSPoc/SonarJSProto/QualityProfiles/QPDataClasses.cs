using System.IO;
using System.Xml.Serialization;

namespace SonarJSProto.QualityProfiles
{
    public static class QualityProfileLoader
    {
        public static profile GetProfile(string partialResourceName)
        {
            var serializer = new XmlSerializer(typeof(profile));

            var resourceName = "SonarJSProto.Resources.ExportedQPs." + partialResourceName;

            using (var reader = new StreamReader(typeof(QualityProfileLoader).Assembly.GetManifestResourceStream(resourceName)))
            {
                return serializer.Deserialize(reader) as profile;
            }
        }
    }

    public class profile
    {
        public string language;
        public string name;
        public rule[] rules;
    }

      //<repositoryKey>javascript</repositoryKey>
      //<key>S101</key>
      //<priority>MINOR</priority>
      //<parameters>
      //  <parameter>
      //    <key>format</key>
      //    <value>^[A-Z] [a-zA-Z0-9]*$</value>
      //  </parameter>
      //</parameters>

    public class rule
    {
        public string repositoryKey;
        public string key;
        public string priority;
        public parameter[] parameters;
    }

    public class parameter
    {
        public string key;
        public string value;
    }
}
