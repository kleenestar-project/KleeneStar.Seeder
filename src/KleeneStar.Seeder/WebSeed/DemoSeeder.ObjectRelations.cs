using KleeneStar.Model;
using KleeneStar.Model.Entities;
using System;
using System.Linq;
using WebExpress.WebApp.WebRelation;

namespace KleeneStar.Seeder
{
    /// <summary>
    /// Seeds the demonstration and test data of KleeneStar.
    /// </summary>
    public static partial class DemoSeeder
    {
        /// <summary>
        /// Adds a small set of demonstration relations so the link surface of a fresh installation
        /// shows what it is for instead of an empty state.
        /// </summary>
        /// <remarks>
        /// The ends are resolved from whatever objects the object seeder produced rather than
        /// named by key, because the keys are derived from the workspace and the running count
        /// and would go stale the moment that seeder changes. Objects of one workspace are
        /// paired, which keeps every seeded relation inside one permission scope.
        /// </remarks>
        /// <param name="db">The database context to which the relations will be added. Cannot be null.</param>
        private static void SeedObjectRelations(KleeneStarDbContext db)
        {
            var relations = new[]
            {
                (Type: RelationType.Blocks, Comment: "Same gateway - the change has to land first."),
                (Type: RelationType.Causes, Comment: "The regression appeared with this rollout."),
                (Type: RelationType.References, Comment: "Background for the decision."),
                (Type: RelationType.Similar, Comment: "Reported twice within the same hour."),
                (Type: RelationType.Duplicate, Comment: "Same symptom, same component.")
            };

            var author = db.Identities.OrderBy(x => x.RawId).FirstOrDefault();
            var seeded = 0;

            foreach (var workspace in db.Workspaces.OrderBy(x => x.RawId).Take(3).ToList())
            {
                var objects = db.Objects
                    .Where(x => x.WorkspaceId == workspace.Id)
                    .OrderBy(x => x.RawId)
                    .Take(8)
                    .ToList();

                // a relation needs two ends, so a workspace the object seeder left thin is
                // simply skipped rather than relationed against itself
                for (var i = 0; i + 1 < objects.Count && i < relations.Length * 2; i += 2)
                {
                    var relation = relations[(i / 2) % relations.Length];

                    db.ObjectRelations.Add(new ObjectRelation
                    {
                        Id = Guid.NewGuid(),
                        System = RelationSystem.Object,
                        TypeKey = relation.Type,
                        Direction = RelationDirection.Bidirectional,
                        Status = RelationStatus.Active,
                        SourceObjectId = objects[i].Id,
                        TargetObjectId = objects[i + 1].Id,
                        Comment = relation.Comment,
                        CreatedById = author?.Id,
                        Created = DateTime.UtcNow,
                        Updated = DateTime.UtcNow
                    });

                    seeded++;
                }

                // a chain, so the impact analysis has something to walk: the pairs above are one
                // hop each and read the same at every depth, while what the analysis is for is
                // the consequence that arrives through an object nobody related to the first.
                // The first blocks the second, the second blocks the third, and the fourth is a
                // duplicate of the third - three steps out and two effects, which is what the
                // depth chooser on the impact page is worth having for
                if (objects.Count >= 4)
                {
                    void chain(string type, Model.Entities.Object source, Model.Entities.Object target, string comment) => db.ObjectRelations.Add(new ObjectRelation
                    {
                        Id = Guid.NewGuid(),
                        System = RelationSystem.Object,
                        TypeKey = type,
                        Direction = RelationDirection.Bidirectional,
                        Status = RelationStatus.Active,
                        SourceObjectId = source.Id,
                        TargetObjectId = target.Id,
                        Comment = comment,
                        CreatedById = author?.Id,
                        Created = DateTime.UtcNow,
                        Updated = DateTime.UtcNow
                    });

                    chain(RelationType.Blocks, objects[0], objects[2], "Nothing downstream moves before this is done.");
                    chain(RelationType.Blocks, objects[2], objects[4 % objects.Count], "Waits on the step before it.");
                    chain(RelationType.Duplicate, objects[3], objects[4 % objects.Count], "Reported a second time - settled with the original.");

                    seeded += 3;

                    // ...and one object that reports the progress of others, so the rollup has
                    // something to roll up: without a single aggregating relation the progress
                    // card never appears and the feature reads as missing rather than as unused
                    if (objects.Count >= 7)
                    {
                        chain(RelationType.Parent, objects[1], objects[5], "Part of this piece of work.");
                        chain(RelationType.Parent, objects[1], objects[6], "Part of this piece of work.");

                        seeded += 2;
                    }
                }

                // one external relation per workspace, so both categories of the hybrid model are
                // visible on a fresh installation
                if (objects.Count > 0)
                {
                    db.ObjectRelations.Add(new ObjectRelation
                    {
                        Id = Guid.NewGuid(),
                        System = RelationSystem.Web,
                        TypeKey = RelationType.WebLink,
                        Direction = RelationDirection.Unidirectional,
                        Status = RelationStatus.Active,
                        SourceObjectId = objects[0].Id,
                        TargetUri = "https://github.com/kleenestar-project",
                        TargetTitle = "KleeneStar on GitHub",
                        Comment = "Project home.",
                        CreatedById = author?.Id,
                        Created = DateTime.UtcNow,
                        Updated = DateTime.UtcNow
                    });

                    seeded++;
                }
            }

            if (seeded == 0)
            {
                Console.WriteLine("No objects available - object relations were not seeded.");
            }
        }
    }
}
