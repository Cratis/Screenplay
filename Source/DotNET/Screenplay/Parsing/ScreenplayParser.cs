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
        var personas = new List<PersonaSyntax>();
        var modules = new List<ModuleSyntax>();
        var seeds = new List<SeedSyntax>();

        while (context.Reader.PeekSignificant() is { } line)
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(line.Content))
            {
                case "domain":
                    domain = ParseDomain(context, line, domain, imports.Count > 0 || concepts.Count > 0 || policies.Count > 0 || personas.Count > 0 || modules.Count > 0 || seeds.Count > 0);
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
                case "persona":
                    personas.Add(ParsePersona(context, line));
                    break;
                case "module":
                    modules.Add(ParseModule(context, line));
                    break;
                case "seed":
                    seeds.Add(SeedParser.Parse(context, line));
                    break;
                default:
                    context.Error($"Unexpected '{LineText.FirstWord(line.Content)}' at the top level - expected domain, import, concept, policy, persona, module or seed", line.Location);
                    context.SkipBlock(line.Indent);
                    break;
            }
        }

        return new(imports, concepts, policies, modules, SourceLocation.Start, domain, personas, seeds);
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

        if (type != "Enum" && !ConceptSyntax.PrimitiveTypes.Contains(type))
        {
            context.Error($"Unknown primitive type '{type}' - expected {string.Join(", ", ConceptSyntax.PrimitiveTypes)} or Enum", line.Location);
        }

        var values = new List<string>();
        var validations = new List<ValidateSyntax>();
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(child.Content) == "validate")
            {
                if (ValidateParser.Parse(context, child, impliedSubject: true) is { } validate)
                {
                    validations.Add(validate);
                }
            }
            else if (type == "Enum" && EnumValueRegex().IsMatch(child.Content))
            {
                values.Add(child.Content);
            }
            else if (type == "Enum")
            {
                context.Error($"Invalid enum value '{child.Content}' - expected an identifier", child.Location);
            }
            else
            {
                context.Error($"Unexpected '{child.Content}' in concept body - expected validate", child.Location);
                context.SkipBlock(child.Indent);
            }
        }

        return new(name, type, attributes, values, line.Location, validations);
    }

    static PersonaSyntax ParsePersona(ParserContext context, SourceLine line)
    {
        var match = PersonaRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error($"Invalid persona declaration '{line.Content}' - expected 'persona <Name>'", line.Location);
        }

        var name = match.Groups[1].Value;
        string? description = null;
        var policies = new List<string>();

        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(child.Content))
            {
                case "description":
                    description = DescriptionParser.Parse(context, child, description, $"Persona '{name}'");
                    break;
                case "policy":
                    if (PersonaPolicyRegex().Match(child.Content) is { Success: true } policy)
                    {
                        policies.Add(policy.Groups[1].Value);
                    }
                    else
                    {
                        context.Error($"Invalid policy reference '{child.Content}' - expected 'policy <Name>'", child.Location);
                    }

                    break;
                default:
                    context.Error($"Unexpected '{LineText.FirstWord(child.Content)}' in persona body - expected description or policy", child.Location);
                    context.SkipBlock(child.Indent);
                    break;
            }
        }

        return new(name, description, policies, line.Location);
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

    [GeneratedRegex(@"^persona\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex PersonaRegex();

    [GeneratedRegex(@"^policy\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex PersonaPolicyRegex();

    [GeneratedRegex(@"^module\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex ModuleRegex();

    [GeneratedRegex(@"^feature\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex FeatureRegex();

    [GeneratedRegex(@"^[ ]*\t", RegexOptions.None, 1000)]
    private static partial Regex TabIndentRegex();
}
