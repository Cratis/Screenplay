# Library

A small lending library in one file, the place to start if you have never read a `.play` document. It has eight
slices across three features, and two people who use it.

```text
Library/
  library.play   concepts, two policies and personas, one module with three features
```

| Persona | Policy | Sees the screens of |
| --- | --- | --- |
| `Librarian` | `IsLibrarian` | AddBook, RegisterMember, BookCatalog, OpeningHours |
| `Member` | `IsMember` | BorrowBook, ReturnBook, MyLoans, BookCatalog, OpeningHours |

## What to read it for

- **A slice from intent to proof.** `AddBook` declares the command, its validation, the event it produces, a
  uniqueness constraint, the screen that invokes it, and the specifications that pin down success, rejection
  and denial.
- **Forms discovered by command actions.** The catalog's add and borrow actions, and the loans view's return
  action, discover command-bound forms that collect their inputs. The borrow screen's navigation to `MyLoans`
  happens after the command runs, not before input is collected.
- **A command that decides against state.** `BorrowBook` `reads` the catalog entry and refuses a book that is
  already on loan with a `require` rule.
- **A view built from events, and one that is not.** `BookCatalog` and `MyLoans` are projections; `OpeningHours`
  comes from the municipality's calendar through a query `performer`, so no event builds it.
- **Something that happens on its own.** `WelcomeNewMembers` reacts to `MemberRegistered`.

## Verify

```bash
screenplay --warnaserror Samples/Library
# or, from a clone of this repository
dotnet run --project Source/DotNET/Tool -- --warnaserror Samples/Library
```

When you outgrow one file, see [Commerce](../Commerce) for the canonical folder layout, and
[Invoicing](../Invoicing) for every construct the language has.
