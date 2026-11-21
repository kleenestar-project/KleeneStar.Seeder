using KleeneStar.Model;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace KleeneStar.Seeder
{
    /// <summary>
    /// Seeds the demonstration and test data of KleeneStar: tenants, workspace categories, demo
    /// groups and accounts, seven workspaces with their classes, fields, forms, workflows,
    /// priorities, security levels, templates, calendars and SLA policies, and the objects with
    /// values, history, labels, shares, comments, attachments, relations, sprints, views,
    /// insights, saved searches, bookmarks, pinned landing documents and workspace home pages.
    /// The help pages of KleeneStar are not demo data; they are seeded by the plugin
    /// <c>KleeneStar.Help</c>.
    /// </summary>
    /// <remarks>
    /// Runs after the base seed of the model (<see cref="KleeneStarDbSeeder.SeedAsync"/>), which
    /// provides the administrators group, the administrator account, the status categories and
    /// the relation catalog this data references. Each part is guarded by a check on the store.
    /// </remarks>
    public static partial class DemoSeeder
    {
        /// <summary>
        /// The ids of the seven demo workspaces (<see cref="SeedWorkspaces"/>).
        /// </summary>
        public static readonly Guid[] DemoWorkspaceIds =
        [
            Guid.Parse("D651799A-690E-4CFF-AE0C-D3341CA3BBB4"),
            Guid.Parse("660E9B11-2D54-4A36-84F9-F3BF5C78B748"),
            Guid.Parse("9994445E-FDBE-42E2-A3A0-65DF13CB453B"),
            Guid.Parse("8C8F10A2-4A98-4A8C-B0C4-12359CAC3C5F"),
            Guid.Parse("25F04599-C02A-4BEF-80B1-5957FBBB1FED"),
            Guid.Parse("D35FDCD6-5B11-4043-98D6-215DF414D99C"),
            Guid.Parse("F027A791-4219-4B1D-BA7C-2E7757091AAA")
        ];

        /// <summary>
        /// Returns whether the demo data applies to the store: it carries no workspace, or at
        /// least one demo workspace (<see cref="DemoWorkspaceIds"/>).
        /// </summary>
        /// <remarks>
        /// A store whose workspaces are all its own is a productive installation; the demo accounts
        /// sign in with a published password and are not added to it.
        /// </remarks>
        /// <param name="db">The database context. Cannot be null.</param>
        /// <returns><see langword="true"/> when the demo data is seeded into the store.</returns>
        public static bool AppliesTo(KleeneStarDbContext db)
        {
            ArgumentNullException.ThrowIfNull(db);

            return !db.Workspaces.Any() || db.Workspaces.Any(x => DemoWorkspaceIds.Contains(x.Id));
        }

        /// <summary>
        /// Adds every part of the demo data the store does not carry yet, provided
        /// <see cref="AppliesTo"/> holds. A run that seeds the workspaces appends an
        /// <c>Seeded</c> event to the audit log (<see cref="KleeneStarDbSeeder.RecordSeed"/>).
        /// </summary>
        /// <param name="db">The database context, migrated and holding the base data. Cannot be null.</param>
        /// <returns><see langword="true"/> when the demo data applies to the store.</returns>
        public static async Task<bool> SeedAsync(KleeneStarDbContext db)
        {
            ArgumentNullException.ThrowIfNull(db);

            if (!AppliesTo(db))
            {
                return false;
            }

            var fresh = !db.Workspaces.Any();

            if (!db.Categories.Any())
            {
                SeedCategories(db);
                await db.SaveChangesAsync();
            }

            if (!db.Tenants.Any())
            {
                SeedTenants(db);
                await db.SaveChangesAsync();
            }

            if (!db.Groups.Any(x => DemoGroupIds.Contains(x.Id)))
            {
                SeedGroups(db);
                await db.SaveChangesAsync();
            }

            if (!db.Identities.Any(x => x.Id == AliceId || x.Id == MarketerId || x.Id == SupportId))
            {
                SeedIdentities(db);
                await db.SaveChangesAsync();

                // the deputy references another row of the table written above
                SeedIdentityDeputies(db);
                await db.SaveChangesAsync();
            }

            if (fresh)
            {
                SeedWorkspaces(db);
                await db.SaveChangesAsync();

                // the demo workspaces start administered like a workspace created from a
                // template; only in the run that seeds them
                SeedWorkspacePermissions(db);
                await db.SaveChangesAsync();
            }

            await Step(db, "classes", !db.Classes.Any(), SeedClasses);
            await Step(db, "fields", !db.Fields.Any(), SeedFields);
            await Step(db, "forms", !db.Forms.Any(), SeedForms);

            // a level belongs to a class and names the groups cleared for it
            await Step(db, "security levels", !db.SecurityLevels.Any(), SeedSecurityLevels);

            // a template is bound to the class its objects are created from
            await Step(db, "templates", !db.Templates.Any(), SeedTemplates);
            await Step(db, "priorities", !db.Priorities.Any(), SeedPriorities);
            await Step(db, "statuses", !db.Statuses.Any(), SeedStatuses);
            await Step(db, "workflows", !db.Workflows.Any(), SeedWorkflows);
            await Step(db, "objects", !db.Objects.Any(), SeedObjects);

            // needs objects and levels; skipped once any object carries a classification, so a
            // record somebody declassified stays declassified
            await Step(db, "object security levels", !db.Objects.Any(x => x.SecurityLevelId != null), SeedObjectSecurityLevels);
            await Step(db, "values", !db.Values.Any(), SeedValues);

            // the commit chains are built backwards from the values
            await Step(db, "commits", !db.Commits.Any(), SeedCommits);

            // every start: dates an earlier seed wrote as text, in the values and their history
            await Step(db, "date values", true, RepairDateValues);
            await Step(db, "tags", !db.ObjectTags.Any(), SeedTags);
            await Step(db, "shares and watchers", !db.ObjectShares.Any() && !db.ObjectWatchers.Any(), SeedSharesAndWatchers);

            // every start: the help pages moved into the plugin KleeneStar.Help, the copies an
            // earlier seed wrote into the service desk leave the landing page; pinned documents
            await Step(db, "retired landing pages", true, RetireLandingPages);
            await Step(db, "landing labels", true, SeedLandingLabels);

            await Step(db, "insights", !db.Insights.Any(), SeedInsights);
            await Step(db, "object views", !db.ObjectViews.Any(), SeedObjectViews);
            await Step(db, "sprints", !db.Sprints.Any(), SeedSprints);
            await Step(db, "saved searches", !db.SavedSearches.Any(), SeedSavedSearches);
            await Step(db, "workspace bookmarks", !db.WorkspaceBookmarks.Any(), SeedWorkspaceBookmarks);

            // the SLA policies resolve their calendar by name
            await Step(db, "calendars", !db.Calendars.Any(), SeedCalendars);
            await Step(db, "SLA policies", !db.SlaPolicies.Any(), SeedSlas);
            await Step(db, "comments", !db.Comments.Any(), SeedComments);
            await Step(db, "attachments", !db.Attachments.Any(), SeedAttachments);
            await Step(db, "object relations", !db.ObjectRelations.Any(), SeedObjectRelations);

            // every start and last, so no comment, label or share of the steps above lands on a
            // home page
            await Step(db, "workspace home pages", true, SeedWorkspaceHomes);

            if (fresh)
            {
                KleeneStarDbSeeder.RecordSeed(db, KleeneStarPlugin.Id);
            }

            return true;
        }

        /// <summary>
        /// Runs one seeding step when its guard holds and saves its changes.
        /// </summary>
        /// <param name="db">The database context.</param>
        /// <param name="name">The name of the step, for the error message.</param>
        /// <param name="due">Whether the step runs.</param>
        /// <param name="seed">The step.</param>
        /// <returns>A task representing the step.</returns>
        /// <exception cref="InvalidOperationException">The step failed; the inner exception names the cause.</exception>
        private static async Task Step(KleeneStarDbContext db, string name, bool due, Action<KleeneStarDbContext> seed)
        {
            if (!due)
            {
                return;
            }

            try
            {
                seed(db);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Seeding the demo {name} failed: {ex.InnerException?.Message ?? ex.Message}", ex);
            }
        }
    }
}
