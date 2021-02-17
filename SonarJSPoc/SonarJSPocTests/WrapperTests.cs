using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarJsConfig;

namespace SonarJSPocTests
{
    [TestClass]
    public class WrapperTests
    {
        [TestMethod]
        public async Task Wip()
        {
            var wrapper = new EslintBridgeWrapper(new ConsoleLogger());

            await wrapper.Start();


            

            await wrapper.Stop();
        
        }
    }
}
