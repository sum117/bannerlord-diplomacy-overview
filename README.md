# Diplomacy Overview — Bannerlord mod

A Mount & Blade II: Bannerlord mod that adds a **Relations** view to the Kingdom screen: every
kingdom (or clan) as a banner medallion on a circle, with colored lines showing who is **at war**,
**allied**, or bound by a **non-aggression pact** — toggleable per relation type, with a dropdown to
switch between kingdom and clan scope. No relation, no line.

Inspired by the diplomacy web in *atWar*; designed to work on vanilla **v1.3.15 + War Sails** and
alongside the **Realm of Thrones** total conversion and the **Diplomacy** mod (whose pacts it reads
when installed). Read-only by design: safe to add or remove mid-campaign.

![Design reference](docs/images/diplomacy-overview-design-reference.png)

## Status

**Bootstrap / research phase.** No shippable code yet. The full research pass (environment, module
anatomy, Gauntlet UI techniques, exact v1.3.15 diplomacy APIs, compatibility, architecture,
pitfalls) lives in [docs/research/](docs/research/README.md) — start there.

## Planned v1 scope

- Kingdom + clan scopes (clans clustered by kingdom)
- War (red) / Alliance (green) / NAP (via Diplomacy mod) edges with legend toggles
- Banner-medallion nodes, edge tooltips (war stats, pact expiry)
- Verified against vanilla and Realm of Thrones

Note: "trade pact" lines from the original request have **no data source** in vanilla or the
Diplomacy mod — see [docs/research/09](docs/research/09-design-reference-mapping.md) for options.

## Contributing / agents

See [AGENTS.md](AGENTS.md) for build commands, hard rules, and conventions.
