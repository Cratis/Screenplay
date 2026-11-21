// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing.for_SecretProjectionValidator;

public class when_validating_secret_projections : Specification
{
    const string Concepts = "concept ApiKey : String secret\nconcept BankAccount : String pii secret\nconcept Email : String pii\nconcept Label : String\n";
    const string Slice = "module M\n  feature F\n    slice StateView S\n";

    [Fact]
    void should_warn_for_an_explicit_mapping_into_a_secret_property()
    {
        var finding = Findings(Model("ApiKey", "ApiKey", "          token = token\n")).Single();
        finding.Message.ShouldEqual("Projection 'Keys' copies secret concept 'ApiKey' into read model property 'Row.token' - the value becomes part of the read model and its query results");
        finding.Severity.ShouldEqual(DiagnosticSeverity.Warning);
        finding.Location.Line.ShouldEqual(15);
    }

    [Fact]
    void should_warn_for_an_explicit_mapping_from_a_secret_event_property_into_a_plain_property() =>
        Findings(Model("ApiKey", "String", "          token = token\n")).Single().Message.ShouldContain("secret concept 'ApiKey' into read model property 'Row.token'");

    [Fact]
    void should_warn_when_automap_copies_a_secret_property()
    {
        var finding = Findings(Model("ApiKey", "ApiKey", string.Empty)).Single();
        finding.Message.ShouldContain("Projection 'Keys' copies secret concept 'ApiKey' into read model property 'Row.token'");
        finding.Location.Line.ShouldEqual(14);
    }

    [Fact]
    void should_count_a_personal_secret_as_secret() => Findings(Model("BankAccount", "BankAccount", string.Empty)).Single().Message.ShouldContain("'BankAccount'");

    [Fact]
    void should_warn_when_a_reducer_builds_a_secret_property()
    {
        var finding = Findings(Concepts + Slice + "      readmodel Row\n        id String\n        token ApiKey\n      event Recorded\n        token ApiKey\n      reducer Fold => Row\n        on Recorded\n").Single();
        finding.Message.ShouldEqual("Reducer 'Fold' copies secret concept 'ApiKey' into read model property 'Row.token' - the value becomes part of the read model and its query results");
        finding.Location.Line.ShouldEqual(13);
    }

    [Fact]
    void should_warn_for_a_secret_copied_into_a_child_element() =>
        Findings(Concepts + "type Entry\n  id String\n  token ApiKey\n" + Slice + "      readmodel Row\n        id String\n        entries Entry[]\n      event Recorded\n        id String\n        token ApiKey\n      projection Keys => Row\n        children entries identified by id\n          from Recorded\n")
            .Single().Message.ShouldContain("into read model property 'Row.entries.token'");

    [Fact]
    void should_not_warn_for_a_plain_concept() => Findings(Model("Label", "Label", "          token = token\n")).ShouldBeEmpty();

    [Fact]
    void should_not_warn_for_a_personal_concept_that_is_not_secret() => Findings(Model("Email", "Email", "          token = token\n")).ShouldBeEmpty();

    [Fact]
    void should_not_warn_when_automap_is_off_and_nothing_maps_the_secret() =>
        Findings(Model("ApiKey", "ApiKey", string.Empty).Replace("      projection Keys => Row\n", "      projection Keys => Row\n        no automap\n", StringComparison.Ordinal)).ShouldBeEmpty();

    [Theory]
    [InlineData("$eventSourceId")]
    [InlineData("\"redacted\"")]
    void should_not_warn_for_metadata_or_a_literal(string source) =>
        Findings(Model("ApiKey", "ApiKey", $"          token = {source}\n").Replace("        token ApiKey\n      projection", "        other ApiKey\n      projection", StringComparison.Ordinal)).ShouldBeEmpty();

    [Fact]
    void should_not_warn_without_a_projection_or_reducer() =>
        Findings(Concepts + Slice + "      readmodel Row\n        id String\n        token ApiKey\n").ShouldBeEmpty();

    [Theory]
    [InlineData("automap", "no automap", "", 1)]
    [InlineData("no automap", "automap", "", 0)]
    [InlineData("automap", "automap", "no automap", 0)]
    [InlineData("no automap", "no automap", "automap", 1)]
    void should_resolve_child_and_nested_automap_against_the_projection(string root, string parent, string inner, int count)
    {
        foreach (var parentKind in new[] { "children", "nested" })
        {
            foreach (var innerKind in new[] { "children", "nested" })
            {
                var findings = Findings(NestedModel(parentKind, innerKind, root, parent, inner));
                findings.Length.ShouldEqual(count);
                if (count > 0)
                {
                    findings.Single().Message.ShouldContain("into read model property 'Row.groups.entries.token'");
                }
            }
        }
    }

    static string NestedModel(string parentKind, string innerKind, string root, string parent, string inner) =>
        Concepts +
        "type Entry\n  id String\n  token ApiKey\n" +
        $"type Group\n  id String\n  entries Entry{(innerKind == "children" ? "[]" : string.Empty)}\n" +
        Slice +
        $"      readmodel Row\n        id String\n        groups Group{(parentKind == "children" ? "[]" : string.Empty)}\n" +
        "      event Recorded\n        id String\n        token ApiKey\n      event GroupRecorded\n        id String\n" +
        $"      projection Keys => Row\n        {root}\n" +
        $"        {parentKind} groups{(parentKind == "children" ? " identified by id" : string.Empty)}\n          {parent}\n          from GroupRecorded\n" +
        $"          {innerKind} entries{(innerKind == "children" ? " identified by id" : string.Empty)}\n" +
        (inner.Length > 0 ? $"            {inner}\n" : string.Empty) +
        "            from Recorded\n";

    static string Model(string eventType, string modelType, string mappings) =>
        Concepts + Slice +
        $"      readmodel Row\n        id String\n        token {modelType}\n      event Recorded\n        token {eventType}\n" +
        "      projection Keys => Row\n        from Recorded\n" + mappings;

    static Diagnostic[] Findings(string source)
    {
        var compiler = new ScreenplayCompiler();
        compiler.Parse(source).Success.ShouldBeTrue();
        return [.. compiler.Compile(source).Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.SecretConceptProjected)];
    }
}
