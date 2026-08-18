// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Printing;

/// <summary>
/// Printing of what a syntax tree holds and the grammar cannot carry back.
/// </summary>
/// <remarks>
/// The printer's contract is that its output compiles to an equivalent tree, so it must never write a
/// construct the parser does not read back in that position - text the compiler rejects is worse than no
/// text at all. A tree built by hand rather than parsed can hold such a value, because nothing in the
/// syntax nodes says which slot is legal where.
/// <para>
/// Where that happens the value is left out of the grammar and named in a comment instead: reported, rather
/// than dropped without a word or replaced with something invented. A comment is the only channel the
/// printing path has - <see cref="IScreenplayPrinter"/> returns text and nothing else - and it is a real
/// one, because comments reach whoever reads the generated file and are stripped before any parser sees a
/// line, so the note costs the document nothing.
/// </para>
/// <para>
/// A note is one way. Compiling the printed document gives back a tree without the value, and printing that
/// tree again has nothing left to note - which is the honest account of what happened: the value never
/// reached the text.
/// </para>
/// </remarks>
public partial class ScreenplayPrinter
{
    /// <summary>
    /// The reason a validation rule's implementation is left out on every kind but a named predicate.
    /// </summary>
    const string OnlyOnANamedPredicate = "a rule implementation is read back only on a 'rule <Name>' predicate";

    /// <summary>
    /// The reason the second of two implementations is left out, where the grammar reads exactly one.
    /// </summary>
    /// <param name="declaration">The declaration that carries the implementation.</param>
    /// <returns>The reason to name in the note.</returns>
    static string ReadsOneImplementation(string declaration) =>
        $"{declaration} is read back as a file reference or an inline block, not both";

    void WriteOmission(ScreenplayWriter writer, string what, string reason) =>
        writer.Line($"// TODO: {what} is not written here - {reason}");

    void WriteOmittedFile(ScreenplayWriter writer, FileReferenceSyntax file, string reason) =>
        WriteOmission(writer, $"'file {file.Path}'", reason);

    void WriteOmittedCode(ScreenplayWriter writer, CodeBlockSyntax code, string reason) =>
        WriteOmission(writer, $"the inline '{code.Language}' block", reason);
}
