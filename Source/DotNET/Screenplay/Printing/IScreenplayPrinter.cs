// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Printing;

/// <summary>
/// Defines a printer that renders a Screenplay syntax tree back to <c>.play</c> source text.
/// </summary>
/// <remarks>
/// The printer is the reverse of <see cref="IScreenplayCompiler"/>: where the compiler turns source text
/// into a syntax tree, the printer turns a syntax tree back into source text. The output is valid
/// Screenplay that compiles to an equivalent tree, so a tree can be modified programmatically and printed
/// back out - the basis for generating <c>.play</c> files from a model.
/// </remarks>
public interface IScreenplayPrinter
{
    /// <summary>
    /// Prints a whole <see cref="ApplicationSyntax"/> as a <c>.play</c> document.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to print.</param>
    /// <returns>The rendered <c>.play</c> source text.</returns>
    string Print(ApplicationSyntax application);

    /// <summary>
    /// Prints a standalone <see cref="ProjectionSyntax"/> as projection source text.
    /// </summary>
    /// <param name="projection">The <see cref="ProjectionSyntax"/> to print.</param>
    /// <returns>The rendered projection source text.</returns>
    string Print(ProjectionSyntax projection);

    /// <summary>
    /// Prints a standalone <see cref="SpecificationSyntax"/> as specification source text.
    /// </summary>
    /// <param name="specification">The <see cref="SpecificationSyntax"/> to print.</param>
    /// <returns>The rendered specification source text.</returns>
    string Print(SpecificationSyntax specification);

    /// <summary>
    /// Prints a standalone <see cref="CaptureSyntax"/> as capture source text.
    /// </summary>
    /// <param name="capture">The <see cref="CaptureSyntax"/> to print.</param>
    /// <returns>The rendered capture source text.</returns>
    string Print(CaptureSyntax capture);
}
