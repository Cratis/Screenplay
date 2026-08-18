// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents an inline fenced code block in a specific language, such as <c>csharp</c> or <c>react</c>.
/// </summary>
/// <param name="Language">The language tag of the block.</param>
/// <param name="Code">The verbatim code inside the fence.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record CodeBlockSyntax(string Language, string Code, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>file</c> directive referencing an external file by repository relative path.
/// </summary>
/// <param name="Path">The repository relative path being referenced.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// One keyword carries both of the language's file relationships, and the node it sits on says which.
/// On a construct that <em>has</em> an implementation - a <see cref="HandlerSyntax">handler</see>, a
/// <see cref="PerformerSyntax">performer</see>, a reducer rule, a reaction trigger, a validation rule
/// predicate, a <see cref="FileConstraintSyntax">constraint</see>, a <see cref="ScreenSyntax">screen</see> -
/// the directive is an alternative to the inline body and says <em>the implementation lives there</em>.
/// On a pure declaration - a concept, a type, an event, a read model, a projection, a slice, a
/// specification, a trigger - there is no body to delegate, so it can only say <em>this is the file the
/// declaration is realized by</em>.
/// <para>
/// Those are different relationships, but the node type already decides which one is meant, so a second
/// keyword would carry no information a reader or a consumer does not already have - and would be one more
/// word to learn. A document therefore navigates back to the code it describes through the one word it
/// already uses.
/// </para>
/// <para>
/// The path is repository relative, matching what <c>cratis screenplay generate</c> emits. It is never
/// checked against a file system: a document is read in a designer, in a build and on a machine where the
/// tree is not present, so a path that has gone stale must never be what makes a valid document invalid.
/// A consumer that can resolve paths decides for itself what an unresolvable one means.
/// </para>
/// </remarks>
public record FileReferenceSyntax(string Path, SourceLocation Location) : SyntaxNode(Location);
