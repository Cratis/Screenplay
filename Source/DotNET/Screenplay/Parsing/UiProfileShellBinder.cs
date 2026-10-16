// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Binds the application shell a <c>ui profile</c> names to the blueprints a host has installed.
/// </summary>
/// <remarks>
/// <see cref="ScreenplayCompiler"/> checks everything the document alone decides - an own template that is missing or cannot be a shell,
/// a blueprint the profile does not list, shell options the shell does not expose, the legacy <c>layout</c> line and invalid appearance
/// values. What a blueprint ships is only known where the blueprint is installed, so a host that knows (Stage, the CLI, MCP) hands its
/// catalog to <see cref="Bind"/>, which reports the remaining facts with the same codes (PLAY0790 to PLAY0799) and the same messages.
/// The rules are one set: the compiler runs them with no catalog, this runs them with one.
/// </remarks>
public static class UiProfileShellBinder
{
    /// <summary>
    /// Reports what the installed blueprints decide about the shells the document's ui profiles name.
    /// </summary>
    /// <param name="application">The compiled <see cref="ApplicationSyntax"/>.</param>
    /// <param name="blueprints">The blueprints that are installed.</param>
    /// <returns>The diagnostics only the catalog can produce; they never repeat those <see cref="ScreenplayCompiler"/> reports.</returns>
    public static IReadOnlyList<Diagnostic> Bind(ApplicationSyntax application, IReadOnlyList<BlueprintDescriptor> blueprints)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(blueprints);
        var context = ParserContext.ForDiagnostics();
        ProfileShellValidator.Validate(application, context, blueprints);
        return context.Diagnostics;
    }
}
