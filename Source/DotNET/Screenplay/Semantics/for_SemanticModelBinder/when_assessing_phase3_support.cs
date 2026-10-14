// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_assessing_phase3_support : given.a_semantic_binder
{
    [Fact]
    void should_keep_assessment_year_out_of_semantic_bytes()
    {
        const string Source = "concept Reference : String\n  generate pattern \"{yyyy}[AB]\" checksum luhn";
        var syntax = new ScreenplayCompiler().Parse(Source).Value!;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("application"), "application", "app.play", Source);
        var documents = SemanticDocumentSet.Create([document], catalog);
        var first = new SemanticModelBinder(2026).Bind("Projects", syntax, documents);
        var later = new SemanticModelBinder(2030).Bind("Projects", syntax, documents);
        first.Success.ShouldBeTrue();
        later.Success.ShouldBeTrue();
        SemanticModelSerializer.Serialize(first.Value!.Model).ShouldEqual(SemanticModelSerializer.Serialize(later.Value!.Model));
    }

    [Fact]
    void should_name_the_first_empty_utc_date()
    {
        var result = BindAtYear("concept Reference : String\n  generate pattern \"{yyyy}[AB]\" checksum mod11", 2026);
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.EmptyGenerationSupport && diagnostic.Message.Contains("UTC 2029-01-01", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9900)]
    void should_require_the_complete_assessment_horizon(int year) => Catch.Exception(() => _ = new SemanticModelBinder(year)).ShouldBeOfExactType<InvalidSemanticContract>();

    static CompilationResult<SemanticCompilation> BindAtYear(string source, int year)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("application"), "application", "app.play", source);

        return new SemanticModelBinder(year).Bind("Projects", new ScreenplayCompiler().Parse(source).Value!, SemanticDocumentSet.Create([document], catalog));
    }

    [Fact]
    void should_preserve_unknown_implementation_entropy_without_fabricating_zero()
    {
        SemanticGenerationContract.Bits(new("implementation") { RequirementId = new string('a', 64) }).ShouldBeNull();
    }

    [Fact]
    void should_not_admit_known_low_entropy_identifiers()
    {
        var result = Bind("concept Id : String\n  generate pattern \"[AB]\"\nmodule M\n  feature F\n    slice StateChange Create\n      command Create\n        id Id generated identifier\n        produces event Created");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.LowGeneratedIdentifierEntropy).ShouldBeTrue();
    }

    [Fact]
    void should_refuse_misplaced_or_malformed_generation_members_programmatically()
    {
        var application = canonical_serialization_golden_vectors.CreateSemanticModelV21().Application;
        var concept = application.Concepts.Single(value => value.Name == "PatternReference");
        SemanticGeneration[] invalid =
        [
            new("pattern") { Pattern = "[AB]", Prefix = "wrong" },
            new("pattern") { Pattern = "[AB]", Minimum = 1 },
            new("pattern") { Pattern = "" },
            new("pattern") { Pattern = "000000006-[AB]", Checksum = "mod11" },
            new("random") { Minimum = 2, Maximum = 1 },
            new("random") { Minimum = 1 },
            new("implementation") { RequirementId = "bad" },
            new("implementation") { RequirementId = new string('A', 64) }
        ];
        foreach (var generation in invalid)
        {
            var changed = application with { Concepts = application.Concepts.SetItem(application.Concepts.IndexOf(concept), concept with { Generation = generation }) };
            Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V21, SemanticVersion.V21, changed, 2026)).ShouldBeOfExactType<InvalidSemanticContract>();
        }
    }
}
