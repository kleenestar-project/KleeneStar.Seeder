using KleeneStar.Core.WebSeed;
using KleeneStar.Model;
using KleeneStar.Model.Entities;
using KleeneStar.Model.Integrity;
using KleeneStar.Seeder.WebSeed;
using Microsoft.EntityFrameworkCore;
using WebExpress.WebCore.WebPlugin;

namespace KleeneStar.Seeder.Test
{
    /// <summary>
    /// Contains unit tests for the plugin side of the demo data: discovery of
    /// <see cref="DemoSeed"/>, the store check <see cref="DemoSeeder.AppliesTo"/> and the audit
    /// event of a demo run.
    /// </summary>
    [Collection("NonParallelTests")]
    public class UnitTestDemoSeed
    {
        /// <summary>
        /// Verifies that the core's catalog of database seeds finds <see cref="DemoSeed"/> in the
        /// plugin assembly under the plugin id.
        /// </summary>
        [Fact]
        public void DemoSeedIsDiscovered()
        {
            using var catalog = new PluginTypeCatalog<IDatabaseSeed>(x => x.Key);

            catalog.Collect(typeof(DemoSeed).Assembly);

            var seed = Assert.Single(catalog.Items);

            Assert.IsType<DemoSeed>(seed);
            Assert.Equal(KleeneStarPlugin.Id, seed.Key);
        }

        /// <summary>
        /// Verifies that <see cref="DemoSeeder.DemoWorkspaceIds"/> names exactly the workspaces
        /// the demo seed writes.
        /// </summary>
        [Fact]
        public async Task DemoWorkspaceIdsNameTheSeededWorkspaces()
        {
            await using var db = InMemoryDbContextFactory.Create($"DemoWorkspaceIds_{Guid.NewGuid()}");

            await KleeneStarDbSeeder.SeedAsync(db);
            await DemoSeeder.SeedAsync(db);

            Assert.Equal
            (
                DemoSeeder.DemoWorkspaceIds.OrderBy(x => x),
                db.Workspaces.Select(x => x.Id).ToList().OrderBy(x => x)
            );
        }

        /// <summary>
        /// Verifies that the demo seed leaves a store alone whose workspaces are all its own: no
        /// demo account, tenant, workspace or object is added.
        /// </summary>
        [Fact]
        public async Task DemoSeedSkipsAProductiveStore()
        {
            await using var db = InMemoryDbContextFactory.Create($"DemoSeedSkipsProductive_{Guid.NewGuid()}");

            await KleeneStarDbSeeder.SeedAsync(db);

            db.Workspaces.Add(new Workspace(Guid.NewGuid()) { Key = "OWN", Name = "Own" });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var identities = db.Identities.Count();
            var groups = db.Groups.Count();

            Assert.False(DemoSeeder.AppliesTo(db));
            Assert.False(await DemoSeeder.SeedAsync(db));

            Assert.Equal(identities, db.Identities.Count());
            Assert.Equal(groups, db.Groups.Count());
            Assert.Empty(db.Tenants);
            Assert.Single(db.Workspaces);
            Assert.Empty(db.Objects);
        }

        /// <summary>
        /// Verifies that the run seeding the demo workspaces appends one <c>Seeded</c> event
        /// sealed onto the genesis event of the base seed, and that a later run appends none.
        /// </summary>
        [Fact]
        public async Task DemoSeedAppendsOneAuditEvent()
        {
            var name = $"DemoSeedAudit_{Guid.NewGuid()}";

            await using (var db = InMemoryDbContextFactory.Create(name))
            {
                await KleeneStarDbSeeder.SeedAsync(db);
                await DemoSeeder.SeedAsync(db);
            }

            await using (var db = InMemoryDbContextFactory.Create(name))
            {
                await KleeneStarDbSeeder.SeedAsync(db);
                await DemoSeeder.SeedAsync(db);
            }

            await using (var db = InMemoryDbContextFactory.Create(name))
            {
                var events = db.AuditEvents.Include(x => x.Deltas).OrderBy(x => x.Sequence).ToList();

                Assert.Equal(2, events.Count);
                Assert.Equal([1L, 2L], events.Select(x => x.Sequence));
                Assert.Equal(KleeneStarDbSeeder.AuditAgent, events[0].Agent);
                Assert.Equal(KleeneStarPlugin.Id, events[1].Agent);
                Assert.Equal(AuditAction.Seeded, events[1].Action);
                Assert.Equal(events[0].Hash, events[1].PreviousHash);
                Assert.True(AuditSeal.Verify(events[1], events[0].Hash));

                var objects = events[1].Deltas.Single(x => x.Attribute == "objects");

                Assert.Equal(db.Objects.Count().ToString(System.Globalization.CultureInfo.InvariantCulture), objects.NewValue);
            }
        }

        /// <summary>
        /// Verifies that the demo seed keeps the base administrator as the base seed wrote it and
        /// names Alice Engineer as its deputy.
        /// </summary>
        [Fact]
        public async Task DemoSeedKeepsTheBaseAdministrator()
        {
            await using var db = InMemoryDbContextFactory.Create($"DemoSeedAdministrator_{Guid.NewGuid()}");

            await KleeneStarDbSeeder.SeedAsync(db);

            var before = db.Identities.AsNoTracking().Single(x => x.Id == KleeneStarDbSeeder.AdministratorId);

            await DemoSeeder.SeedAsync(db);

            var after = db.Identities.AsNoTracking().Single(x => x.Id == KleeneStarDbSeeder.AdministratorId);

            Assert.Equal(before.Name, after.Name);
            Assert.Equal(before.Email, after.Email);
            Assert.Equal(before.PasswordHash, after.PasswordHash);
            Assert.Equal(DemoSeeder.AliceId, after.DeputyId);
        }
    }
}
