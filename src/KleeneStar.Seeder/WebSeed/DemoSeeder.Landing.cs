using KleeneStar.Model;
using KleeneStar.Model.Entities;
using Microsoft.EntityFrameworkCore;
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
        /// The label that pins an object to the landing page.
        /// </summary>
        /// <remarks>
        /// The landing page reads its labels through
        /// <c>KleeneStar.Core.WebFragment.Landing.LandingLabel</c>. The string is repeated here
        /// because a label is data, matched without case.
        /// </remarks>
        private const string LandingLabelPinned = "Pinned";

        /// <summary>
        /// The reserved help labels the retired landing pages carried.
        /// </summary>
        private static readonly string[] RetiredLandingLabels = ["Help", "FAQ", "First Steps"];

        /// <summary>
        /// The help pages this seed wrote into the service desk before the help moved into the
        /// plugin <c>KleeneStar.Help</c>, as (key, summary).
        /// </summary>
        /// <remarks>
        /// The help of KleeneStar is product content and lives in the workspace <c>HELP</c> of
        /// the help plugin. A store seeded earlier still carries these pages as knowledge
        /// documents of the service desk; <see cref="RetireLandingPages"/> takes them off the
        /// landing page so the help is not listed twice.
        /// </remarks>
        private static readonly (string Key, string Summary)[] RetiredLandingPages =
        [
            ("SD-9000", "Create an issue"),
            ("SD-9001", "Work with templates"),
            ("SD-9002", "Assign and hand over an issue"),
            ("SD-9003", "Filter and save a view"),
            ("SD-9004", "Comments and attachments"),
            ("SD-9005", "Who can see my issue?"),
            ("SD-9006", "What happens when a deadline passes?"),
            ("SD-9007", "Can I replace this start page?"),
            ("SD-9008", "How do I find an old issue?"),
            ("SD-9009", "Your first issue in three steps"),
            ("SD-9010", "Understand templates"),
            ("SD-9011", "The basic functions at a glance"),
            ("SD-9012", "Class, view, workspace")
        ];

        /// <summary>
        /// Retires the help pages an earlier version of this seed wrote into the service desk:
        /// their help labels are removed and the pages are archived.
        /// </summary>
        /// <remarks>
        /// A page is retired only while it still carries one of the help labels and is still
        /// titled as seeded, so a page somebody rewrote, relabelled or restored is left alone.
        /// The pages are archived rather than deleted, because their history and comments stay
        /// readable that way. The step runs on every start and does nothing on a store seeded
        /// after the move.
        /// </remarks>
        /// <param name="db">The database context. Cannot be null.</param>
        private static void RetireLandingPages(KleeneStarDbContext db)
        {
            var keys = RetiredLandingPages.Select(x => x.Key).ToList();
            var summaries = RetiredLandingPages.ToDictionary(x => x.Key, x => x.Summary);

            var candidates = db.Objects
                .Where(x => keys.Contains(x.Key) && x.WorkspaceId == ServiceDeskWorkspaceId)
                .ToList()
                .Where(x => summaries[x.Key] == x.Summary)
                .ToList();

            if (candidates.Count == 0)
            {
                return;
            }

            var ids = candidates.Select(x => x.Id).ToList();
            var labels = db.ObjectTags
                .Where(x => ids.Contains(x.ObjectId))
                .ToList()
                .Where(x => RetiredLandingLabels.Contains(x.Name, StringComparer.OrdinalIgnoreCase))
                .ToList();

            var labelled = labels.Select(x => x.ObjectId).ToHashSet();

            db.ObjectTags.RemoveRange(labels);

            foreach (var page in candidates.Where(x => labelled.Contains(x.Id) && x.State == WorkspaceState.Active))
            {
                page.State = WorkspaceState.Archived;
                page.Updated = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// The id of the service desk workspace.
        /// </summary>
        private static readonly Guid ServiceDeskWorkspaceId = Guid.Parse("F027A791-4219-4B1D-BA7C-2E7757091AAA");

        /// <summary>
        /// Pins a small set of documents to the landing page.
        /// </summary>
        /// <remarks>
        /// The pinned set is picked by summary rather than by id: those documents are generated
        /// by <c>SeedObjects</c>, so their ids are not known here. A document that is not present
        /// is simply not pinned. The help pages and their labels are seeded by the plugin
        /// <c>KleeneStar.Help</c>.
        /// </remarks>
        /// <param name="db">The database context to which the labels are added. Cannot be null.</param>
        private static void SeedLandingLabels(KleeneStarDbContext db)
        {
            // the documents an organization would keep in reach: the big picture, the manual,
            // and the way in for a new colleague
            var pinnedSummaries = new[] { "Architecture Diagram", "User Manual", "Setup Guide" };

            var pinned = db.Objects
                .AsNoTracking()
                .Where(x => x.Kind == ObjectKind.Document && pinnedSummaries.Contains(x.Summary))
                .Select(x => x.Id)
                .ToList();

            var tagged = db.ObjectTags
                .AsNoTracking()
                .Where(x => pinned.Contains(x.ObjectId))
                .Select(x => new { x.ObjectId, x.Name })
                .ToList()
                .Where(x => string.Equals(x.Name, LandingLabelPinned, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.ObjectId)
                .ToHashSet();

            foreach (var objectId in pinned)
            {
                // the composite unique index on (ObjectId, Name) makes a second attempt fatal
                if (tagged.Contains(objectId))
                {
                    continue;
                }

                db.ObjectTags.Add(new ObjectTag
                {
                    ObjectId = objectId,
                    Name = LandingLabelPinned,
                    Color = "#fd7e14",
                    Created = DateTime.UtcNow
                });
            }
        }
    }
}
