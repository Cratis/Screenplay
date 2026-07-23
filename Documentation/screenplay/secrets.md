# Secrets

Applications need credentials — client secrets, API keys, connection strings. Putting them in a `.play` file as plain strings would leak them into source control; keeping them entirely outside the language would make the declarations incomplete. Screenplay solves both: the language references secrets symbolically with `$secrets.<name>`, and the values live encrypted in `.secrets` files next to your `.play` files.

## Referencing a secret

Wherever a value expression is accepted — [authentication provider settings](authentication.md), `produces` mappings — a `$secrets.` expression references a secret by name:

```screenplay
authentication
  provider AzureAd
    type oidc
    clientId $secrets.azureAdClientId
    clientSecret $secrets.azureAdClientSecret
```

The compiler never resolves secrets. A `$secrets.<name>` expression stays symbolic in the syntax tree — consumers resolve it at runtime against the decrypted secrets of the environment they run in. The name allows dotted segments, consistent with the other expression paths.

## Secrets files

Secrets live in their own files, sibling to `.play` files, with the `.secrets` extension. The format is line based and human readable — but the values are always encrypted:

```
// Azure AD credentials
azureAdClientId     = enc:v1:q83vEjRWeJASNFZ4kKvN7w==
azureAdClientSecret = enc:v1:ASNFZ4mrze8SNFZ4q83vEg==
```

- One `<name> = enc:v1:<base64>` assignment per line.
- `//` comments and blank lines are allowed.
- A value that does not carry the `enc:` prefix is a parse error — a `.secrets` file never holds plaintext.

`SecretsFile.Parse` reads the format and `Write` produces the canonical form with assignments aligned on the longest name. `SecretsFiles` discovers every `.secrets` file beneath a root using the `**/*.secrets` glob, mirroring how `.play` files are discovered.

## Encryption

Values are encrypted with AES-256-GCM through `ISecretsCipher` and its `AesGcmSecretsCipher` implementation:

- The caller supplies a 256-bit key — key management is explicitly out of scope for this library. The Cratis CLI's centralized configuration holds and backs up the key.
- Every encryption uses a fresh random 12-byte nonce, so encrypting the same value twice yields different tokens.
- The encrypted format is `enc:v1:` followed by the base64 encoding of `nonce || ciphertext || 16-byte tag`.
- Decryption authenticates the value — a tampered token or a wrong key fails instead of yielding garbage.

## What this gives you

Because references are symbolic and values are encrypted, `.play` and `.secrets` files are both safe to commit. The declaration stays complete — you can read exactly which secrets an application needs — while the sensitive material is only readable where the key is available.
