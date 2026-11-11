// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a top-level <c>secret &lt;Name&gt;</c> declaration: a named secret that systems map settings from and a
/// deployment binds to its store.
/// </summary>
/// <remarks>
/// A secret is realization metadata: the model names it and never holds its value, and it never enters the executable
/// semantic model.
/// </remarks>
/// <param name="Name">The name of the secret, unique application-wide ignoring case.</param>
/// <param name="Description">The optional description.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SecretSyntax(string Name, string? Description, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents the <c>extension "&lt;reference&gt;" [version &lt;range&gt;]</c> line of a system.
/// </summary>
/// <param name="Reference">The package reference: a catalog short name, an OCI reference or an npm package.</param>
/// <param name="Version">The version range, or <see langword="null"/> for the latest release.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SystemExtensionSyntax(string Reference, string? Version, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>setting</c> line of a system: where one of the extension's settings gets its value.
/// </summary>
/// <remarks>
/// Exactly one of <see cref="Secret"/> and <see cref="Value"/> is set. A literal is for non-secret settings only, and
/// the environment may override it.
/// </remarks>
/// <param name="Name">The setting name as the extension's contract declares it.</param>
/// <param name="Secret">The name of the declared <see cref="SecretSyntax"/> the value comes from, or <see langword="null"/> for a literal.</param>
/// <param name="Value">The literal value as text, or <see langword="null"/> when it comes from a secret.</param>
/// <param name="ValueKind">The kind of literal: <c>string</c>, <c>number</c> or <c>boolean</c>, or <see langword="null"/> when it comes from a secret.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SystemSettingSyntax(string Name, string? Secret, string? Value, string? ValueKind, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents the <c>activation always | on demand [idle after &lt;n&gt; &lt;unit&gt;]</c> line of a system.
/// </summary>
/// <param name="Always">Whether the backend is always on; otherwise it starts on demand.</param>
/// <param name="IdleAmount">The idle timeout amount, or <see langword="null"/> when none is written.</param>
/// <param name="IdleUnit">The idle timeout unit, singular: <c>second</c>, <c>minute</c>, <c>hour</c> or <c>day</c>, or <see langword="null"/> when none is written.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SystemActivationSyntax(bool Always, int? IdleAmount, string? IdleUnit, SourceLocation Location) : SyntaxNode(Location);
