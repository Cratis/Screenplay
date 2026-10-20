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
        "slice", "feature", "description", "documentation", "authorize", "screen", "dialog", "form", "contribute", "on", "uses"
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
        context.DeferIdentityValidation = true;
        WarnOnTabIndentation(context, lines);
        placement ??= PlayPlacement.Document;
        var moduleBody = placement.Scope.Count == 1 ? new ModuleBody(placement.Scope[0]) : null;
        var featureBody = placement.Scope.Count > 1 ? new FeatureBody(placement.Scope[^1]) : null;
        var fileImports = new List<FileImportSyntax>();

        DomainSyntax? domain = null;
        AuthenticationSyntax? authentication = null;
        IdentitySyntax? identity = null;
        ReactionProductionMode? reactionProductionMode = null;
        var sawReactionBatches = false;
        CommandReadMode? commandReadMode = null;
        var sawCommandReads = false;
        var imports = new List<ImportSyntax>();
        var concepts = new List<ConceptSyntax>();
        var types = new List<TypeSyntax>();
        var policies = new List<PolicySyntax>();
        var purposes = new List<PurposeSyntax>();
        var personas = new List<PersonaSyntax>();
        var stickyNotes = new List<StickyNoteSyntax>();
        var modules = new List<ModuleSyntax>();
        var seeds = new List<SeedSyntax>();
        var uiProfiles = new List<UiProfileSyntax>();
        var themes = new List<ThemeSyntax>();
        var sampleDataPacks = new List<SampleDataPackSyntax>();
        var triggers = new List<TriggerSyntax>();
        var layouts = new List<LayoutSyntax>();
        var behaviors = new List<BehaviorSyntax>();
        var systems = new List<SystemSyntax>();
        var eventSources = new List<EventSourceSyntax>();
        var examples = new List<SpecificationExampleSyntax>();
        var templates = new List<TemplateAssignmentSyntax>();
        var exposures = new List<ExposureSyntax>();
        var instanceContributions = new List<InstanceContributionsSyntax>();

        while (context.Reader.PeekSignificant() is { } line)
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(line.Content))
            {
                case "command" when CommandReadsParser.IsDeclaration(line.Content):
                    if (sawCommandReads || !placement.IsDocument || !CommandReadsParser.IsProtected(line.Content))
                    {
                        context.Error(DiagnosticCodes.InvalidCommandReadsDeclaration, "Declare 'command reads protected' once at the application root.", line.Location);
                    }
                    else
                    {
                        commandReadMode = CommandReadMode.Protected;
                    }
                    sawCommandReads = true;
                    break;
                case "reaction":
                    if (sawReactionBatches || !placement.IsDocument || !ReactionBatchesParser.IsAtomic(line.Content))
                    {
                        context.Error(DiagnosticCodes.InvalidReactionBatchesDeclaration, "Declare 'reaction batches atomic' once at the application root.", line.Location);
                    }
                    else
                    {
                        reactionProductionMode = ReactionProductionMode.Atomic;
                    }
                    sawReactionBatches = true;
                    break;
                case "domain":
                    domain = ParseDomain(context, line, domain, imports.Count > 0 || concepts.Count > 0 || types.Count > 0 || policies.Count > 0 || purposes.Count > 0 || personas.Count > 0 || modules.Count > 0 || seeds.Count > 0 || authentication is not null || identity is not null || uiProfiles.Count > 0 || themes.Count > 0 || triggers.Count > 0 || layouts.Count > 0 || systems.Count > 0 || eventSources.Count > 0 || examples.Count > 0);
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
                        var origin = import.Groups["origin"].Success ? StringLiteral.Unescape(import.Groups["origin"].Value) : null;
                        if (origin is not null && string.IsNullOrWhiteSpace(origin))
                        {
                            context.Error(DiagnosticCodes.InvalidImportDeclaration, "An import origin must be a nonblank quoted string", line.Location);
                        }

                        imports.Add(new(import.Groups[1].Value, line.Location)
                        {
                            Origin = origin,
                            Visibility = origin is null ? EventVisibility.Private : EventVisibility.Public
                        });
                    }
                    else
                    {
                        context.Error(DiagnosticCodes.InvalidImportDeclaration, $"Invalid import '{line.Content}' - expected 'import <Qualified.Name> [from \"<origin>\"]'", line.Location);
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
                case "purpose" when moduleBody is not null || featureBody is not null:
                    _ = moduleBody?.TryParse(context, line) ?? featureBody!.TryParse(context, line);
                    break;
                case "purpose":
                    purposes.Add(PurposeParser.Parse(context, line));
                    break;
                case "policy":
                    policies.Add(PolicyParser.Parse(context, line));
                    break;
                case "persona":
                    personas.Add(ParsePersona(context, line));
                    break;
                case "sticky":
                    if (ParseStickyNote(context, line) is { } stickyNote)
                    {
                        stickyNotes.Add(stickyNote);
                    }

                    break;
                case "identity":
                    identity = IdentityParser.Parse(context, line, identity);
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
                case "sample":
                    AddSampleDataPack(context, SampleDataPackParser.Parse(context, line), sampleDataPacks);
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
                case "template":
                    if (TemplateAssignmentParser.Parse(context, line) is { } template)
                    {
                        templates.Add(template);
                    }

                    break;
                case "behavior":
                    AddBehavior(context, InteractionParser.ParseBehavior(context, line), behaviors);
                    break;
                case "exposure":
                    exposures.Add(CompositionParser.ParseExposure(context, line));
                    break;
                case "instance":
                    instanceContributions.Add(CompositionParser.ParseInstance(context, line));
                    break;
                case "runs":
                    ReactionIdentityParser.Misplaced(context, line);
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
            Identity = identity,
            ReactionProductionMode = reactionProductionMode,
            CommandReadMode = commandReadMode,
            SourceOptions = context.SourceOptions,
            Purposes = purposes,
            StickyNotes = stickyNotes,
            Examples = examples,
            Systems = systems,
            SampleDataPacks = sampleDataPacks,
            EventSources = eventSources,
            Behaviors = behaviors,
            FileImports = fileImports,
            Templates = templates,
            Exposures = exposures,
            InstanceContributions = instanceContributions
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
                : " - expected domain, import, concept, type, policy, persona, sticky note, authentication, identity, module, seed, trigger, behavior, ui profile, theme, sample data or layout";
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

    static void AddSampleDataPack(ParserContext context, SampleDataPackSyntax pack, List<SampleDataPackSyntax> packs)
    {
        if (packs.Exists(existing => existing.Name == pack.Name))
        {
            context.Error(DiagnosticCodes.DuplicateSampleDataPack, $"A sample data pack named '{pack.Name}' is already declared - pack names must be unique", pack.Location);
            return;
        }

        packs.Add(pack);
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
        var attributes = ConceptComplianceParser.ParseMarkers(context, line, name, match.Groups[3].Value);

        if (type != "Enum" && !ConceptSyntax.PrimitiveTypes.Contains(type))
        {
            context.Error(DiagnosticCodes.UnknownPrimitiveType, $"Unknown primitive type '{type}' - expected {string.Join(", ", ConceptSyntax.PrimitiveTypes)} or Enum", line.Location);
        }

        var values = new List<string>();
        var members = new EnumerationMembers();
        GenerationSyntax? generation = null;
        var declaredGeneration = false;
        string? description = null;
        var validations = new List<ValidateSyntax>();
        var directiveLocations = new Dictionary<string, SourceLocation>();
        FileReferenceSyntax? file = null;
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (type == "Enum" && members.TryNumbered(context, child, out var memberName))
            {
                values.Add(memberName);
                directiveLocations[DirectiveLocationKeys.ForValue("value", values, values.Count - 1)] = child.Location;
                continue;
            }

            if (DescriptionParser.TryParse(context, child, ref description, $"Concept '{name}'", directiveLocations, type == "Enum")) continue;
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
            else if (LineText.FirstWord(child.Content) == "generate" && !(type == "Enum" && child.Content == "generate"))
            {
                if (declaredGeneration) context.Error(DiagnosticCodes.DuplicateGeneration, $"Concept '{name}' declares generation more than once.", child.Location);
                var parsed = GenerationParser.Parse(context, child, type);
                if (!declaredGeneration)
                {
                    generation = parsed;
                    directiveLocations["generate"] = child.Location;
                }

                declaredGeneration = true;
            }
            else if (ConceptComplianceParser.TryParseDirective(context, child, name, attributes, directiveLocations))
            {
                // Compliance directives preserve their original source anchors for trivia and repairs.
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
                context.Error(DiagnosticCodes.UnknownConceptDirective, $"Unexpected '{child.Content}' in concept body - expected validate, generate, 'file <path>' or '<attribute> reason \"<text>\"'", child.Location);
                context.SkipBlock(child.Indent);
            }
        }

        return new(name, type, attributes, values, line.Location, validations) { Generation = generation, Description = description, File = file, ValueNumbers = members.Finish(context, line, values.Count), DirectiveLocations = directiveLocations };
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
        var goals = new List<string>();
        (int X, int Y)? position = null;
        (int Width, int Height)? size = null;
        var directiveLocations = new Dictionary<string, SourceLocation>();

        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(child.Content))
            {
                case "goal":
                    if (PersonaGoalRegex().Match(child.Content) is { Success: true } goal)
                    {
                        goals.Add(StringLiteral.Unescape(goal.Groups[1].Value));
                        directiveLocations[DirectiveLocationKeys.ForValue("goal", goals, goals.Count - 1)] = child.Location;
                    }
                    else
                    {
                        context.Error(DiagnosticCodes.InvalidPersonaDeclaration, $"Invalid goal '{child.Content}' - expected 'goal \"<text>\"'", child.Location);
                    }

                    break;
                case "position":
                    position = ParseCanvasPair(context, child, "position", "x", "y", position, allowNegative: true) ?? position;
                    break;
                case "size":
                    size = ParseCanvasPair(context, child, "size", "width", "height", size, allowNegative: false) ?? size;
                    break;
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
                    context.Error(DiagnosticCodes.UnknownPersonaDirective, $"Unexpected '{LineText.FirstWord(child.Content)}' in persona body - expected description, goal, policy, position or size", child.Location);
                    context.SkipBlock(child.Indent);
                    break;
            }
        }

        return new(name, description, policies, line.Location)
        {
            DirectiveLocations = directiveLocations,
            Goals = goals,
            X = position?.X,
            Y = position?.Y,
            Width = size?.Width,
            Height = size?.Height
        };
    }

    // 'sticky note on <canvas>' with the text it carries, an optional colour and where it sits on that canvas.
    static StickyNoteSyntax? ParseStickyNote(ParserContext context, SourceLine line)
    {
        var canvasMatch = StickyNoteRegex().Match(line.Content);
        if (!canvasMatch.Success)
        {
            context.Error(DiagnosticCodes.InvalidPersonaDeclaration, $"Invalid sticky note declaration '{line.Content}' - expected 'sticky note on personas', 'sticky note on eventsources', 'sticky note on givencontexts' or 'sticky note on seeddata'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        string? text = null;
        string? colour = null;
        (int X, int Y)? position = null;
        (int Width, int Height)? size = null;
        var directiveLocations = new Dictionary<string, SourceLocation>();

        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(child.Content))
            {
                case "description":
                    var previous = text;
                    text = DescriptionParser.Parse(context, child, text, "A sticky note");
                    if (previous is null && text is not null)
                    {
                        directiveLocations["description"] = child.Location;
                    }

                    break;
                case "colour":
                    if (StickyNoteColourRegex().Match(child.Content) is { Success: true } colourMatch)
                    {
                        colour = colourMatch.Groups[1].Value;
                    }
                    else
                    {
                        context.Error(DiagnosticCodes.InvalidPersonaDeclaration, $"Invalid colour '{child.Content}' - expected 'colour yellow', 'colour pink', 'colour green' or 'colour blue'", child.Location);
                    }

                    break;
                case "position":
                    position = ParseCanvasPair(context, child, "position", "x", "y", position, allowNegative: true) ?? position;
                    break;
                case "size":
                    size = ParseCanvasPair(context, child, "size", "width", "height", size, allowNegative: false) ?? size;
                    break;
                default:
                    context.Error(DiagnosticCodes.UnknownPersonaDirective, $"Unexpected '{LineText.FirstWord(child.Content)}' in sticky note body - expected description, colour, position or size", child.Location);
                    context.SkipBlock(child.Indent);
                    break;
            }
        }

        if (text is null || position is null)
        {
            context.Error(DiagnosticCodes.InvalidPersonaDeclaration, "A sticky note needs a description (the text on it) and a position", line.Location);
            return null;
        }

        return new(canvasMatch.Groups[1].Value, text, colour, position.Value.X, position.Value.Y, size?.Width, size?.Height, line.Location) { DirectiveLocations = directiveLocations };
    }

    // 'position <x> <y>' or 'size <width> <height>' - two whole numbers in canvas units; a second line is a mistake.
    internal static (int First, int Second)? ParseCanvasPair(ParserContext context, SourceLine line, string keyword, string first, string second, (int, int)? existing, bool allowNegative)
    {
        var match = CanvasPairRegex().Match(line.Content);
        if (!match.Success
            || match.Groups[1].Value != keyword
            || !int.TryParse(match.Groups[2].Value, out var firstValue)
            || !int.TryParse(match.Groups[3].Value, out var secondValue)
            || (!allowNegative && (firstValue < 0 || secondValue < 0)))
        {
            context.Error(DiagnosticCodes.InvalidPersonaDeclaration, $"Invalid {keyword} '{line.Content}' - expected '{keyword} <{first}> <{second}>' with whole numbers{(allowNegative ? string.Empty : " that are not negative")}", line.Location);
            return null;
        }

        if (existing is not null)
        {
            context.Error(DiagnosticCodes.InvalidPersonaDeclaration, $"The {keyword} is already declared - at most one is allowed", line.Location);
            return null;
        }

        return (firstValue, secondValue);
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

    [GeneratedRegex(@"^domain\s+([A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)$", RegexOptions.NonBacktracking)]
    private static partial Regex DomainRegex();

    [GeneratedRegex(@"^import\s+([\w.]+)(?:\s+from\s+""(?<origin>" + StringLiteral.BodyPattern + @")"")?$", RegexOptions.NonBacktracking)]
    private static partial Regex ImportRegex();

    [GeneratedRegex(@"^concept\s+(\w+)\s*:\s*(\w+)((?:\s+@?\w+)*)$", RegexOptions.NonBacktracking)]
    private static partial Regex ConceptRegex();

    [GeneratedRegex(@"^@?[a-z_]\w*$", RegexOptions.NonBacktracking)]
    private static partial Regex EnumValueRegex();

    [GeneratedRegex(@"^persona\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex PersonaRegex();

    [GeneratedRegex(@"^policy\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex PersonaPolicyRegex();

    [GeneratedRegex(@"^goal\s+""(" + StringLiteral.BodyPattern + @")""$", RegexOptions.NonBacktracking)]
    private static partial Regex PersonaGoalRegex();

    [GeneratedRegex(@"^sticky\s+note\s+on\s+(personas|eventsources|givencontexts|seeddata)$", RegexOptions.NonBacktracking)]
    private static partial Regex StickyNoteRegex();

    [GeneratedRegex(@"^colour\s+(yellow|pink|green|blue)$", RegexOptions.NonBacktracking)]
    private static partial Regex StickyNoteColourRegex();

    [GeneratedRegex(@"^(position|size)\s+(-?\d+)\s+(-?\d+)$", RegexOptions.NonBacktracking)]
    private static partial Regex CanvasPairRegex();

    [GeneratedRegex(@"^module\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex ModuleRegex();

    [GeneratedRegex(@"^feature\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex FeatureRegex();

    [GeneratedRegex(@"^[ ]*\t", RegexOptions.NonBacktracking)]
    private static partial Regex TabIndentRegex();
}
