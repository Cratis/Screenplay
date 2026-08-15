// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.RegularExpressions;
using Cratis.Screenplay.for_Documentation.given;

namespace Cratis.Screenplay.Diagnostics.for_DiagnosticCodes;

/// <summary>
/// A code is the only part of a diagnostic a consumer may rely on, which is the whole reason message text is
/// free to be reworded. That guarantee only holds for codes the catalogue actually lists, and 58 of them once
/// did not - added with the screens, forms, contribution point, ui profile and theme work, each of which
/// updated <see cref="DiagnosticCodes"/> and not the page. Nothing compared the two, so nothing said so.
/// </summary>
public class when_holding_the_catalogue_against_the_documentation : Specification
{
    // Retired codes stay listed on the page after they stop being reported, so the section that records them
    // is not part of the comparison in either direction.
    const string RetiredHeading = "## Retired codes";

    List<string> _declared;
    List<string> _documented;

    void Establish()
    {
        _declared =
        [
            .. typeof(DiagnosticCodes)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral)
                .Select(field => (string)field.GetRawConstantValue()!)
        ];

        var catalogue = File.ReadAllText(Path.Combine(DocumentationExamples.Root(), "screenplay", "diagnostics.md"));
        var retired = catalogue.IndexOf(RetiredHeading, StringComparison.Ordinal);
        var current = retired < 0 ? catalogue : catalogue[..retired];

        _documented = [.. Regex.Matches(current, @"`(PLAY\d{4})`").Select(match => match.Groups[1].Value).Distinct(StringComparer.Ordinal)];
    }

    [Fact] void should_document_every_code_the_compiler_can_report() =>
        Report("declared but absent from the catalogue", _declared.Except(_documented, StringComparer.Ordinal)).ShouldEqual(string.Empty);

    [Fact] void should_not_list_a_code_the_compiler_cannot_report() =>
        Report("listed in the catalogue but declared nowhere", _documented.Except(_declared, StringComparer.Ordinal)).ShouldEqual(string.Empty);

    static string Report(string what, IEnumerable<string> codes)
    {
        var offenders = codes.Order(StringComparer.Ordinal).ToList();
        return offenders.Count == 0 ? string.Empty : $"{offenders.Count} code(s) {what}: {string.Join(", ", offenders)}";
    }
}
