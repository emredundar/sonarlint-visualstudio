using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SonarJsConfig.Config
{
    public class TsConfigLocator
    {
        public IEnumerable<string> Locate(string baseDirectory)
        {
            var files = Directory.GetFiles(baseDirectory, "tsconfig.json", SearchOption.AllDirectories);

            return files.Where(x => !x.Contains("node_modules")).ToArray();
        }
    }
}
