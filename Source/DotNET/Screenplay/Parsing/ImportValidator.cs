// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Reports an <c>import</c> whose name the application declares itself.
/// </summary>
/// <remarks>
/// An import names something from outside the application. When the application declares the name, the
/// declaration is what every reference resolves to and what every check sees, so the import changes nothing
/// and only claims the opposite of what is true. It stays a warning: the application it describes is still
/// well defined, and removing the line is the whole fix.
/// </remarks>
internal static class ImportValidator
{
    /// <summary>
    /// Validates every import of an application.
    /// </summary>
    /// <param name="application">The application whose imports to check.</param>
    /// <param name="declarations">The application declarations.</param>
    /// <param name="context">The diagnostic sink.</param>
    public static void Validate(ApplicationSyntax application, ConsistencyDeclarations declarations, ParserContext context)
    {
        foreach (var import in application.Imports.Where(import => declarations.Declares(import.Name)))
        {
            context.Warning(
                DiagnosticCodes.RedundantImport,
                $"Import '{import.QualifiedName}' names '{import.Name}', which this application declares - the import has no effect",
                import.Location);
        }
    }
}
