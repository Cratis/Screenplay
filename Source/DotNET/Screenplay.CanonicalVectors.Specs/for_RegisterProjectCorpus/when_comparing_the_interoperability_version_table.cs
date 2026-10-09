// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
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
        released.Length.ShouldEqual(7);
        claimed.Length.ShouldEqual(1);
        var rows = released.Concat(claimed).ToArray();
        var languageVersions = rows.Select(row => LanguageVersion.Parse(row[0])).ToArray();
        var semanticVersions = rows.Select(row => SemanticVersion.Parse(row[0])).ToArray();
        languageVersions.Distinct().Count().ShouldEqual(rows.Length);
        semanticVersions.Distinct().Count().ShouldEqual(rows.Length);
        languageVersions.ShouldEqual(Declared<LanguageVersion>());
        semanticVersions.ShouldEqual(Declared<SemanticVersion>());
        languageVersions.ShouldEqual(EsmSchemaV8Support.LanguageVersions);
        semanticVersions.ShouldEqual(EsmSchemaV8Support.SemanticVersions);
        foreach (var row in rows)
        {
            row.Length.ShouldEqual(6);
            row.All(cell => !string.IsNullOrWhiteSpace(cell)).ShouldBeTrue();
            var version = SemanticVersion.Parse(row[0]);
            row[2].ShouldEqual(version.Major.ToString(System.Globalization.CultureInfo.InvariantCulture));
            row[3].ShouldContain("https://github.com/Cratis/Screenplay/blob/main/decisions/");
            row[4].ShouldEqual($"`full-esm-v{version.Major}.json`");
            using var golden = typeof(when_comparing_the_interoperability_version_table).Assembly.GetManifestResourceStream($"Cratis.Screenplay.CanonicalVectors.Golden.full-esm-v{version.Major}.json")!;
            using var memory = new MemoryStream();
            golden.CopyTo(memory);
            SemanticModelSerializer.Deserialize(memory.ToArray()).SemanticVersion.ShouldEqual(version);
        }
    }

    static string[][] Rows(string text, string heading)
    {
        var section = text[(text.IndexOf(heading, StringComparison.Ordinal) + heading.Length)..];
        var lines = section.Split('\n').SkipWhile(line => !line.StartsWith("| Version", StringComparison.Ordinal)).Skip(2).TakeWhile(line => line.StartsWith('|'));

        return [.. lines.Select(line => line.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(cell => cell.Trim()).ToArray())];
    }

    static T[] Declared<T>() => [.. typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.FieldType == typeof(T))
        .Select(field => (T)field.GetValue(null)!)
        .OrderBy(version => SemanticVersion.Parse(version!.ToString()!).Major)
        .ThenBy(version => SemanticVersion.Parse(version!.ToString()!).Minor)];
}
