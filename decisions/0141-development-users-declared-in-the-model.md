---
id: 0141
title: Declare development and test users in the model with `user ... as <Persona>`
status: accepted
stage: designed
class: contract
reversibility: costly
decided: 2026-11-07
decider: Einar Ingebrigtsen (approved in conversation)
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
---

## Context

Arc offers development tooling a list of users through `/.cratis/users`, and Lens switches between them by sending the chosen
identity with every request. A Screenplay model has `persona` (a role and its policies) but nothing that names the people who
play them, so a running Stage has no users to offer: it plays one persona it picks itself, and switching means editing the model
and restarting.

## Decision

1. **A top-level `user` declaration, on one line.**

   ```screenplay
   user kari "Kari Nordmann" as SchemaSteward, ModelAdmin
   user ola "Ola Nordmann" as SchemaSteward
   user guest "Guest"
   ```

   The name is the identity a session presents. The quoted display name is optional. `as` lists the personas the user plays, in
   the form `invite tenant as` already uses. A user playing no persona is authenticated with no roles.
2. **A user holds the roles of its personas.** The principal is built from the policies of every persona the user plays, the
   way a persona caller in a specification is.
3. **Authoring metadata, outside the executable model.** Like a persona, a user never enters the ESM: no version bump, no change
   to canonical bytes, and a model without users compiles as before. It lives in the syntax tree and its JSON.
4. **Diagnostics.** `PLAY0887` for a malformed declaration or a nested line, `PLAY0888` for an unknown persona, `PLAY0889` for a
   repeated name. The TypeScript compiler reports `PLAY0887`; the C# compiler reports all three, as it does for personas.
5. **Where users may be used is the host's decision, not the language's.** Stage offers them to development tooling only when its
   own development mode and an explicit users switch are both on (see Stage). A rendered or published application never carries them.

## Consequences

- A host that does not know `user` ignores it, as it ignores a persona's goals.
- A user cannot carry claims of its own; a person who needs different claims needs a persona with the policies that give them.
