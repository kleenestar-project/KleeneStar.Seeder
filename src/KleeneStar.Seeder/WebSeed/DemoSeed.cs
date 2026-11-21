using KleeneStar.Core.WebSeed;
using KleeneStar.Model;
using System.Threading.Tasks;

namespace KleeneStar.Seeder.WebSeed
{
    /// <summary>
    /// The demo data as a database seed of the core (<see cref="IDatabaseSeed"/>), delegating to
    /// <see cref="DemoSeeder.SeedAsync"/>.
    /// </summary>
    public sealed class DemoSeed : IDatabaseSeed
    {
        /// <inheritdoc/>
        public string Key => KleeneStarPlugin.Id;

        /// <inheritdoc/>
        public int Order => 100;

        /// <inheritdoc/>
        public Task SeedAsync(KleeneStarDbContext db)
        {
            return DemoSeeder.SeedAsync(db);
        }
    }
}
