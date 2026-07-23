# Concepts

Concepts are formalized value types that wrap a primitive. They give every domain value a precise, strongly-typed name — you never pass a raw `Uuid` or `String` around — and they are where compliance is declared. Attributes control compliance behavior: Chronicle applies `@pii` and `@sensitive` rules automatically wherever the concept is used.

## Syntax

```screenplay
concept <Name> : <PrimitiveType> [<attributes>]
  [validate ...]*

concept <Name> : Enum
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
    if (Value.EndsWith("@example.com")) yield ValidationError("Example addresses are not allowed");
    ```
````

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
