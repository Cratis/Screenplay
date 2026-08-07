# Authentication

Policies and personas describe *who may do what* — but an application also has to say *how its users sign in*. The top level `authentication` block declares the identity providers of the application, so the sign-in surface is part of the same declaration as everything else.

## Syntax

```screenplay
authentication
  provider <Name> [name <Alias>]
  ...
```

- `authentication` — top level, alongside concepts, policies and personas. At most one block per document; a second one is a compile error.
- `provider <Name>` — one entry per identity provider, naming which provider it is: `EntraId`, `GitHub`, `Google`, `Apple`, `OpenId`.
- `name <Alias>` — what this provider goes by. Optional, and only needed when the provider alone does not identify it.

## Example

```screenplay
authentication
  provider EntraId
  provider GitHub
  provider Google
  provider Apple
```

## Naming a provider

A generic provider can appear more than once. Two `OpenId` providers are two *different* identity providers, and nothing about the word `OpenId` tells them apart — so give them names:

```screenplay
authentication
  provider OpenId name Partner
  provider OpenId name Supplier
```

A provider must be distinguishable: two entries that resolve to the same name are a compile error. Where no alias is given, the provider name is the name.

## What a provider deliberately does not carry

A provider says *which* identity provider the application signs users in with. It says nothing about how to reach one — no authority, no client id, no secret.

That is not an omission. Authority URLs, client ids and the credentials that go with them are what *running* the application needs to know, not what the application *is*, and they differ per environment while the document does not. A document that carried them would be a different document in test and in production, which is the opposite of what it is for.

Configuration lines under a provider are therefore a compile error rather than something quietly ignored — a document that states them is saying something the language does not express, and hiding that would be worse than reporting it.

A runtime such as Stage resolves the configuration itself, looking each provider up by name through the [compiler's visitors](visitors.md) when it runs or renders the application. How it stores that configuration — a JSON file holding encrypted values, a secret store, environment variables — is its own business, and the language stays out of it.
