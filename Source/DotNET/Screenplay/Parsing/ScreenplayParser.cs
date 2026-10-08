// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses a full Screenplay document into an <see cref="ApplicationSyntax"/>.
/// </summary>
internal static partial class ScreenplayParser
{
    // The words that open something belonging in a module or feature body, which a document's top level cannot hold.
    static readonly HashSet<string> _scopeKeywords = new(StringComparer.Ordinal)
    {
        "slice", "feature", "description", "authorize", "screen", "dialog", "form", "contribute", "on", "uses"
    };

    /// <summary>
    /// Parses a document.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="lines">All <see cref="SourceLine">lines</see> of the document, for whole document checks.</param>
    /// <param name="placement">Where the document's top level belongs - the application unless an import placed it in a module or feature.</param>
    /// <returns>The parsed <see cref="ApplicationSyntax"/>.</returns>
    public static ApplicationSyntax Parse(ParserContext context, IReadOnlyList<SourceLine> lines, PlayPlacement? placement = null)
    {
        WarnOnTabIndentation(context, lines);
        placement ??= PlayPlacement.Document;
        var moduleBody = placement.Scope.Count == 1 ? new ModuleBody(placement.Scope[0]) : null;
        var featureBody = placement.Scope.Count > 1 ? new FeatureBody(placement.Scope[^1]) : null;
        var fileImports = new List<FileImportSyntax>();

        DomainSyntax? domain = null;
        AuthenticationSyntax? authentication = null;
        var imports = new List<ImportSyntax>();
        var concepts = new List<ConceptSyntax>();
        var types = new List<TypeSyntax>();
        var policies = new List<PolicySyntax>();
        var personas = new List<PersonaSyntax>();
        var modules = new List<ModuleSyntax>();
        var seeds = new List<SeedSyntax>();
        var uiProfiles = new List<UiProfileSyntax>();
        var themes = new List<ThemeSyntax>();
        var triggers = new List<TriggerSyntax>();
        var layouts = new List<LayoutSyntax>();
        var behaviors = new List<BehaviorSyntax>();
        var systems = new List<SystemSyntax>();
        var eventSources = new List<EventSourceSyntax>();
        var examples = new List<SpecificationExampleSyntax>();

        while (context.Reader.PeekSignificant() is { } line)
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(line.Content))
            {
                case "domain":
                    domain = ParseDomain(context, line, domain, imports.Count > 0 || concepts.Count > 0 || types.Count > 0 || policies.Count > 0 || personas.Count > 0 || modules.Count > 0 || seeds.Count > 0 || authentication is not null || uiProfiles.Count > 0 || themes.Count > 0 || triggers.Count > 0 || layouts.Count > 0 || systems.Count > 0 || eventSources.Count > 0 || examples.Count > 0);
                    break;
                case "import" when FileImportParser.IsFileImport(line.Content):
                    // A top level import belongs to whatever the document's top level is - the application, or the
                    // module or feature the document was itself imported into.
                    if (moduleBody is not null || featureBody is not null)
                    {
                        _ = moduleBody?.TryParse(context, line) ?? featureBody!.TryParse(context, line);
                    }
                    else
                    {
                        FileImportParser.Parse(context, line, fileImports);
                    }

                    break;
                case "import":
                    if (ImportRegex().Match(line.Content) is { Success: true } import)
                    {
                        imports.Add(new(import.Groups[1].Value, line.Location));
                    }
                    else
                    {
                        context.Error(DiagnosticCodes.InvalidImportDeclaration, $"Invalid import '{line.Content}' - expected 'import <Qualified.Name>'", line.Location);
                    }

                    break;
                case "example":
                    if (moduleBody is not null || featureBody is not null)
                    {
                        _ = moduleBody?.TryParse(context, line) ?? featureBody!.TryParse(context, line);
                    }
                    else
                    {
                        examples.Add(SpecificationParser.ParseExample(context, line));
                    }
                    break;
                case "eventsource":
                    eventSources.Add(EventSourceParser.Parse(context, line));
                    break;
                case "system":
                    systems.Add(OperationParser.ParseSystem(context, line));
                    break;
                case "concept":
                    concepts.Add(ParseConcept(context, line));
                    break;
                case "type":
                    types.Add(TypeParser.Parse(context, line));
                    break;
                case "policy":
                    policies.Add(PolicyParser.Parse(context, line));
                    break;
                case "persona":
                    personas.Add(ParsePersona(context, line));
                    break;
                case "authentication":
                    authentication = AuthenticationParser.Parse(context, line, authentication);
                    break;
                case "module" when !placement.IsDocument:
                    ParseModuleInPlacedFile(context, line, placement, moduleBody);
                    break;
                case "module":
                    modules.Add(ParseModule(context, line));
                    break;
                case "seed":
                    seeds.Add(SeedParser.Parse(context, line));
                    break;
                case "ui":
                    AddUiProfile(context, UiProfileParser.Parse(context, line), uiProfiles);
                    break;
                case "theme":
                    AddTheme(context, ThemeParser.Parse(context, line), themes);
                    break;
                case "trigger":
                    AddTrigger(context, TriggerParser.Parse(context, line), triggers);
                    break;
                case "layout":
                    AddLayout(context, LayoutParser.ParseLayout(context, line), layouts);
                    break;
                case "behavior":
                    AddBehavior(context, InteractionParser.ParseBehavior(context, line), behaviors);
                    break;
                default:
                    if (moduleBody?.TryParse(context, line) == true || featureBody?.TryParse(context, line) == true)
                    {
                        break;
                    }

                    ReportUnexpectedTopLevel(context, line, placement);
                    break;
            }
        }

        if (moduleBody is not null)
        {
            modules.Insert(0, moduleBody.Build(context.Start, isPlacement: true));
        }
        else if (featureBody is not null)
        {
            modules.Insert(0, Place(placement, featureBody.Build(context.Start, isPlacement: true), context.Start));
        }

        return new(imports, concepts, policies, modules, context.Start, domain, personas, seeds, authentication, types, uiProfiles, themes, triggers, layouts)
        {
            SourceOptions = context.SourceOptions,
            Examples = examples,
            Systems = systems,
            EventSources = eventSources,
            Behaviors = behaviors,
            FileImports = fileImports
        };
    }

    /// <summary>
    /// Finds the files a document imports and where in it each import is written, without settling where the
    /// document itself belongs.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in - its diagnostics are not the document's.</param>
    /// <returns>Each <see cref="DiscoveredFileImport"/>, with the module and feature names around it.</returns>
    /// <remarks>
    /// Where a file belongs depends on what imports it, and what it imports depends on where the imports are
    /// written in it - so this reads the module and feature structure of any file, whether its top level is the
    /// application's or the body of a module or feature it will later be placed in.
    /// </remarks>
    public static IReadOnlyList<DiscoveredFileImport> DiscoverImports(ParserContext context)
    {
        var found = new List<DiscoveredFileImport>();
        while (context.Reader.PeekSignificant() is { } line)
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(line.Content))
            {
                case "import" when FileImportParser.TryParse(line) is { } import:
                    found.Add(new([], false, import));
                    break;
                case "module":
                    var module = ParseModule(context, line);
                    found.AddRange(module.FileImports.Select(import => new DiscoveredFileImport([module.Name], true, import)));
                    found.AddRange(module.Features.SelectMany(feature => ImportsIn(feature, [module.Name], true)));
                    break;
                case "feature":
                    found.AddRange(ImportsIn(ParseFeature(context, line), [], false));
                    break;
                default:
                    SkipDiscoveryBlock(context, line.Indent);
                    break;
            }
        }

        return found;
    }

    /// <summary>
    /// Parses a feature from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="line">The consumed <see cref="SourceLine"/> holding the <c>feature</c> header.</param>
    /// <returns>The parsed <see cref="FeatureSyntax"/>.</returns>
    internal static FeatureSyntax ParseFeature(ParserContext context, SourceLine line)
    {
        var match = FeatureRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidFeatureDeclaration, $"Invalid feature declaration '{line.Content}' - expected 'feature <Name>'", line.Location);
        }

        var body = new FeatureBody(match.Groups[1].Value);
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (!body.TryParse(context, child))
            {
                context.Error(DiagnosticCodes.UnknownFeatureDirective, $"Unexpected '{LineText.FirstWord(child.Content)}' in feature body - expected {FeatureBody.Expected}", child.Location);
                context.SkipBlock(child.Indent);
            }
        }

        return body.Build(line.Location);
    }

    static void SkipDiscoveryBlock(ParserContext context, int parentIndent)
    {
        while (context.TryPeekChild(parentIndent, out var child))
        {
            context.Reader.TakeSignificant();
            if (child.Content.StartsWith("```", StringComparison.Ordinal) &&
                (child.Content == "```" || child.Content == "```text" || child.Content == "```markdown" || context.Languages.InlineLanguages.Contains(child.Content[3..])))
            {
                _ = CodeBlockParser.ParseFencedBody(context, child.Content[3..], child);
            }
            else if (context.Languages.InlineLanguages.Contains(child.Content))
            {
                _ = CodeBlockParser.Parse(context, child);
            }
        }
    }

    static IEnumerable<DiscoveredFileImport> ImportsIn(FeatureSyntax feature, IReadOnlyList<string> outer, bool startsAtModule)
    {
        IReadOnlyList<string> scope = [.. outer, feature.Name];
        return feature.FileImports.Select(import => new DiscoveredFileImport(scope, startsAtModule, import))
            .Concat(feature.Features.SelectMany(nested => ImportsIn(nested, scope, startsAtModule)));
    }

    static void ParseModuleInPlacedFile(ParserContext context, SourceLine line, PlayPlacement placement, ModuleBody? moduleBody)
    {
        var name = ModuleRegex().Match(line.Content).Groups[1].Value;

        // Restating the module a file is placed in says nothing new, so its body simply joins the placement.
        if (moduleBody is not null && string.Equals(name, placement.Scope[0], StringComparison.Ordinal))
        {
            moduleBody.RecordRestatedHeader(line.Location);
            while (context.TryPeekChild(line.Indent, out var child))
            {
                context.Reader.TakeSignificant();
                if (!moduleBody.TryParse(context, child))
                {
                    context.Error(DiagnosticCodes.UnknownModuleDirective, $"Unexpected '{LineText.FirstWord(child.Content)}' in module body - expected {ModuleBody.Expected}", child.Location);
                    context.SkipBlock(child.Indent);
                }
            }

            return;
        }

        context.Error(
            DiagnosticCodes.ModuleInPlacedFile,
            $"This file is imported into {placement.Description}, so it cannot declare module '{name}' - import it at the top level of a document instead",
            line.Location);
        context.SkipBlock(line.Indent);
    }

    static void ReportUnexpectedTopLevel(ParserContext context, SourceLine line, PlayPlacement placement)
    {
        var word = LineText.FirstWord(line.Content);
        if (!placement.IsDocument)
        {
            var expected = placement.Scope.Count == 1 ? ModuleBody.Expected : FeatureBody.Expected;
            context.Error(
                DiagnosticCodes.UnexpectedInPlacedFile,
                $"Unexpected '{word}' in a file imported into {placement.Description} - expected an application declaration or {expected}",
                line.Location);
        }
        else
        {
            var hint = _scopeKeywords.Contains(word)
                ? $" - '{word}' belongs in a module or feature; wrap it in one, or import this file from inside one"
                : " - expected domain, import, concept, type, policy, persona, authentication, module, seed, trigger, behavior, ui profile, theme or layout";
            context.Error(DiagnosticCodes.UnknownTopLevelConstruct, $"Unexpected '{word}' at the top level{hint}", line.Location);
        }

        context.SkipBlock(line.Indent);
    }

    static ModuleSyntax Place(PlayPlacement placement, FeatureSyntax innermost, SourceLocation start)
    {
        var feature = innermost;
        for (var index = placement.Scope.Count - 2; index >= 1; index--)
        {
            feature = new FeatureSyntax(placement.Scope[index], [feature], [], start) { IsPlacement = true };
        }

        return new ModuleSyntax(placement.Scope[0], [], [feature], start) { IsPlacement = true };
    }

    static void AddLayout(ParserContext context, LayoutSyntax layout, List<LayoutSyntax> layouts)
    {
        if (layouts.Exists(existing => existing.Name == layout.Name))
        {
            context.Error(DiagnosticCodes.DuplicateLayout, $"A layout named '{layout.Name}' is already declared - layout names must be unique", layout.Location);
            return;
        }

        layouts.Add(layout);
    }

    static void AddUiProfile(ParserContext context, UiProfileSyntax profile, List<UiProfileSyntax> uiProfiles)
    {
        if (uiProfiles.Exists(existing => existing.Name == profile.Name))
        {
            context.Error(DiagnosticCodes.DuplicateUiProfile, $"A ui profile named '{profile.Name}' is already declared - profile names must be unique", profile.Location);
            return;
        }

        uiProfiles.Add(profile);
    }

    static void AddTheme(ParserContext context, ThemeSyntax theme, List<ThemeSyntax> themes)
    {
        if (themes.Exists(existing => existing.Name == theme.Name))
        {
            context.Error(DiagnosticCodes.DuplicateTheme, $"A theme named '{theme.Name}' is already declared - theme names must be unique", theme.Location);
            return;
        }

        themes.Add(theme);
    }

    static void AddTrigger(ParserContext context, TriggerSyntax trigger, List<TriggerSyntax> triggers)
    {
        if (triggers.Exists(existing => existing.Name == trigger.Name))
        {
            context.Error(DiagnosticCodes.DuplicateTrigger, $"A trigger named '{trigger.Name}' is already declared - trigger names must be unique", trigger.Location);
            return;
        }

        // An application trigger is reachable from an interaction as 'on <Name>'. If its name is also a
        // built-in interaction kind, the built-in wins and the declaration becomes unreachable - so the
        // collision is reported here rather than silently capturing what the author meant.
        if (InteractionParser.IsBuiltInTriggerName(trigger.Name))
        {
            context.Error(
                DiagnosticCodes.ApplicationTriggerCollidesWithInteractionKind,
                $"A trigger named '{trigger.Name}' collides with the built-in interaction kind of the same name - 'on {trigger.Name}' would mean the interaction, never this trigger",
                trigger.Location);
            return;
        }

        triggers.Add(trigger);
    }

    static void AddBehavior(ParserContext context, BehaviorSyntax behavior, List<BehaviorSyntax> behaviors)
    {
        if (behaviors.Exists(existing => existing.Name == behavior.Name))
        {
            context.Error(DiagnosticCodes.DuplicateBehavior, $"A behavior named '{behavior.Name}' is already declared - behavior names must be unique", behavior.Location);
            return;
        }

        behaviors.Add(behavior);
    }

    static DomainSyntax? ParseDomain(ParserContext context, SourceLine line, DomainSyntax? existing, bool hasOtherConstructs)
    {
        var match = DomainRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidDomainDeclaration, $"Invalid domain declaration '{line.Content}' - expected 'domain <Qualified.Name>'", line.Location);
            return existing;
        }

        if (existing is not null)
        {
            context.Error(DiagnosticCodes.DuplicateDomain, "The document already declares a domain - a document can have at most one", line.Location);
            return existing;
        }

        if (hasOtherConstructs)
        {
            context.Error(DiagnosticCodes.DomainNotFirst, "'domain' must be declared before any other construct", line.Location);
        }

        return new(match.Groups[1].Value, line.Location);
    }

    static void WarnOnTabIndentation(ParserContext context, IReadOnlyList<SourceLine> lines)
    {
        var inFence = false;
        foreach (var line in lines)
        {
            if (line.Raw.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                inFence = !inFence;
                continue;
            }

            if (!inFence && TabIndentRegex().IsMatch(line.Raw))
            {
                context.Warning(DiagnosticCodes.TabIndentation, "Screenplay is indentation based - use spaces, not tabs", line.Start);
            }
        }
    }

    static ConceptSyntax ParseConcept(ParserContext context, SourceLine line)
    {
        var match = ConceptRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidConceptDeclaration, $"Invalid concept declaration '{line.Content}' - expected 'concept <Name> : <Type>'", line.Location);
            context.SkipBlock(line.Indent);
            return new(LineText.FirstWord(line.Content), string.Empty, [], [], line.Location);
        }

        var name = match.Groups[1].Value;
        var type = match.Groups[2].Value;
        var attributes = match.Groups[3].Value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(attribute => new ConceptAttributeSyntax(attribute.TrimStart('@'), line.Location))
            .ToList();

        if (type != "Enum" && !ConceptSyntax.PrimitiveTypes.Contains(type))
        {
            context.Error(DiagnosticCodes.UnknownPrimitiveType, $"Unknown primitive type '{type}' - expected {string.Join(", ", ConceptSyntax.PrimitiveTypes)} or Enum", line.Location);
        }

        var values = new List<string>();
        var validations = new List<ValidateSyntax>();
        var directiveLocations = new Dictionary<string, SourceLocation>();
        FileReferenceSyntax? file = null;
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (FileReferenceParser.IsDirective(child))
            {
                file = FileReferenceParser.ParseReplacing(context, child, file, directiveLocations);
            }
            else if (LineText.FirstWord(child.Content) == "validate")
            {
                if (type == "Enum" && child.Content == "validate" && !context.TryPeekChild(child.Indent, out _))
                {
                    context.Warning(
                        DiagnosticCodes.ValidateReadAsEnumerationBlock,
                        $"'validate' in enumeration concept '{name}' declares an empty validate block, not a value named 'validate' - write '@validate' for the value",
                        child.Location);
                }

                if (ValidateParser.Parse(context, child, ValidationOwnerKind.Concept) is { } validate)
                {
                    validations.Add(validate);
                }
            }
            else if (AttributeReasonRegex().Match(child.Content) is { Success: true } reason)
            {
                if (ApplyAttributeReason(context, child, name, attributes, reason))
                {
                    directiveLocations[$"reason:{reason.Groups[1].Value}"] = child.Location;
                }
            }
            else if (type == "Enum" && EnumValueRegex().IsMatch(child.Content))
            {
                values.Add(LineText.Unescape(child.Content));
                directiveLocations[DirectiveLocationKeys.ForValue("value", values, values.Count - 1)] = child.Location;
            }
            else if (type == "Enum")
            {
                context.Error(DiagnosticCodes.InvalidEnumerationValue, $"Invalid enum value '{child.Content}' - expected an identifier", child.Location);
            }
            else
            {
                context.Error(DiagnosticCodes.UnknownConceptDirective, $"Unexpected '{child.Content}' in concept body - expected validate, 'file <path>' or '<attribute> reason \"<text>\"'", child.Location);
                context.SkipBlock(child.Indent);
            }
        }

        return new(name, type, attributes, values, line.Location, validations) { File = file, DirectiveLocations = directiveLocations };
    }

    static bool ApplyAttributeReason(
        ParserContext context,
        SourceLine line,
        string concept,
        List<ConceptAttributeSyntax> attributes,
        Match reason)
    {
        var attribute = reason.Groups[1].Value;
        var index = attributes.FindIndex(candidate => candidate.Name == attribute);
        if (index < 0)
        {
            context.Error(DiagnosticCodes.AttributeReasonWithoutAttribute, $"Concept '{concept}' declares a reason for '{attribute}' without the attribute - write 'concept {concept} : <Type> @{attribute}'", line.Location);
            return false;
        }

        if (attributes[index].Reason is not null)
        {
            context.Error(DiagnosticCodes.DuplicateAttributeReason, $"Concept '{concept}' already declares a reason for '{attribute}' - at most one is allowed", line.Location);
            return false;
        }

        attributes[index] = attributes[index] with { Reason = StringLiteral.Unescape(reason.Groups[2].Value) };
        return true;
    }

    static PersonaSyntax ParsePersona(ParserContext context, SourceLine line)
    {
        var match = PersonaRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidPersonaDeclaration, $"Invalid persona declaration '{line.Content}' - expected 'persona <Name>'", line.Location);
        }

        var name = match.Groups[1].Value;
        string? description = null;
        var policies = new List<string>();
        var directiveLocations = new Dictionary<string, SourceLocation>();

        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(child.Content))
            {
                case "description":
                    var previousDescription = description;
                    description = DescriptionParser.Parse(context, child, description, $"Persona '{name}'");
                    if (previousDescription is null && description is not null)
                    {
                        directiveLocations["description"] = child.Location;
                    }

                    break;
                case "policy":
                    if (PersonaPolicyRegex().Match(child.Content) is { Success: true } policy)
                    {
                        policies.Add(policy.Groups[1].Value);
                        directiveLocations[DirectiveLocationKeys.ForValue("policy", policies, policies.Count - 1)] = child.Location;
                    }
                    else
                    {
                        context.Error(DiagnosticCodes.InvalidPersonaPolicyReference, $"Invalid policy reference '{child.Content}' - expected 'policy <Name>'", child.Location);
                    }

                    break;
                default:
                    context.Error(DiagnosticCodes.UnknownPersonaDirective, $"Unexpected '{LineText.FirstWord(child.Content)}' in persona body - expected description or policy", child.Location);
                    context.SkipBlock(child.Indent);
                    break;
            }
        }

        return new(name, description, policies, line.Location) { DirectiveLocations = directiveLocations };
    }

    static ModuleSyntax ParseModule(ParserContext context, SourceLine line)
    {
        var match = ModuleRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidModuleDeclaration, $"Invalid module declaration '{line.Content}' - expected 'module <Name>'", line.Location);
        }

        var body = new ModuleBody(match.Groups[1].Value);
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (!body.TryParse(context, child))
            {
                context.Error(DiagnosticCodes.UnknownModuleDirective, $"Unexpected '{LineText.FirstWord(child.Content)}' in module body - expected {ModuleBody.Expected}", child.Location);
                context.SkipBlock(child.Indent);
            }
        }

        return body.Build(line.Location);
    }

    [GeneratedRegex(@"^domain\s+([A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)$", RegexOptions.None, 1000)]
    private static partial Regex DomainRegex();

    [GeneratedRegex(@"^import\s+([\w.]+)$", RegexOptions.None, 1000)]
    private static partial Regex ImportRegex();

    [GeneratedRegex(@"^concept\s+(\w+)\s*:\s*(\w+)((?:\s+@\w+)*)$", RegexOptions.None, 1000)]
    private static partial Regex ConceptRegex();

    [GeneratedRegex(@"^@?[a-z_]\w*$", RegexOptions.None, 1000)]
    private static partial Regex EnumValueRegex();

    [GeneratedRegex(@"^([a-z_]\w*)\s+reason\s+""(" + StringLiteral.BodyPattern + @")""$", RegexOptions.None, 1000)]
    private static partial Regex AttributeReasonRegex();

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
