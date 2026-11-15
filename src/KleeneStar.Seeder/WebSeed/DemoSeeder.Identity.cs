using KleeneStar.Model;
using KleeneStar.Model.Entities;
using System;
using System.Linq;

namespace KleeneStar.Seeder
{
    /// <summary>
    /// Seeds the demonstration and test data of KleeneStar.
    /// </summary>
    public static partial class DemoSeeder
    {
        /// <summary>
        /// The password every demo account signs in with: the initial password of the base
        /// administrator (<see cref="KleeneStarDbSeeder.InitialPassword"/>), published with the
        /// source.
        /// </summary>
        public const string DemoPassword = KleeneStarDbSeeder.InitialPassword;

        /// <summary>
        /// The id of the demo account Alice Engineer (tenant Acme Corp, group Engineering).
        /// </summary>
        public static readonly Guid AliceId = Guid.Parse("BBF45E5D-AA35-4382-9B84-6055193CE544");

        /// <summary>
        /// The id of the demo account Marketing User (no tenant, group Marketing).
        /// </summary>
        public static readonly Guid MarketerId = Guid.Parse("1AA3B0E0-5C40-46D8-8ACF-ED12740FD239");

        /// <summary>
        /// The id of the demo account Support User (no tenant, group Support).
        /// </summary>
        public static readonly Guid SupportId = Guid.Parse("D1C5AED2-78D3-45F7-BB19-E87B8F134301");

        /// <summary>
        /// The ids of the demo groups Engineering, Marketing and Support.
        /// </summary>
        private static readonly Guid[] DemoGroupIds =
        [
            Guid.Parse("7420A9F7-F23A-4EC2-91E4-EDDB2E3172BD"),
            Guid.Parse("4D3739DF-EBB0-4113-A40D-EEEBF9B26182"),
            Guid.Parse("7EEB5E9D-87E6-4017-A94A-884F03DF129A")
        ];

        /// <summary>
        /// Adds the demo groups Engineering, Marketing and Support.
        /// </summary>
        /// <param name="db">The database context to which the group entities will be added. Cannot be null.</param>
        private static void SeedGroups(KleeneStarDbContext db)
        {
            void addGroup(Guid id, string name, string description) => db.Groups.Add(new Group(id)
            {
                Name = name,
                Description = description
            });

            addGroup(DemoGroupIds[0], "Engineering", "Software engineering team members.");
            addGroup(DemoGroupIds[1], "Marketing", "Marketing department members.");
            addGroup(DemoGroupIds[2], "Support", "Customer support team members.");
        }

        /// <summary>
        /// Adds the demo accounts with filled-in profiles (display name, bio, contact channels,
        /// regional formats, business data of the tenant) and their group memberships.
        /// </summary>
        /// <remarks>
        /// Every demo account is internal and signs in with <see cref="DemoPassword"/>.
        /// </remarks>
        /// <param name="db">The database context to which the identity entities will be added. Cannot be null.</param>
        private static void SeedIdentities(KleeneStarDbContext db)
        {
            void addIdentity(Identity identity, string tenantName, params string[] groups)
            {
                identity.PasswordHash = IdentityPassword.Hash(identity, DemoPassword);
                identity.PasswordChanged = DateTime.UtcNow;

                identity.Tenant = tenantName is null
                    ? null
                    : db.Tenants.FirstOrDefault(x => x.Name == tenantName);

                identity.GroupMemberships =
                [
                    .. db.Groups
                        .Where(x => groups.Contains(x.Name))
                        .Select(x => new IdentityGroupMembership { Group = x })
                ];

                db.Identities.Add(identity);
            }

            // Alice is an Acme tenant member (typical end-user identity).
            addIdentity(new Identity
            {
                Id = AliceId,
                Name = "Alice Engineer",
                UserName = "alice.engineer",
                Email = "alice.engineer@kleenestar.org",
                EmailVerified = true,
                Bio = "Backend-Entwicklerin · Plattform und Integrationen.",
                PhoneCountry = "+49",
                Phone = "170 9876543",
                Location = "Hamburg, Deutschland",
                Position = "Software Engineer",
                Language = "de",
                DateFormat = "dd.MM.yyyy",
                WeekStart = WeekStart.Monday,
                Role = "Mitglied · Engineering",
                RoleSince = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Department = "Engineering · Platform",
                CostCenter = "CC-2200",
                PersonnelNumber = "A-01137"
            }, "Acme Corp", "Engineering");

            // Marketer stays tenant-less: operator-side account.
            addIdentity(new Identity
            {
                Id = MarketerId,
                Name = "Marketing User",
                UserName = "marketing.user",
                Email = "marketer@kleenestar.org",
                Location = "München, Deutschland",
                Position = "Marketing Manager",
                Language = "de",
                DateFormat = "dd.MM.yyyy",
                WeekStart = WeekStart.Monday,
                Role = "Mitglied · Marketing",
                Department = "Marketing"
            }, null, "Marketing");

            // Support stays tenant-less: operator-side account.
            addIdentity(new Identity
            {
                Id = SupportId,
                Name = "Support User",
                UserName = "support.user",
                Email = "support@kleenestar.org",
                Location = "Wien, Österreich",
                Position = "Service Desk Agent",
                Language = "de",
                DateFormat = "dd.MM.yyyy",
                WeekStart = WeekStart.Monday,
                Role = "Mitglied · Support",
                Department = "Customer Support"
            }, null, "Support");
        }

        /// <summary>
        /// Names Alice Engineer as the deputy of the administrator. Runs after
        /// <see cref="SeedIdentities"/> was saved, because the deputy references a row of the same
        /// table.
        /// </summary>
        /// <param name="db">The database context holding the seeded identities. Cannot be null.</param>
        private static void SeedIdentityDeputies(KleeneStarDbContext db)
        {
            var profile = db.Identities.FirstOrDefault(x => x.Id == KleeneStarDbSeeder.AdministratorId);
            var deputy = db.Identities.FirstOrDefault(x => x.Id == AliceId);

            if (profile is null || deputy is null || profile.DeputyId.HasValue)
            {
                return;
            }

            profile.DeputyId = deputy.Id;
        }
    }
}
