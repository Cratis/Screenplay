# Concepts

Concepts are formalized value types that wrap a primitive. They give every domain value a precise, strongly-typed name — you never pass a raw `Uuid` or `String` around — and they are where compliance is declared. Attributes control compliance behavior: Chronicle applies `@pii` and `@sensitive` rules automatically wherever the concept is used.

A concept names one primitive value. For a shape made of several — the child records events carry — see [Types](types.md).

## Syntax

```screenplay
concept <Name> : <PrimitiveType> [<attributes>]
  [<attribute> reason "<text>"]*
  [validate ...]*

concept <Name> : Enum
  [<attribute> reason "<text>"]*
  <value>+
  [validate ...]*
```

## Primitive types

`Uuid`, `String`, `Int`, `Decimal`, `Bool`, `Date`, `DateTime`

## Attributes

| Attribute | Meaning |
| --- | --- |
| `@pii` | The value is personally identifiable information. Chronicle manages it and can erase it for GDPR compliance. |
| `@sensitive` | The value is sensitive and handled under Chronicle's sensitivity rules. |

## Examples

```screenplay
concept InvoiceId        : Uuid
concept EmailAddress     : String   @pii
concept NationalIdNumber : String   @pii @sensitive
concept DateOfBirth      : Date     @pii
```

## Why a value is personal data

The marker says a value *is* personal data. It does not say **why** — the purpose it is kept for, the lawful basis, whose subject it lives under, whether it is erasable. That is exactly what a compliance reader opens a Screenplay to find, and Screenplay's stated goal is that concepts carry compliance, not just the flag.

An indented `<attribute> reason "<text>"` line records it:

```screenplay
concept BankAccount : String @pii @sensitive
  pii reason "Partner payout bank account - financial data. Remits self-billing payments; lawful basis: contract performance / legal obligation. Personal only for sole-proprietor partners."
  sensitive reason "Fraud-sensitive - a leaked account number enables direct financial harm, so it never leaves the payout path."
```

Each attribute the concept declares may carry at most one reason, and a reason may only be given for an attribute the concept actually declares — `sensitive reason "…"` on a concept that is only `@pii` is a compile error. A reason is optional throughout: a bare `@pii` stays valid and prints on one line.

## Enum concepts

An enum concept declares a fixed set of values as an indented list:

```screenplay
concept InvoiceStatus : Enum
  draft
  sent
  paid
  overdue
  cancelled

concept PaymentTerms : Enum
  net30
  net60
  immediate
```

## Validation

A concept can declare validation rules in an optional indented body — business rules that travel with the value everywhere it appears. The rules use the same shapes as command validation (see [Commands](commands.md)): declarative `validate` blocks and imperative `validate csharp` blocks. The one difference is that the rules omit the property subject — the concept's own value is implied.

````screenplay
concept EmailAddress : String @pii
  validate
    not empty          message "Email is required"
    matches "^.+@.+$"  message "Must be a valid email address"
  validate csharp
    ```
    string email = context.Value;
    if (email.EndsWith("@example.com", StringComparison.OrdinalIgnoreCase))
    {
        yield return "Example addresses are not allowed";
    }
    ```
````

Inside the block `context` is the [`RuleContext`](context.md) — for a concept rule there is no surrounding artifact, so `context.Artifact` and `context.Value` are both the concept's own value. The block yields the message of every rule the value breaks, and yields nothing when the value is valid.

Enum concepts can combine their values with validate blocks — the values remain bare identifiers and the blocks are recognized by the `validate` keyword:

```screenplay
concept InvoiceStatus : Enum
  draft
  sent
  validate
    not empty  message "Status is required"
```

In the compiled syntax tree the implied subject is represented by the well-known property name `value` — the `ValidationRuleSyntax.ConceptValue` constant — so consumers can treat concept rules and command rules uniformly.

## Attribute inheritance

When a concept is used as a property type on a command or event, its attributes are inherited — you never annotate at the property level. Declaring `EmailAddress` as `@pii` once means every event property, command property, and read model field typed as `EmailAddress` is treated as PII automatically.

This holds through composite [types](types.md) too: a `@pii` concept inside a `type` is personal data wherever that type is used.
