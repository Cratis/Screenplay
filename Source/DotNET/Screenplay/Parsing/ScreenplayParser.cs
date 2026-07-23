// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses a full Screenplay document into an <see cref="ApplicationSyntax"/>.
/// </summary>
internal static partial class ScreenplayParser
{
    /// <summary>
    /// Parses a document.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="lines">All <see cref="SourceLine">lines</see> of the document, for whole document checks.</param>
    /// <returns>The parsed <see cref="ApplicationSyntax"/>.</returns>
    public static ApplicationSyntax Parse(ParserContext context, IReadOnlyList<SourceLine> lines)
    {
        WarnOnTabIndentation(context, lines);

        DomainSyntax? domain = null;
        var imports = new List<ImportSyntax>();
        var concepts = new List<ConceptSyntax>();
        var policies = new List<PolicySyntax>();
        var modules = new List<ModuleSyntax>();

        while (context.Reader.PeekSignificant() is { } line)
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(line.Content))
            {
                case "domain":
                    domain = ParseDomain(context, line, domain, imports.Count > 0 || concepts.Count > 0 || policies.Count > 0 || modules.Count > 0);
                    break;
                case "import":
                    if (ImportRegex().Match(line.Content) is { Success: true } import)
                    {
                        imports.Add(new(import.Groups[1].Value, line.Location));
                    }
                    else
                    {
                        context.Error($"Invalid import '{line.Content}' - expected 'import <Qualified.Name>'", line.Location);
                    }

                    break;
                case "concept":
                    concepts.Add(ParseConcept(context, line));
                    break;
                case "policy":
                    policies.Add(PolicyParser.Parse(context, line));
                    break;
                case "module":
                    modules.Add(ParseModule(context, line));
                    break;
                default:
                    context.Error($"Unexpected '{LineText.FirstWord(line.Content)}' at the top level - expected domain, import, concept, policy or module", line.Location);
                    context.SkipBlock(line.Indent);
                    break;
            }
        }

        return new(imports, concepts, policies, modules, SourceLocation.Start, domain);
    }

    static DomainSyntax? ParseDomain(ParserContext context, SourceLine line, DomainSyntax? existing, bool hasOtherConstructs)
    {
        var match = DomainRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error($"Invalid domain declaration '{line.Content}' - expected 'domain <Qualified.Name>'", line.Location);
            return existing;
        }

        if (existing is not null)
        {
            context.Error("The document already declares a domain - a document can have at most one", line.Location);
            return existing;
        }

        if (hasOtherConstructs)
        {
            context.Error("'domain' must be declared before any other construct", line.Location);
        }

        return new(match.Groups[1].Value, line.Location);
    }

    static void WarnOnTabIndentation(ParserContext context, IReadOnlyList<SourceLine> lines)
    {
        var inFence = false;
        foreach (var line in lines)
        {
            if (line.Raw.Trim() == "```")
            {
                inFence = !inFence;
                continue;
            }

            if (!inFence && TabIndentRegex().IsMatch(line.Raw))
            {
                context.Warning("Screenplay is indentation based - use spaces, not tabs", new(line.Number, 1));
            }
        }
    }

    static ConceptSyntax ParseConcept(ParserContext context, SourceLine line)
    {
        var match = ConceptRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error($"Invalid concept declaration '{line.Content}' - expected 'concept <Name> : <Type>'", line.Location);
            context.SkipBlock(line.Indent);
            return new(LineText.FirstWord(line.Content), string.Empty, [], [], line.Location);
        }

        var name = match.Groups[1].Value;
        var type = match.Groups[2].Value;
        var attributes = match.Groups[3].Value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(_ => _.TrimStart('@'))
            .ToList();

        var values = new List<string>();
        if (type == "Enum")
        {
            while (context.TryPeekChild(line.Indent, out var value))
            {
                context.Reader.TakeSignificant();
                if (EnumValueRegex().IsMatch(value.Content))
                {
                    values.Add(value.Content);
                }
                else
                {
                    context.Error($"Invalid enum value '{value.Content}' - expected an identifier", value.Location);
                }
            }
        }
        else if (!ConceptSyntax.PrimitiveTypes.Contains(type))
        {
            context.Error($"Unknown primitive type '{type}' - expected {string.Join(", ", ConceptSyntax.PrimitiveTypes)} or Enum", line.Location);
        }

        return new(name, type, attributes, values, line.Location);
    }

    static ModuleSyntax ParseModule(ParserContext context, SourceLine line)
    {
        var match = ModuleRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error($"Invalid module declaration '{line.Content}' - expected 'module <Name>'", line.Location);
        }

        var name = match.Groups[1].Value;
        string? description = null;
        var layouts = new List<LayoutSyntax>();
        var features = new List<FeatureSyntax>();

        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(child.Content))
            {
                case "description":
                    description = DescriptionParser.Parse(context, child, description, $"Module '{name}'");
                    break;
                case "layout":
                    layouts.Add(ParseLayout(context, child));
                    break;
                case "feature":
                    features.Add(ParseFeature(context, child));
                    break;
                default:
                    context.Error($"Unexpected '{LineText.FirstWord(child.Content)}' in module body - expected description, layout or feature", child.Location);
                    context.SkipBlock(child.Indent);
                    break;
            }
        }

        return new(name, layouts, features, line.Location, description);
    }

    static LayoutSyntax ParseLayout(ParserContext context, SourceLine line)
    {
        var name = line.Content["layout".Length..].Trim();
        var slots = new List<string>();

        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (child.Content == "template")
            {
                while (context.TryPeekChild(child.Indent, out var slot))
                {
                    context.Reader.TakeSignificant();
                    if (EnumValueRegex().IsMatch(slot.Content))
                    {
                        slots.Add(slot.Content);
                    }
                    else
                    {
                        context.Error($"Invalid slot name '{slot.Content}' - expected an identifier", slot.Location);
                    }
                }
            }
            else
            {
                context.Error($"Unexpected '{child.Content}' in layout body - expected 'template'", child.Location);
                context.SkipBlock(child.Indent);
            }
        }

        return new(name, slots, line.Location);
    }

    static FeatureSyntax ParseFeature(ParserContext context, SourceLine line)
    {
        var match = FeatureRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error($"Invalid feature declaration '{line.Content}' - expected 'feature <Name>'", line.Location);
        }

        var name = match.Groups[1].Value;
        string? description = null;
        var features = new List<FeatureSyntax>();
        var slices = new List<SliceSyntax>();

        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(child.Content))
            {
                case "description":
                    description = DescriptionParser.Parse(context, child, description, $"Feature '{name}'");
                    break;
                case "feature":
                    features.Add(ParseFeature(context, child));
                    break;
                case "slice":
                    slices.Add(SliceParser.Parse(context, child));
                    break;
                default:
                    context.Error($"Unexpected '{LineText.FirstWord(child.Content)}' in feature body - expected description, feature or slice", child.Location);
                    context.SkipBlock(child.Indent);
                    break;
            }
        }

        return new(name, features, slices, line.Location, description);
    }

    [GeneratedRegex(@"^domain\s+([A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)$", RegexOptions.None, 1000)]
    private static partial Regex DomainRegex();

    [GeneratedRegex(@"^import\s+([\w.]+)$", RegexOptions.None, 1000)]
    private static partial Regex ImportRegex();

    [GeneratedRegex(@"^concept\s+(\w+)\s*:\s*(\w+)((?:\s+@\w+)*)$", RegexOptions.None, 1000)]
    private static partial Regex ConceptRegex();

    [GeneratedRegex(@"^[a-z_]\w*$", RegexOptions.None, 1000)]
    private static partial Regex EnumValueRegex();

    [GeneratedRegex(@"^module\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex ModuleRegex();

    [GeneratedRegex(@"^feature\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex FeatureRegex();

    [GeneratedRegex(@"^[ ]*\t", RegexOptions.None, 1000)]
    private static partial Regex TabIndentRegex();
}
