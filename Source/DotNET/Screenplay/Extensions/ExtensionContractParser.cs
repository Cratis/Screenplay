// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Extensions;

/// <summary>
/// Parses and validates the <c>contract.play</c> file of an extension package.
/// </summary>
public static partial class ExtensionContractParser
{
    static readonly HashSet<string> _declaredOnce = new(StringComparer.Ordinal) { "display", "icon", "backend", "frontend", "activation", "description" };

    /// <summary>
    /// Parses the text of a contract file.
    /// </summary>
    /// <param name="source">The contract text.</param>
    /// <param name="path">The path to attribute locations to.</param>
    /// <returns>The <see cref="CompilationResult{T}"/> holding the contract and its diagnostics.</returns>
    public static CompilationResult<ExtensionContract> Parse(string source, string? path = null)
    {
        var lines = SourceLineSplitter.Split(source, hashComments: false);
        var context = SourceOptionsParser.Create(lines, path: path);
        ExtensionContract? contract = null;
        while (context.Reader.PeekSignificant() is { } line)
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(line.Content) == "extension" && contract is null)
            {
                contract = ParseExtension(context, line);
            }
            else
            {
                context.Error(DiagnosticCodes.InvalidExtensionContract, contract is null ? "A contract file starts with 'extension <Name>'." : "A contract file holds exactly one 'extension' declaration.", line.Location);
                context.SkipBlock(line.Indent);
            }
        }

        if (contract is null && context.Diagnostics.Count == 0) context.Error(DiagnosticCodes.InvalidExtensionContract, "A contract file must declare 'extension <Name>'.", SourceLocation.Start.In(path));
        if (contract is not null) Validate(contract, context);
        return new(contract, context.Diagnostics);
    }

    static ExtensionContract ParseExtension(ParserContext context, SourceLine header)
    {
        var match = ExtensionHeaderRegex().Match(header.Content);
        if (!match.Success) context.Error(DiagnosticCodes.InvalidExtensionContract, "Expected 'extension <Name>'.", header.Location);
        var contract = new ExtensionContract(match.Success ? match.Groups[1].Value : string.Empty, header.Location);
        var images = ImmutableArray.CreateBuilder<ExtensionImage>();
        var requirements = ImmutableArray.CreateBuilder<ExtensionRequirement>();
        var settings = ImmutableArray.CreateBuilder<ExtensionSetting>();
        var abilities = ImmutableArray.CreateBuilder<AbilitySyntax>();
        var triggers = ImmutableArray.CreateBuilder<ExtensionTrigger>();
        var components = ImmutableArray.CreateBuilder<ExtensionComponent>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            var word = LineText.FirstWord(line.Content);
            if (_declaredOnce.Contains(word) && !seen.Add(word))
            {
                context.Error(DiagnosticCodes.InvalidExtensionContract, $"A contract declares '{word}' once.", line.Location);
                context.SkipBlock(line.Indent);
                continue;
            }

            switch (word)
            {
                case "display":
                    contract = contract with { DisplayName = OneString(context, line, DisplayRegex(), "display name \"<text>\"") };
                    break;
                case "description":
                    contract = contract with { Description = OneString(context, line, DescriptionRegex(), "description \"<text>\"") };
                    break;
                case "icon":
                    contract = contract with { Icon = OneString(context, line, IconRegex(), "icon \"<path>\"") };
                    break;
                case "image":
                    if (ParseImage(context, line) is { } image) images.Add(image);
                    break;
                case "requires":
                    if (ParseRequirement(context, line) is { } requirement) requirements.Add(requirement);
                    break;
                case "activation":
                    contract = contract with { Activation = SystemConfigurationParser.ParseActivation(context, line) };
                    break;
                case "setting":
                    if (ParseSetting(context, line) is { } setting) settings.Add(setting);
                    break;
                case "ability":
                    abilities.Add(OperationParser.ParseAbility(context, line));
                    break;
                case "trigger":
                    if (ParseTrigger(context, line) is { } trigger) triggers.Add(trigger);
                    break;
                case "component":
                    components.Add(ParseComponent(context, line));
                    break;
                case "backend":
                    contract = contract with { Backend = ParseBackend(context, line) };
                    break;
                case "frontend":
                    contract = contract with { Frontend = OneString(context, line, FrontendRegex(), "frontend \"<path>\"") };
                    break;
                default:
                    context.Error(DiagnosticCodes.InvalidExtensionContract, $"'{word}' is not a contract line; expected display name, description, icon, image, requires, activation, setting, ability, trigger, component, backend or frontend.", line.Location);
                    context.SkipBlock(line.Indent);
                    break;
            }
        }

        return contract with { Images = images.ToImmutable(), Requirements = requirements.ToImmutable(), Settings = settings.ToImmutable(), Abilities = abilities.ToImmutable(), Triggers = triggers.ToImmutable(), Components = components.ToImmutable() };
    }

    static string? OneString(ParserContext context, SourceLine line, Regex regex, string form)
    {
        RejectChildren(context, line);
        var match = regex.Match(line.Content);
        if (match.Success) return StringLiteral.Unescape(match.Groups[1].Value);
        context.Error(DiagnosticCodes.InvalidExtensionContract, $"Expected '{form}'.", line.Location);
        return null;
    }

    static ExtensionImage? ParseImage(ParserContext context, SourceLine line)
    {
        var match = ImageRegex().Match(line.Content);
        string? caption = null;
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (CaptionRegex().Match(child.Content) is { Success: true } captionMatch && caption is null) caption = StringLiteral.Unescape(captionMatch.Groups[1].Value);
            else context.Error(DiagnosticCodes.InvalidExtensionContract, "An image takes one optional 'caption \"<text>\"' line.", child.Location);
            RejectChildren(context, child);
        }

        if (match.Success) return new(StringLiteral.Unescape(match.Groups[1].Value), caption, line.Location);
        context.Error(DiagnosticCodes.InvalidExtensionContract, "Expected 'image \"<path>\"'.", line.Location);
        return null;
    }

    static ExtensionRequirement? ParseRequirement(ParserContext context, SourceLine line)
    {
        RejectChildren(context, line);
        var match = RequiresRegex().Match(line.Content);
        if (!match.Success || !ExtensionVocabulary.RequirementKinds.Contains(match.Groups[1].Value))
        {
            context.Error(DiagnosticCodes.InvalidExtensionContract, "Expected 'requires stage \"<range>\"', 'requires screenplay \"<range>\"', 'requires platform <name>' or 'requires runtime <name> <version>'.", line.Location);
            return null;
        }

        var kind = match.Groups[1].Value;
        var value = StringLiteral.Unescape(match.Groups[2].Value.Trim('"'));
        if (kind == "platform" && !ExtensionVocabulary.Platforms.Contains(value))
        {
            context.Error(DiagnosticCodes.UnknownComponentPlatform, $"'{value}' is not a platform; expected {string.Join(", ", ExtensionVocabulary.Platforms)}.", line.Location);
        }

        return new(kind, value, line.Location);
    }

    static ExtensionSetting? ParseSetting(ParserContext context, SourceLine line)
    {
        RejectChildren(context, line);
        var match = SettingRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidExtensionContract, "Expected 'setting <name> [public | secret]'.", line.Location);
            return null;
        }

        var modifiers = match.Groups[2].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var isPublic = modifiers.Contains("public");
        var isSecret = modifiers.Contains("secret");
        if (modifiers.Except(["public", "secret"]).Any()) context.Error(DiagnosticCodes.InvalidExtensionContract, "A setting is 'public', 'secret' or neither.", line.Location);
        if (isPublic && isSecret) context.Error(DiagnosticCodes.SettingBothPublicAndSecret, $"Setting '{match.Groups[1].Value}' cannot be both public and secret; a secret must never reach a browser.", line.Location);
        var exposure = ExtensionSettingExposure.Backend;
        if (isPublic) exposure = ExtensionSettingExposure.Public;
        if (isSecret) exposure = ExtensionSettingExposure.Secret;
        return new(match.Groups[1].Value, exposure, line.Location);
    }

    static ExtensionTrigger? ParseTrigger(ParserContext context, SourceLine header)
    {
        var match = NamedRegex("trigger").Match(header.Content);
        var payload = ParseFields(context, header, "trigger");
        if (match.Success) return new(match.Groups[1].Value, payload, header.Location);
        context.Error(DiagnosticCodes.InvalidExtensionContract, "Expected 'trigger <Name>'.", header.Location);
        return null;
    }

    static ImmutableArray<PropertySyntax> ParseFields(ParserContext context, SourceLine header, string owner)
    {
        var fields = ImmutableArray.CreateBuilder<PropertySyntax>();
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (PropertyLineParser.TryParse(line) is null)
            {
                context.Error(DiagnosticCodes.InvalidExtensionContract, $"Expected '<field> <Type>' in a {owner}.", line.Location);
                context.SkipBlock(line.Indent);
                continue;
            }

            fields.Add(PropertyLineParser.Parse(context, line)!);
            RejectChildren(context, line);
        }

        return fields.ToImmutable();
    }

    static ExtensionComponent ParseComponent(ParserContext context, SourceLine header)
    {
        var match = NamedRegex("component").Match(header.Content);
        if (!match.Success) context.Error(DiagnosticCodes.InvalidExtensionContract, "Expected 'component <Name>'.", header.Location);
        string? kind = null, placeholder = null;
        var platforms = ImmutableArray.CreateBuilder<string>();
        var properties = ImmutableArray.CreateBuilder<PropertySyntax>();
        var signals = ImmutableArray.CreateBuilder<ExtensionSignal>();
        var invokes = ImmutableArray.CreateBuilder<string>();
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            var word = LineText.FirstWord(line.Content);
            var rest = line.Content[word.Length..].Trim();
            switch (word)
            {
                case "kind":
                    RejectChildren(context, line);
                    kind = rest;
                    if (!ExtensionVocabulary.ComponentKinds.Contains(rest)) context.Error(DiagnosticCodes.UnknownComponentKind, $"'{rest}' is not a component kind; expected {string.Join(", ", ExtensionVocabulary.ComponentKinds)}.", line.Location);
                    break;
                case "platform":
                    RejectChildren(context, line);
                    foreach (var platform in rest.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    {
                        platforms.Add(platform);
                        if (!ExtensionVocabulary.Platforms.Contains(platform)) context.Error(DiagnosticCodes.UnknownComponentPlatform, $"'{platform}' is not a platform; expected {string.Join(", ", ExtensionVocabulary.Platforms)}.", line.Location);
                    }

                    break;
                case "placeholder":
                    placeholder = OneString(context, line, PlaceholderRegex(), "placeholder \"<path>\"");
                    break;
                case "property":
                    if (PropertyLineParser.TryParse(line with { Content = rest }) is null)
                    {
                        context.Error(DiagnosticCodes.InvalidExtensionContract, "Expected 'property <name> <Type>'.", line.Location);
                        context.SkipBlock(line.Indent);
                    }
                    else
                    {
                        properties.Add(PropertyLineParser.Parse(context, line with { Content = rest, Indent = line.Indent + word.Length + 1 })!);
                        RejectChildren(context, line);
                    }

                    break;
                case "raises":
                    var signal = NamedRegex("raises").Match(line.Content);
                    var values = ParseFields(context, line, "signal");
                    if (signal.Success) signals.Add(new(signal.Groups[1].Value, values, line.Location));
                    else context.Error(DiagnosticCodes.InvalidExtensionContract, "Expected 'raises <signal>'.", line.Location);
                    break;
                case "invokes":
                    RejectChildren(context, line);
                    if (IdentifierRegex().IsMatch(rest)) invokes.Add(rest);
                    else context.Error(DiagnosticCodes.InvalidExtensionContract, "Expected 'invokes <Ability>'.", line.Location);
                    break;
                default:
                    context.Error(DiagnosticCodes.InvalidExtensionContract, $"'{word}' is not a component line; expected kind, platform, placeholder, property, raises or invokes.", line.Location);
                    context.SkipBlock(line.Indent);
                    break;
            }
        }

        return new(match.Success ? match.Groups[1].Value : string.Empty, kind, platforms.ToImmutable(), placeholder, properties.ToImmutable(), signals.ToImmutable(), invokes.ToImmutable(), header.Location);
    }

    static ExtensionBackend? ParseBackend(ParserContext context, SourceLine line)
    {
        RejectChildren(context, line);
        var match = BackendRegex().Match(line.Content);
        if (match.Success) return new(match.Groups[1].Value, match.Groups[2].Value, StringLiteral.Unescape(match.Groups[3].Value), line.Location);
        context.Error(DiagnosticCodes.InvalidExtensionContract, "Expected 'backend runtime <name> <version> assembly \"<file>\"'.", line.Location);
        return null;
    }

    static void RejectChildren(ParserContext context, SourceLine line)
    {
        if (!context.TryPeekChild(line.Indent, out var child)) return;
        context.Error(DiagnosticCodes.InvalidExtensionContract, "This contract line is written on one line.", child.Location);
        context.SkipBlock(line.Indent);
    }

    static void Validate(ExtensionContract contract, ParserContext context)
    {
        foreach (var (field, present) in new[] { ("display name", contract.DisplayName), ("description", contract.Description), ("icon", contract.Icon) })
        {
            if (string.IsNullOrWhiteSpace(present)) context.Error(DiagnosticCodes.MissingExtensionManifestField, $"Extension '{contract.Name}' does not declare its {field}.", contract.Location);
        }

        var abilities = contract.Abilities.Select(ability => ability.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var component in contract.Components)
        {
            if (component.Kind is null) context.Error(DiagnosticCodes.InvalidExtensionContract, $"Component '{component.Name}' declares no kind.", component.Location);
            if (component.Kind is not null and not "screenplay" && string.IsNullOrWhiteSpace(component.Placeholder)) context.Error(DiagnosticCodes.ComponentWithoutPlaceholder, $"Component '{component.Name}' has a frontend but declares no placeholder for design tools to draw.", component.Location);
            foreach (var invoked in component.Invokes.Where(name => !abilities.Contains(name))) context.Error(DiagnosticCodes.ComponentInvokesUnknownAbility, $"Component '{component.Name}' invokes ability '{invoked}', which the contract does not declare.", component.Location);
        }

        Duplicates(context, "ability", contract.Abilities.Select(member => (member.Name, member.Location)));
        Duplicates(context, "trigger", contract.Triggers.Select(member => (member.Name, member.Location)));
        Duplicates(context, "component", contract.Components.Select(member => (member.Name, member.Location)));
        Duplicates(context, "setting", contract.Settings.Select(member => (member.Name, member.Location)));
    }

    static void Duplicates(ParserContext context, string kind, IEnumerable<(string Name, SourceLocation Location)> members)
    {
        foreach (var duplicate in members.GroupBy(member => member.Name, StringComparer.OrdinalIgnoreCase).SelectMany(group => group.Skip(1)))
        {
            context.Error(DiagnosticCodes.DuplicateExtensionMember, $"The contract already declares {kind} '{duplicate.Name}'.", duplicate.Location);
        }
    }

    [GeneratedRegex(@"^extension\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex ExtensionHeaderRegex();

    [GeneratedRegex(@"^display\s+name\s+""((?:[^""\\]|\\.)*)""$", RegexOptions.NonBacktracking)]
    private static partial Regex DisplayRegex();

    [GeneratedRegex(@"^description\s+""((?:[^""\\]|\\.)*)""$", RegexOptions.NonBacktracking)]
    private static partial Regex DescriptionRegex();

    [GeneratedRegex(@"^icon\s+""((?:[^""\\]|\\.)*)""$", RegexOptions.NonBacktracking)]
    private static partial Regex IconRegex();

    [GeneratedRegex(@"^image\s+""((?:[^""\\]|\\.)*)""$", RegexOptions.NonBacktracking)]
    private static partial Regex ImageRegex();

    [GeneratedRegex(@"^caption\s+""((?:[^""\\]|\\.)*)""$", RegexOptions.NonBacktracking)]
    private static partial Regex CaptionRegex();

    [GeneratedRegex(@"^requires\s+([a-z]+)\s+(""(?:[^""\\]|\\.)*""|\S.*)$", RegexOptions.NonBacktracking)]
    private static partial Regex RequiresRegex();

    [GeneratedRegex(@"^setting\s+([A-Za-z_]\w*)((?:\s+\w+)*)$", RegexOptions.NonBacktracking)]
    private static partial Regex SettingRegex();

    [GeneratedRegex(@"^placeholder\s+""((?:[^""\\]|\\.)*)""$", RegexOptions.NonBacktracking)]
    private static partial Regex PlaceholderRegex();

    [GeneratedRegex(@"^frontend\s+""((?:[^""\\]|\\.)*)""$", RegexOptions.NonBacktracking)]
    private static partial Regex FrontendRegex();

    [GeneratedRegex(@"^backend\s+runtime\s+([A-Za-z_][\w.-]*)\s+(\S+)\s+assembly\s+""((?:[^""\\]|\\.)*)""$", RegexOptions.NonBacktracking)]
    private static partial Regex BackendRegex();

    [GeneratedRegex(@"^[A-Za-z_]\w*$", RegexOptions.NonBacktracking)]
    private static partial Regex IdentifierRegex();

    static Regex NamedRegex(string keyword) => new($@"^{keyword}\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking);
}
