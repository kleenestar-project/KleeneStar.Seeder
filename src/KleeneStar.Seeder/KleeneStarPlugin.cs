using KleeneStar.Core;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebPlugin;

namespace KleeneStar.Seeder
{
    /// <summary>
    /// Plugin entry point of the demonstration and test data of KleeneStar.
    /// </summary>
    /// <remarks>
    /// The plugin contributes one database seed (<see cref="WebSeed.DemoSeed"/>), which the core
    /// collects as an <c>IDatabaseSeed</c> and runs from <c>KleeneStarApplication.Run</c> after the
    /// base seed. It contributes no page, no endpoint and no manager. It names the core's
    /// application, because the framework refuses a plugin without one. An installation without
    /// this plugin starts with the base data only.
    /// </remarks>
    [Name("kleenestar.seeder:plugin.name")]
    [Description("kleenestar.seeder:plugin.description")]
    [Application<KleeneStarApplication>()]
    [Dependency("webexpress.webapp")]
    [Dependency("kleenestar.core")]
    public sealed class KleeneStarPlugin : IPlugin
    {
        /// <summary>
        /// The plugin id (the namespace of this class, lower-cased).
        /// </summary>
        public const string Id = "kleenestar.seeder";

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public KleeneStarPlugin()
        {
        }

        /// <summary>
        /// Called when the plugin starts working. The seed is run by the core.
        /// </summary>
        public void Run()
        {
        }

        /// <summary>
        /// Called when the host shuts down. The plugin holds no resources of its own.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
