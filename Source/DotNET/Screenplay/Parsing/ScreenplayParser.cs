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
    static readonly HashSet<string> _scopeKeywords =
    [
        with(StringComparer.Ordinal),
        "slice", "feature", "description", "documentation", "authorize", "available", "screen", "dialog", "form", "contribute", "on", "uses"
    ];

    // The fences that open a code block whatever languages are configured.
    static readonly HashSet<string> _plainFences =
    [
        with(StringComparer.Ordinal),
        "```", "```text", "```markdown"
    ];

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
        var state = new RootBodyState(placement ?? PlayPlacement.Document);
        while (context.Reader.PeekSignificant() is { } line)
        {
            context.Reader.TakeSignificant();
            DispatchRoot(context, line, state);
        }

        return SinkResolution.Resolve(state.Build(context), context);
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

    static void SkipDiscoveryBlock(ParserContext context, int parentIndent)
    {
        while (context.TryPeekChild(parentIndent, out var child))
        {
            context.Reader.TakeSignificant();
            if (OpensFencedBlock(context, child.Content))
            {
                _ = CodeBlockParser.ParseFencedBody(context, child.Content[3..], child);
            }
            else if (context.Languages.InlineLanguages.Contains(child.Content))
            {
                _ = CodeBlockParser.Parse(context, child);
            }
        }
    }

    static bool OpensFencedBlock(ParserContext context, string content) =>
        content.StartsWith("```", StringComparison.Ordinal) && (_plainFences.Contains(content) || context.Languages.InlineLanguages.Contains(content[3..]));

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
                : " - expected domain, import, concept, type, policy, audience, availability, persona, sticky note, authentication, onboarding, identity, context, module, seed, trigger, behavior, ui profile, design system, theme, sample data, layout, statemachine or workflow";
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

    static void AddDesignSystem(ParserContext context, DesignSystemSyntax designSystem, List<DesignSystemSyntax> designSystems)
    {
        if (designSystems.Count > 0)
        {
            context.Error(DiagnosticCodes.DuplicateDesignSystem, $"The design system '{designSystem.Name}' is a second one - an application has exactly one design system, '{designSystems[0].Name}'", designSystem.Location);
            return;
        }

        designSystems.Add(designSystem);
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

    static UserSyntax ParseUser(ParserContext context, SourceLine line)
    {
        var match = UserRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidUserDeclaration, $"Invalid user declaration '{line.Content}' - expected 'user <name> [\"<display name>\"] [as <Persona>, ...]'", line.Location);
            context.SkipBlock(line.Indent);
            return new(LineText.FirstWord(line.Content), null, [], line.Location);
        }

        // A user is one line: its body would have nothing to say, so a nested line is reported instead of silently dropped.
        if (context.TryPeekChild(line.Indent, out var child))
        {
            context.Error(DiagnosticCodes.InvalidUserDeclaration, $"A user is declared on one line; '{child.Content}' has no place under it", child.Location);
            context.SkipBlock(line.Indent);
        }

        var displayName = match.Groups[2].Success ? StringLiteral.Unescape(match.Groups[2].Value) : null;
        var personas = match.Groups[3].Success
            ? match.Groups[3].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            : [];
        return new(match.Groups[1].Value, displayName, personas, line.Location);
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

    [GeneratedRegex(@"^audience\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex PersonaAudienceRegex();

    [GeneratedRegex(@"^user\s+([A-Za-z_]\w*)(?:\s+""(" + StringLiteral.BodyPattern + @")"")?(?:\s+as\s+([A-Za-z_]\w*(?:\s*,\s*[A-Za-z_]\w*)*))?$", RegexOptions.NonBacktracking)]
    private static partial Regex UserRegex();

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
