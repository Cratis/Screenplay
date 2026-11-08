// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Mcp;

// The list-storylines report: every storyline with its steps in order, each resolved the way the compiler resolves it.
static class McpStorylines
{
    internal static object Read(McpSnapshot snapshot, JsonElement arguments)
    {
        var name = McpJson.OptionalString(arguments, "name");
        var application = snapshot.Compilation.Value;
        var storylines = (application?.Storylines ?? []).ToArray();
        if (name is not null && storylines.All(storyline => storyline.Name != name))
        {
            throw new McpFailure($"Unknown storyline '{name}'. Declared: {(storylines.Length == 0 ? "none" : string.Join(", ", storylines.Select(storyline => storyline.Name)))}.", -32602);
        }

        var declarations = application is null ? null : StorylineDeclarations.Collect(application);
        return new
        {
            snapshot.Compilation.Success,
            snapshot.SourceRevision,
            storylines = storylines.Where(storyline => name is null || storyline.Name == name).Select(storyline => Describe(storyline, declarations!)).ToArray()
        };
    }

    static object Describe(StorylineSyntax storyline, StorylineDeclarations declarations) => new
    {
        storyline.Name,
        storyline.Title,
        storyline.Description,
        storyline.Location,
        steps = storyline.Steps.Select((step, index) => Step(step, index, declarations)).ToArray()
    };

    static object Step(StorylineStepSyntax step, int index, StorylineDeclarations declarations)
    {
        var (resolved, candidates) = declarations.Resolve(step);
        var status = (resolved, candidates.Count) switch
        {
            (not null, _) => "resolved",
            (null, > 0) => "ambiguous",
            _ => "missing"
        };
        return new
        {
            index,
            kind = StorylineParser.Keyword(step.ElementKind),
            step.Target,
            step.Note,
            step.Location,
            status,
            element = resolved is null ? null : Element(resolved),
            candidates = candidates.Select(Element).ToArray()
        };
    }

    // kind and address are what declaration-details and find-declaration take.
    static object Element(StorylineDeclaration declaration) => new
    {
        kind = declaration.Node switch
        {
            ModuleSyntax => "Module",
            FeatureSyntax => "Feature",
            SliceSyntax => "Slice",
            CommandSyntax => "Command",
            EventSyntax => "Event",
            ReadModelSyntax => "ReadModel",
            QuerySyntax => "Query",
            ScreenSyntax => "Screen",
            _ => "Reaction"
        },
        address = string.Join('.', declaration.Declaration.Scope.Segments.Append(declaration.Declaration.Name)),
        location = declaration.Node.Location
    };
}
