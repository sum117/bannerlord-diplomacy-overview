# Diplomacy Overview — Bannerlord mod

A Mount & Blade II: Bannerlord mod that adds a **Relations** view to the Kingdom screen: every
kingdom (or clan) as a banner medallion on a circle, with colored lines showing who is **at war**,
**allied**, or bound by a **non-aggression pact** — toggleable per relation type, with a dropdown to
switch between kingdom and clan scope. No relation, no line.

Inspired by the diplomacy web in *atWar*; designed to work on vanilla **v1.4.7 + War Sails** and
alongside the **Realm of Thrones** total conversion and the **Diplomacy** mod (whose pacts it reads
when installed). Read-only by design: safe to add or remove mid-campaign.

![Design reference](docs/images/diplomacy-overview-design-reference.png)

## Status

**In development** (game v1.4.7): module scaffold, the pure relations core (96 passing unit tests),
and the kingdom war web (issue #6) are in flight; a sacrificial tracer proved the injection seams.
The research pass (environment, module anatomy, Gauntlet UI techniques, diplomacy APIs,
compatibility, architecture, pitfalls) lives in [docs/research/](docs/research/README.md) — start
there.

## Planned v1 scope

- Kingdom + clan scopes (clans clustered by kingdom)
- War (red) / Alliance (green) / NAP (via Diplomacy mod) edges with legend toggles
- Banner-medallion nodes, edge tooltips (war stats, pact expiry)
- Verified against vanilla and Realm of Thrones

Note: game **v1.4.7 added native kingdom Trade Agreements**, giving the originally requested "trade
pact" lines a first-class vanilla data source — see
[docs/research/11](docs/research/11-game-1.4.7-migration.md) for the API and
[docs/research/09](docs/research/09-design-reference-mapping.md) for the (reopened) scope decision.

## Contributing / agents

See [AGENTS.md](AGENTS.md) for build commands, hard rules, and conventions.
