// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_generation : given.a_semantic_binder
{
    [Theory]
    [InlineData("Uuid", "uuidv7")]
    [InlineData("String", "uuid")]
    [InlineData("String", "uuidv7 case upper prefix \"id_\"")]
    [InlineData("String", "ulid")]
    [InlineData("String", "typeid \"\"")]
    [InlineData("String", "typeid \"user\"")]
    [InlineData("String", "nanoid")]
    [InlineData("String", "nanoid 32 alphabet \"ab\" prefix \"ref_\"")]
    [InlineData("String", "bytes 32 base64url")]
    [InlineData("String", "bytes 16 hex case upper")]
    [InlineData("String", "bytes 16 base32 case lower")]
    void should_admit_high_entropy_strategies(string primitive, string strategy)
    {
        var result = Bind($"concept C : {primitive}\n  generate {strategy}");
        result.Success.ShouldBeTrue();
        result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V15);
        SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(result.Value.Model)).Revision.ShouldEqual(result.Value.Model.Revision);
    }

    [Theory]
    [InlineData("String", "pattern \"ORD-#{7}\"", DiagnosticCodes.UnsupportedSemanticSyntax)]
    [InlineData("Int", "random 1..9", DiagnosticCodes.UnsupportedSemanticSyntax)]
    [InlineData("String", "rule Make", DiagnosticCodes.UnsupportedSemanticSyntax)]
    [InlineData("String", "sequence", DiagnosticCodes.ReservedSequenceGeneration)]
    [InlineData("String secret", "bytes 32 base64url", DiagnosticCodes.UnsupportedSemanticSyntax)]
    [InlineData("String pii", "ulid", DiagnosticCodes.UnsupportedSemanticSyntax)]
    void should_refuse_unadmitted_strategies_and_markers(string primitive, string strategy, string code) =>
        Bind($"concept C : {primitive}\n  generate {strategy}").Diagnostics.ShouldContain(diagnostic => diagnostic.Code == code);

    [Fact]
    void should_preserve_default_uuid_bytes_revision_and_version()
    {
        const string source = "concept Id : Uuid\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id Id generated identifier\n        produces event Created";
        var baseline = Bind(source).Value!.Model;
        var explicitDefault = Bind(source.Replace("concept Id : Uuid", "concept Id : Uuid\n  generate uuid", StringComparison.Ordinal)).Value!.Model;
        SemanticModelSerializer.Serialize(explicitDefault).SequenceEqual(SemanticModelSerializer.Serialize(baseline)).ShouldBeTrue();
        explicitDefault.Revision.ShouldEqual(baseline.Revision);
        explicitDefault.SemanticVersion.ShouldEqual(baseline.SemanticVersion);
        explicitDefault.Application.Concepts.Single().Generation.ShouldBeNull();
    }

    [Fact]
    void should_keep_admitting_a_generated_property_of_a_default_uuid_concept_with_a_compliance_marker()
    {
        var result = Bind("concept PatientRef : Uuid pii\nmodule M\n  feature F\n    slice StateChange S\n      command Admit\n        patientId String identifier\n        reference PatientRef generated\n        produces event Admitted\n          reference PatientRef = reference");
        result.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax);
    }

    [Fact]
    void should_keep_a_bare_generate_line_in_an_enumeration_as_a_value() =>
        Bind("concept Mode : Enum\n  generate\n  manual").Value!.Model.Application.Concepts.Single().Values.ShouldContainOnly(["generate", "manual"]);

    [Fact]
    void should_keep_generated_concept_rules_refused() =>
        Bind("concept C : String\n  generate nanoid\n  validate\n    min 1").Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax);

    [Fact]
    void should_suppress_nonidentifier_entropy_warning_for_a_unique_mapped_property()
    {
        var result = Bind("concept Code : String\n  generate nanoid 1\nmodule M\n  feature F\n    slice StateChange S\n      command Create\n        id String identifier\n        code Code generated\n        produces event Created\n          code Code = code\n      constraint UniqueCode\n        unique code on Created");
        result.Success.ShouldBeTrue();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.LowGenerationEntropy).ShouldBeFalse();
    }

    [Theory]
    [InlineData("nanoid 1", true, DiagnosticCodes.LowGeneratedIdentifierEntropy)]
    [InlineData("nanoid 1", false, DiagnosticCodes.LowGenerationEntropy)]
    [InlineData("bytes 7 hex", true, DiagnosticCodes.LowGeneratedIdentifierEntropy)]
    void should_diagnose_low_entropy(string strategy, bool identifier, string code)
    {
        var property = identifier ? " generated identifier" : " generated";
        var result = Bind($"concept C : String\n  generate {strategy}\nmodule M\n  feature F\n    slice StateChange S\n      command Create\n        code C{property}\n        returns code");
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == code && diagnostic.Message.Contains("1% collision", StringComparison.Ordinal));
    }
}
