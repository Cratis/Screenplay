# Authentication

Policies and personas describe *who may do what* — but an application also has to say *how its users sign in*. The top level `authentication` block declares the identity providers of the application, so the sign-in surface is part of the same declaration as everything else.

## Syntax

```screenplay
authentication
  provider <Name>
    <setting> <value>
    ...
  ...
```

- `authentication` — top level, alongside concepts, policies and personas. At most one block per document; a second one is a compile error.
- `provider <Name>` — one entry per identity provider. Provider names must be unique; duplicates are compile errors.
- `<setting> <value>` — free-form settings. Screenplay does not prescribe the setting names — each provider type has its own vocabulary (`type`, `authority`, `clientId`, ...). Values use the same expression grammar as `produces` mappings: string literals, numbers, booleans, bare identifiers, `$env.` and [`$secrets.`](secrets.md) references.

## Example

```screenplay
authentication
  provider AzureAd
    type oidc
    authority "https://login.microsoftonline.com/common/v2.0"
    clientId $secrets.azureAdClientId
    clientSecret $secrets.azureAdClientSecret
  provider GitHub
    type oauth
    clientId $secrets.githubClientId
    clientSecret $secrets.githubClientSecret
```

Credentials never appear as plaintext — `$secrets.` references stay symbolic in the syntax tree and resolve at runtime against the encrypted [secrets files](secrets.md), while `$env.` references resolve from the environment. This keeps the whole block safe to commit.

The compiler validates the shape of the block — the single-block rule, provider name uniqueness and the setting line format — and hands the providers to consumers as-is. What a runtime does with a provider declaration is up to it; the declaration is the contract.
