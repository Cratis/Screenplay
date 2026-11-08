// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Checks storyline names and resolves every storyline step against the declarations of the kind it names.
/// </summary>
/// <remarks>
/// A storyline is authoring metadata, so a step that names nothing is a warning rather than an error: a stale
/// storyline never stops the model from compiling, but every tool reports it.
/// </remarks>
internal static class StorylineValidator
{
    static readonly DeclarationScope _root = new([]);

    internal static void Validate(ApplicationSyntax application, ParserContext context)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var storylines = application.Storylines.ToList();
        if (storylines.Count == 0) return;

        var declarations = StorylineDeclarations.Collect(application);
        foreach (var storyline in storylines)
        {
            if (storyline.Name.Length > 0 && !seen.Add(storyline.Name))
            {
                context.Error(DiagnosticCodes.DuplicateStoryline, $"Storyline '{storyline.Name}' is declared more than once", storyline.Location);
            }

            foreach (var step in storyline.Steps)
            {
                var kind = StorylineParser.Keyword(step.ElementKind);
                ScreenplayValidator.Report(step.Target, _root, declarations[step.ElementKind], DiagnosticCodes.UnknownStorylineElement, kind, step.Location, context);
            }
        }
    }
}
