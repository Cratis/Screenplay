// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.CanonicalVectors.Specs.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_RegisterProjectCorpus;

public class when_comparing_the_interoperability_version_table : Specification
{
    [Fact]
    void should_match_the_versions_contract_and_allow_only_one_unreleased_entry()
    {
        using var stream = typeof(when_comparing_the_interoperability_version_table).Assembly.GetManifestResourceStream("Cratis.Screenplay.CanonicalVectors.Documentation.interoperability.md")!;
        using var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();
        var released = Rows(text, "#### Released versions");
        var claimed = text.Contains("#### Claimed, unreleased version", StringComparison.Ordinal) ? Rows(text, "#### Claimed, unreleased version") : [];
        released.Length.ShouldEqual(27);
        claimed.Length.ShouldEqual(1);
        var rows = released.Concat(claimed).ToArray();
        rows.Length.ShouldEqual(28);
        esm_version_pins.AssertEveryDeclaredVersionIsPinned();
        var languageVersions = rows.Select(row => LanguageVersion.Parse(row[0])).ToArray();
        var semanticVersions = rows.Select(row => SemanticVersion.Parse(row[0])).ToArray();
        languageVersions.Distinct().Count().ShouldEqual(rows.Length);
        semanticVersions.Distinct().Count().ShouldEqual(rows.Length);
        languageVersions.ShouldEqual(esm_version_pins.Declared<LanguageVersion>());
        semanticVersions.ShouldEqual(esm_version_pins.Declared<SemanticVersion>());
        languageVersions.ShouldEqual(EsmSchemaV28Support.LanguageVersions);
        semanticVersions.ShouldEqual(EsmSchemaV28Support.SemanticVersions);
        foreach (var row in rows)
        {
            row.Length.ShouldEqual(6);
            row.All(cell => !string.IsNullOrWhiteSpace(cell)).ShouldBeTrue();
            var version = SemanticVersion.Parse(row[0]);
            row[2].ShouldEqual(version.Major.ToString(System.Globalization.CultureInfo.InvariantCulture));
            row[3].ShouldContain("https://github.com/Cratis/Screenplay/blob/main/decisions/");
            row[4].ShouldEqual(EsmGoldenVectors.Versions.Contains((int)version.Major) ? $"`full-esm-v{version.Major}.json`" : "Feature corpus");
            SemanticModelSerializer.Deserialize(esm_version_pins.Read(version)).SemanticVersion.ShouldEqual(version);
        }
    }

    static string[][] Rows(string text, string heading)
    {
        var section = text[(text.IndexOf(heading, StringComparison.Ordinal) + heading.Length)..];
        var lines = section.Split('\n').SkipWhile(line => !line.StartsWith("| Version", StringComparison.Ordinal)).Skip(2).TakeWhile(line => line.StartsWith('|'));

        return [.. lines.Select(line => line.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(cell => cell.Trim()).ToArray())];
    }
}
