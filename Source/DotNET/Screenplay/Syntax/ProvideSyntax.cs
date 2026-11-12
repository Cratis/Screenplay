// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a <c>provide &lt;name&gt; &lt;Type&gt; from &lt;source&gt;</c> line that adds a named value to <c>$context</c>
/// for everything beneath the application, module, feature or slice that declares it.
/// </summary>
/// <param name="Name">The name of the value, read as <c>$context.&lt;name&gt;</c>.</param>
/// <param name="Type">The type of the value; optional when the value may be absent.</param>
/// <param name="Source">Where the value comes from.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ProvideSyntax(string Name, TypeRefSyntax Type, ProvideSourceSyntax Source, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents where a provided value comes from.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public abstract record ProvideSourceSyntax(SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents <c>from query &lt;Query&gt; [by &lt;expression&gt;, ...]</c> - a read model query run once per request.
/// </summary>
/// <param name="Query">The name of the query.</param>
/// <param name="By">The key expressions the query is run with; empty when the query takes none.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record QueryProvideSourceSyntax(string Query, IEnumerable<ExpressionSyntax> By, SourceLocation Location) : ProvideSourceSyntax(Location);

/// <summary>
/// Represents <c>from host</c> - a value of the declared name and type that the hosting runtime supplies.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record HostProvideSourceSyntax(SourceLocation Location) : ProvideSourceSyntax(Location);

/// <summary>
/// Represents <c>from &lt;value&gt;</c> - a literal or an expression over other <c>$context</c> values.
/// </summary>
/// <param name="Value">The literal or expression.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ValueProvideSourceSyntax(ExpressionSyntax Value, SourceLocation Location) : ProvideSourceSyntax(Location);

/// <summary>
/// Represents an inline code block that computes the value.
/// </summary>
/// <param name="Code">The <see cref="CodeBlockSyntax">code</see>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record CodeProvideSourceSyntax(CodeBlockSyntax Code, SourceLocation Location) : ProvideSourceSyntax(Location);

/// <summary>
/// Represents a <c>file</c> reference to the code that computes the value.
/// </summary>
/// <param name="File">The <see cref="FileReferenceSyntax">file</see>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record FileProvideSourceSyntax(FileReferenceSyntax File, SourceLocation Location) : ProvideSourceSyntax(Location);
