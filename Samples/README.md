# Samples

Hand-written Screenplay applications, each in its own folder. A folder compiles as one application, so every
sample can be verified on its own:

```bash
screenplay --warnaserror Samples/<Sample>
```

| Sample | Size | Shows |
| --- | --- | --- |
| [Library](Library) | 1 file, 8 slices | The smallest useful application: commands, events, a projection, a view no event builds, a reaction, screens and specifications. Start here. |
| [Invoicing](Invoicing) | 1 file, 30 slices | Every construct in the language, used for something an invoicing system needs, with English and Norwegian `.strings` files. |
| [Commerce](Commerce) | many small files | An application composed with [imports](../Documentation/screenplay/imports.md) at every level: a root file that reads like a table of contents, a file per module and per feature that imports its own folder, and one file per slice that holds only the slice. |
| [TimeTracking](TimeTracking) | many small files | An application composed from one root glob. Module files declare their features inline and import each feature's folder, and each step of the story is a file of its own. |

## What every sample keeps to

- Every state change and state view slice has a screen. Whatever gates it is satisfied by the policies of the
  persona who uses it, so the event model board draws the screen in that persona's row.
- Every slice has Given/When/Then specifications.
- Views do not have to be built from events. Each sample beyond the smallest has at least one read model that
  a query's `performer` composes from somewhere else.
- No sample uses deprecated syntax, and each compiles with zero errors and zero warnings.
- A folder sample has no large file. Each file tells one part of the story, and the imports in its root reach every
  file in the folder.

`dotnet test` checks all of this in `Source/DotNET/Screenplay/for_Samples`, and the samples change in the same
pull request as the language does. See [the samples rule](../.cratis/ai/rules/project/samples.md).
