using NUnit.Framework;
using StructureMap.Configuration;
using StructureMap.Graph;
using StructureMap.Testing.Widget3;

namespace StructureMap.Testing.Configuration
{
    [TestFixture]
    public class GraphBuilderTester
    {
        [Test]
        public void Call_the_action_on_configure_family_if_the_pluginType_is_found()
        {
            var typePath = new TypePath(typeof (IGateway));

            var iWasCalled = false;
            var builder = new GraphBuilder(new PluginGraph());
            builder.ConfigureFamily(typePath, f => {
                Assert.AreEqual(typeof (IGateway), f.PluginType);
                iWasCalled = true;
            });


            Assert.IsTrue(iWasCalled);
        }


    }
}