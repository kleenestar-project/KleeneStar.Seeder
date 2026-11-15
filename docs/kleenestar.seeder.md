# KleeneStar.Seeder

The plugin `kleenestar.seeder` writes the demonstration and test data of KleeneStar into the
database. The base data every database needs is written by the model (`KleeneStarDbSeeder`); this
plugin adds everything that exists only to demonstrate or test the product.

## Base data and demo data

|Written by                                          |Data
|----------------------------------------------------|-------------------------------------------------------------------
|`KleeneStar.Model` (`KleeneStarDbSeeder.SeedAsync`) |maintenance record, branding record, administrators group (`Group.AdministratorsId`), built-in groups (`Group.AuthenticatedId`, `Group.AnonymousId`), administrator account (`KleeneStarDbSeeder.AdministratorId`, user `admin`, password `KleeneStarDbSeeder.InitialPassword`), password placeholder upgrade, status categories (ToDo, InProgress, Waiting, Done), relation catalog (eight `ObjectRelationType` rows), audit genesis event |
|`KleeneStar.Seeder` (`DemoSeeder.SeedAsync`)        |workspace categories, tenants, demo groups (Engineering, Marketing, Support), demo accounts, deputy of the administrator, seven workspaces (`DemoSeeder.DemoWorkspaceIds`) with default grants, classes, fields, forms, security levels, templates, priorities, statuses, workflows, objects, object classifications, values, commits, labels, shares and watchers, landing pages and labels, insights, object views, sprints, saved searches, workspace bookmarks, calendars, SLA policies, comments, attachments, object relations, workspace home pages, audit event of the demo run 

## Execution

1. The constructor of `KleeneStarApplication` calls `DatabaseStartup.Run`: migration, then the
   base seed, under the cluster lock `kleenestar.core.database` and the EF database lock.
2. `KleeneStarApplication.Run` calls `DatabaseStartup.RunSeeds` with
   `CoreHub.DatabaseSeedManager.Seeds` under the same two locks. The constructor cannot run plugin
   seeds: it is called while the core plugin is registered, before the other plugins are.
3. The core then runs the hierarchy verifier, rebuilds the path index (`ObjectPath`) and checks the
   home pages, so the seeded objects are covered.

`DatabaseSeedManager` holds a `PluginTypeCatalog<IDatabaseSeed>`: every `public sealed` top level
implementation of `KleeneStar.Core.WebSeed.IDatabaseSeed` with a public parameterless constructor
in a plugin assembly. The seeds run in ascending `Order`, then by `Key`. A seed that throws is
logged, its tracked entities are discarded and the next seed runs; the start continues.

`WebSeed/DemoSeed` is the plugin's seed (`Key` = `kleenestar.seeder`, `Order` = 100) and delegates
to `DemoSeeder.SeedAsync`.

## Rules of the demo seed

- **Store check.** `DemoSeeder.AppliesTo` holds when the store carries no workspace or at least one
  demo workspace. Otherwise the seed writes nothing: the demo accounts sign in with a published
  password.
- **Guards.** Each part runs only when its table (or its demo rows, for groups and accounts) is
  empty. Landing pages, landing labels and workspace home pages run on every start and skip what
  exists.
- **Administrator.** The demo data references the base administrator by
  `KleeneStarDbSeeder.AdministratorId` and leaves the account unchanged; it only sets Alice
  Engineer as its deputy.
- **Accounts.** Every demo account is internal and signs in with `DemoSeeder.DemoPassword`
  (`kleenestar`).
- **Audit.** The run that writes the demo workspaces appends one `Seeded` event with agent
  `kleenestar.seeder` through `KleeneStarDbSeeder.RecordSeed`: next sequence after the head, sealed
  onto the head's hash, row counts of tenants, identities, groups, workspaces, classes, fields,
  workflows and objects. The genesis event of the base seed carries the agent
  `kleenestar.model.seeder`.

## Deployment

The host references the plugin in the `Debug` and `DebugLocal` configurations
(`KleeneStar/src/KleeneStar/KleeneStar.csproj`). The `Release` build, the package and the container
image (`BUILD_CONFIGURATION=Release`) start with the base data.

## Tests

`KleeneStar.Seeder.Test`:

- `UnitTestDemoSeeder`: content and idempotency of the demo data on top of the base seed.
- `UnitTestDemoSeed`: discovery of `DemoSeed`, `DemoWorkspaceIds`, store check, audit event,
  unchanged administrator.

The base seed is tested in `Kleenestar.Model.Test` (`UnitTestKleeneStarDbSeeder`), the plugin seed
runner in `KleeneStar.Core.Test` (`UnitTestDatabaseStartup`).
