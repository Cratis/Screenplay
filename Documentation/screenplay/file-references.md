---
title: "Screenplay file references: link a model to code"
description: "Use Screenplay file references to locate hand-written implementations or record where declarations are realized, without importing source code."
---

# File references

Cratis Screenplay's `file <path>` links a model element to hand-written code. On an implementation, such as a command handler, it replaces the inline code body. On a declaration, such as an event, it records where that declaration is realized without replacing its properties.

Use `file` when the implementation lives outside the `.play` document. To compose the model from other `.play` documents instead, use [file imports](imports.md). A file reference does not import code or make it executable.

## Two relationships, one word

For an implementation, write `file` where you would otherwise put the inline body. This command handler delegates its implementation to a C# file:

```screenplay
command RegisterInvoice
  invoiceId InvoiceId identifier
  handler
    file Invoicing/RegisterInvoice/RegisterInvoiceHandler.cs
```

The constructs that carry it in this sense are a command's [`handler`](commands.md) and its [validation rule predicates](commands.md), a query's [`performer`](queries.md), a [reducer](readmodels.md) rule, a [reaction](reactions.md) trigger, a [`constraint`](constraints.md), a [`screen`](screens.md) and a [`policy`](policies.md). A policy's file implements the same `bool` decision against `PolicyContext` as its inline `csharp` block; the two cannot appear together.

A declaration still needs its model shape: a concept's primitive type, for example, or an event's properties. Its `file` reference records where the corresponding code lives:

```screenplay
concept InvoiceId : Uuid
  file Invoicing/InvoiceId.cs
```

The declarations that carry it are `concept`, `type`, `event`, `readmodel`, `projection`, `slice`, `specification` and the top-level `trigger`.

The owning construct determines the relationship. A tool reading the model can distinguish an implementation attachment from a declaration reference without opening the named file.

A file-backed implementation uses the same typed context descriptor as its inline counterpart. The descriptor is derived from the declared command, concept, policy use site or reducer transition, not from the file path or its code. Unresolved file content can still have a descriptor when its model shape resolves; this does not mean the attachment can execute. Command handlers remain unbound even when their context is described. See [Contexts](context.md#typed-context-sidecars).

A command handler may nest this same `file` under an [`implementation` wrapper](commands.md#implementation-intent-handlers-only), alongside ordered hints. The wrapper does not select another file, change the path convention or introduce a provider/language selector. It has at most one file or tagged fence; without either it is pending intent. Other attachment owners retain their existing direct forms.

## A worked example

```screenplay
concept InvoiceId : Uuid
  file Invoicing/InvoiceId.cs

module Invoicing
  feature Invoices
    slice StateChange RegisterInvoice
      description "Registers an invoice against a customer"
      file Invoicing/RegisterInvoice/RegisterInvoice.cs

      command RegisterInvoice
        invoiceId InvoiceId identifier

        produces InvoiceRegistered
          invoiceId = invoiceId

      event InvoiceRegistered
        file Invoicing/RegisterInvoice/RegisterInvoice.cs
        invoiceId InvoiceId

      readmodel Invoice
        file Invoicing/RegisterInvoice/Invoice.cs
        invoiceId InvoiceId

      projection Invoices => Invoice
        file Invoicing/RegisterInvoice/Invoices.cs
        from InvoiceRegistered
          invoiceId = invoiceId

      specification RegisteringAnInvoice
        file Invoicing/RegisterInvoice/when_registering_an_invoice.cs
        when RegisterInvoice
          invoiceId = "0f5f5f7f-0f6f-4f47-9f39-5c1f2f0a1a9f"
        then InvoiceRegistered
```

Several declarations can name the same file. In this example, the slice and its event both point to `RegisterInvoice.cs`, following the Cratis convention of keeping a slice's backend artifacts together.

The `file` line is written directly under the header, after a `description` when there is one. That is where the printer puts it too, so a generated document and a hand-written one read the same.

## The rules a path follows

Write repository-relative paths, not absolute ones, so the reference is portable between machines. An absolute path reports [`PLAY0264`](diagnostics.md), a warning that does not prevent syntax compilation.

Syntax compilation does not open the referenced file. You can therefore read and check a `.play` document without the implementation's source tree. The standalone Tool and `PlayFileCompiler` compile syntax only and do not load attachments (#244).

A host with a trusted physical model root can call `AttachmentFiles.Load(root, documents)` to supply implementation attachment text to semantic compilation for content hashing only. Paths resolve from that model root, not from the `.play` file's directory. This differs from [file imports](imports.md#patterns), which resolve relative to the importing document. Moving a `.play` file within the same model root therefore does not change the attachment path's base.

Declaration-only `file` references are not read. The in-memory workspace does not read the file system either: pass the loader's `Contents` and `Diagnostics` to `WithAttachmentContents` when your host needs attachment hashes.

The loader normalizes `./` and separators, rejects absolute, drive-qualified and escaping paths, and refuses symbolic links and reparse points in the attachment or any directory between the root and it. Files exceeding 2 MiB each or 8 MiB combined, missing files, and unreadable/non-UTF-8 files are omitted with `PLAY0430`–`PLAY0434` warnings. A refused implementation remains `UnresolvedFile` with no content hash. Hosts can display the warnings alongside the semantic compilation. Requirement identities do not depend on paths or contents.

On Linux and macOS (x64 and arm64), the loader opens the model root once and walks the normalized relative path using directory handles. Each open does not follow links; the final handle must identify a regular file, and its reported size is checked before reading. Other Unix ABIs are refused. Windows retains path-based attribute checks before opening and link checks after reading; it does not provide the same handle-relative no-follow behavior, so the model root must not be changed during loading. Only a SHA-256 hash of loaded content is exposed in the semantic model, not the content itself.

Keep the declaration's shape in the model even when it names a file. A `projection` still declares its blocks, an `event` its properties, and a `type` at least one property. A document remains [expressible with zero `file` references](grammar.md#declarative-first--file-is-never-required); adding a reference records where the code lives rather than leaving the model incomplete.

## Telling it from a property named `file`

`event`, `readmodel` and `type` bodies also read property lines, and `file` is a legal property name. The two are told apart by shape, which is the rule [`description`](types.md) already follows in the same bodies: a type reference is a bare identifier, so a value carrying a separator or an extension is a path and nothing else.

```screenplay
type Upload
  file Attachment
  size Int
```

That declares a property named `file` of type `Attachment` — the property wins the tie, so a document written before the directive existed keeps meaning what it meant. A single-segment path with no extension (`file Makefile`) is read as a property for the same reason; write it as the repository relative path it is.

A `trigger` body has always reserved the word, so there the directive wins and a trigger value named after it is written `@file`:

```screenplay
trigger LedgerFileArrived
  description "A ledger export landed in the drop folder"
  file Integrations/LedgerFileTrigger.cs
  @file
  name
```

## Reading them from the syntax tree

Every one of these is a `FileReferenceSyntax` on the node that declares it, reached through [`ScreenplaySyntaxWalker.VisitFileReference`](visitors.md). Override that one method and you see every file a document names, whichever construct named it.

On most constructs that have an implementation, the reference is a constructor parameter. A policy uses an additive `init` property named `File` to preserve its positional record contract. Declarations also use an `init` property named `File`, so set it in an object initializer or a `with` expression rather than positionally. See [Syntax tree compatibility](ast-compatibility.md) for the compatibility contract.

If your tool also edits the model, use a [revision-bound workspace](folders.md#propose-revision-safe-workspace-changes) to propose and review the source change. Reprinting a merged tree is not a substitute for preserving the original documents.

## See also

- [Grammar](grammar.md) — the EBNF, and the declarative-first guarantee.
- [Diagnostics](diagnostics.md) — `PLAY0264`, and why an unresolvable path is not one.
- [Visitors and traversal](visitors.md) — walking a document to collect what it points at.
