![KleeneStar](https://raw.githubusercontent.com/kleenestar-project/.github/main/docs/assets/img/banner.png)

# KleeneStar.Seeder - Demo and Test Data

**KleeneStar.Seeder** is the plugin that fills a **KleeneStar** installation with realistic demonstration and test data. It provides tools and predefined datasets for seeding test, demo, and development environments.

Built on the **WebExpress-Framework**, it contributes one database seed that the core runs at start, after the database is migrated and the base data is written.

## What It Seeds

A fresh database created by **KleeneStar.Core** carries the base data only: the administrator account and its group, the built-in groups, the status categories, the relation catalog, the branding and maintenance records and the genesis event of the audit log.

**KleeneStar.Seeder** adds on top of that:
- Tenants, workspace categories, demo groups and demo accounts (`alice.engineer`, `marketing.user`, `support.user`)
- Seven workspaces with classes, fields, forms, workflows, priorities, security levels, templates, calendars and SLA policies
- Objects with values, history, labels, shares, comments, attachments, relations and sprints
- Views, insights, saved searches, bookmarks, landing pages and workspace home pages

Every part is written only when the store does not carry it yet, so the seed runs on every start of every replica. A store whose workspaces are all its own is a productive installation; the demo data is not added to it. The demo accounts sign in with the published password `kleenestar`.

## Usage

The host references the plugin in the `Debug` and `DebugLocal` configurations. The `Release` build (package and container image) does not include it and starts with the base data. To get demo data elsewhere, deploy the plugin beside the host; to start without it, leave it out.

```
dotnet build KleeneStar.Seeder/src/KleeneStar.Seeder.slnx
KleeneStar.Seeder/src/KleeneStar.Seeder.Test/bin/Debug/net10.0/KleeneStar.Seeder.Test.exe
```

## For Developers

The plugin implements the core's extension point `KleeneStar.Core.WebSeed.IDatabaseSeed`. Any plugin can contribute data the same way: a `public sealed` class with a public parameterless constructor, collected by the core's `DatabaseSeedManager` and run in ascending `Order` under the startup locks. See `docs/kleenestar.seeder.md`.

## Legal & Licensing

**KleeneStar.Seeder** is released under the `MIT License`, a permissive open-source license that allows reuse, modification, and distribution with minimal restrictions. You're free to use **KleeneStar** in personal, academic, or commercial projects, just include the original copyright notice.

The system is designed to be GDPR-compliant:
- No tracking
- No monetization
- No hidden dependencies
- Full transparency and infrastructure control

**KleeneStar** respects your data and your autonomy. It's built for clarity, not surveillance.

## Contributing

We welcome contributions in many areas:
- Demo scenarios and datasets
- Plugin development (C#)
- Documentation and onboarding flows
- Semantic modeling and naming conventions

Feel free to fork the repository, open issues, or submit pull requests. For larger contributions, please reach out via kleenestar.project@gmail.com.

# AI transparency notice

Parts of this software, its documentation, and its assets were created with the assistance of AI-based tools, including large language models. AI-assisted contributions are reviewed by the project maintainer before they are included.

---

Become part of the **KleeneStar** community and contribute to a modular, open-source future.
