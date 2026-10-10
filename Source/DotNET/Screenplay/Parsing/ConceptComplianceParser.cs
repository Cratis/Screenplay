// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

internal static partial class ConceptComplianceParser
{
    internal static readonly string[] SpecialCategories = ["racialOrEthnicOrigin", "politicalOpinions", "religiousOrPhilosophicalBeliefs", "tradeUnionMembership", "genetic", "biometric", "health", "sexLifeOrSexualOrientation"];

    internal static string WireName(string marker) => marker.TrimStart('@') switch
    {
        "personal" => ConceptAttributeSyntax.Pii,
        "secret" => ConceptAttributeSyntax.Sensitive,
        var name => name
    };

    internal static string CanonicalName(string marker) => WireName(marker) == ConceptAttributeSyntax.Sensitive ? "secret" : WireName(marker);

    internal static List<ConceptAttributeSyntax> ParseMarkers(ParserContext context, SourceLine line, string concept, string text)
    {
        var markers = WhitespaceRegex().Split(text.Trim()).Where(marker => marker.Length > 0).ToArray();
        ReportLegacy(context, line, markers);
        var attributes = new List<ConceptAttributeSyntax>();
        var firstMarkers = new Dictionary<string, string>();
        var reported = false;
        foreach (var marker in markers)
        {
            var name = WireName(marker);
            if (name is not (ConceptAttributeSyntax.Pii or ConceptAttributeSyntax.Sensitive) || marker == "@personal" || marker == "@secret")
            {
                context.Error(DiagnosticCodes.UnknownComplianceMarker, $"Unknown concept marker '{marker}' - expected pii, personal or secret", line.Location);
            }

            if (firstMarkers.TryGetValue(name, out var first))
            {
                if (!reported)
                {
                    var canonical = CanonicalName(marker);
                    var alias = first == marker ? string.Empty : $" - '{(marker == canonical ? first : marker)}' is the same marker as '{canonical}'";
                    context.Warning(DiagnosticCodes.DuplicateComplianceMarker, $"Concept '{concept}' repeats the '{canonical}' marker{alias} - remove the duplicate", line.Location);
                    reported = true;
                }

                continue;
            }

            firstMarkers.Add(name, marker);
            attributes.Add(new(name, line.Location));
        }

        return attributes;
    }

    internal static bool TryParseDirective(ParserContext context, SourceLine line, string concept, List<ConceptAttributeSyntax> attributes, Dictionary<string, SourceLocation> locations)
    {
        var match = DirectiveRegex().Match(line.Content);
        if (!match.Success) return false;
        var marker = match.Groups[1].Value;
        var setting = match.Groups[2].Value;
        var value = match.Groups[3].Value;
        var name = WireName(marker);
        var canonical = CanonicalName(marker);
        ReportLegacy(context, line, [marker]);
        if (name is not (ConceptAttributeSyntax.Pii or ConceptAttributeSyntax.Sensitive) || marker == "@personal" || marker == "@secret")
        {
            context.Error(DiagnosticCodes.UnknownComplianceMarker, $"Unknown concept marker '{marker}' - expected pii, personal or secret", line.Location);
            return true;
        }

        var index = attributes.FindIndex(attribute => attribute.Name == name);
        if (index < 0)
        {
            context.Error(DiagnosticCodes.AttributeReasonWithoutAttribute, $"Concept '{concept}' declares '{canonical} {setting}' without the marker - write 'concept {concept} : <Type> {canonical}'", line.Location);
            return true;
        }

        var attribute = attributes[index];
        switch (setting)
        {
            case "reason":
                var reason = ReasonRegex().Match(value);
                if (!reason.Success)
                {
                    context.Error(DiagnosticCodes.UnknownConceptDirective, $"Invalid '{canonical} reason' - expected '{canonical} reason \"<text>\"'", line.Location);
                    return true;
                }

                if (attribute.Reason is not null)
                {
                    context.Error(DiagnosticCodes.DuplicateAttributeReason, $"Concept '{concept}' already declares a reason for '{canonical}' - at most one is allowed", line.Location);
                    return true;
                }

                attributes[index] = attribute with { Reason = StringLiteral.Unescape(reason.Groups[1].Value) };
                locations[$"reason:{name}"] = line.Location;
                return true;
            case "scope":
                if (name != ConceptAttributeSyntax.Sensitive || value is not ("subject" or "namespace" or "global"))
                {
                    context.Error(DiagnosticCodes.InvalidSecretScope, $"Invalid '{canonical} scope {value}' - scope belongs to secret and must be subject, namespace or global", line.Location);
                    return true;
                }

                if (attribute.Scope is not null)
                {
                    context.Error(DiagnosticCodes.DuplicateSecretScope, $"Concept '{concept}' already declares secret scope - at most one is allowed", line.Location);
                    return true;
                }

                attributes[index] = attribute with { Scope = value };
                locations["scope:sensitive"] = line.Location;
                if (attributes.Exists(attribute => attribute.Name == ConceptAttributeSyntax.Pii))
                {
                    context.Warning(DiagnosticCodes.SecretScopeIgnoredForPii, $"Concept '{concept}' is pii secret - secret scope is ignored because only Chronicle [PII] renders", line.Location);
                }

                return true;
            case "special":
            case "criminal":
                if (name != ConceptAttributeSyntax.Pii || (setting == "special" ? !SpecialCategories.Contains(value) : value.Length > 0))
                {
                    context.Error(DiagnosticCodes.InvalidPersonalDataQualifier, $"Invalid '{canonical} {setting}{(value.Length > 0 ? $" {value}" : string.Empty)}' - expected 'pii criminal' or 'pii special <category>' with category {string.Join(", ", SpecialCategories)}", line.Location);
                    return true;
                }

                if (setting == "special" && attribute.SpecialCategory is not null)
                {
                    context.Error(DiagnosticCodes.DuplicateSpecialCategory, $"Concept '{concept}' already declares pii special - at most one is allowed", line.Location);
                    return true;
                }

                attributes[index] = setting == "special" ? attribute with { SpecialCategory = value } : attribute with { Criminal = true };
                locations[$"{setting}:pii"] = line.Location;
                return true;
            default:
                return false;
        }
    }

    static void ReportLegacy(ParserContext context, SourceLine line, IEnumerable<string> markers)
    {
        if (markers.Any(marker => marker == "@pii" || marker == "sensitive" || marker == "@sensitive"))
        {
            context.Add(new(DiagnosticSeverity.Information, DiagnosticCodes.LegacyComplianceMarker, "Legacy compliance spelling - use bare pii for personal data and secret for operational secrets; reason text is preserved", line.Location));
        }
    }

    [GeneratedRegex(@"\s+", RegexOptions.None, 1000)]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"^(@?[a-z_]\w*)\s+(reason|scope|special|criminal)(?:\s+(.*))?$", RegexOptions.None, 1000)]
    private static partial Regex DirectiveRegex();

    [GeneratedRegex("^\"(" + StringLiteral.BodyPattern + ")\"$", RegexOptions.None, 1000)]
    private static partial Regex ReasonRegex();
}
