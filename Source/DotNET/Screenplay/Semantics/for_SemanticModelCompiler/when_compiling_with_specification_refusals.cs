// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Semantics.for_SemanticModelCompiler;

public class when_compiling_with_specification_refusals : given.a_specification_refusal_compiler
{
    CompilationResult<SemanticCompilation> _result;
    SemanticCompilation _baseline;

    void Because()
    {
        _baseline = Compile(Source).Value!;
        _result = Compile(Source.Replace("when Record amount = 10", "when Record", StringComparison.Ordinal));
    }

    [Fact] void should_still_fail_compilation() => _result.Success.ShouldBeFalse();
    [Fact] void should_leave_the_existing_value_absent() => _result.Value.ShouldBeNull();
    [Fact] void should_offer_the_opt_in_compilation() => _result.SpecificationRefusedValue.ShouldNotBeNull();
    [Fact] void should_remove_the_broken_specification() => Recording(_result.SpecificationRefusedValue!).Specifications.ShouldBeEmpty();
    [Fact] void should_keep_its_command() => Recording(_result.SpecificationRefusedValue!).Commands.Single().Id.ShouldEqual(Recording(_baseline).Commands.Single().Id);
    [Fact] void should_keep_the_other_slice() => Confirming(_result.SpecificationRefusedValue!).Id.ShouldEqual(Confirming(_baseline).Id);
    [Fact] void should_keep_the_healthy_specification_identity() => Confirming(_result.SpecificationRefusedValue!).Specifications.Single().Id.ShouldEqual(Confirming(_baseline).Specifications.Single().Id);
    [Fact] void should_execute_the_healthy_specification() => new SemanticSpecificationRunner().Run(_result.SpecificationRefusedValue!, Confirming(_baseline).Specifications.Single().Id).Passed.ShouldBeTrue();
    [Fact] void should_record_the_refused_identity() => _result.SpecificationRefusedValue!.RefusedSpecifications.Single().Id.ShouldEqual(Recording(_baseline).Specifications.Single().Id);
    [Fact] void should_keep_the_missing_property_error() => _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.MissingSpecificationProperty && diagnostic.Severity == DiagnosticSeverity.Error);
    [Fact] void should_retain_the_exact_error_on_the_refusal() => _result.SpecificationRefusedValue!.RefusedSpecifications.Single().Diagnostics.ShouldContainOnly(_result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray());
    [Fact] void should_not_map_a_refused_identity_into_the_model() => _result.SpecificationRefusedValue!.SourceMap.Entries.ShouldNotContain(entry => entry.SemanticId == Recording(_baseline).Specifications.Single().Id);
    [Fact]
    void should_preserve_the_healthy_source_map()
    {
        var id = Confirming(_baseline).Specifications.Single().Id;
        var expected = _baseline.SourceMap.Entries.Single(entry => entry.SemanticId == id);
        var actual = _result.SpecificationRefusedValue!.SourceMap.Entries.Single(entry => entry.SemanticId == id);
        actual.Origin.ShouldEqual(expected.Origin);
        actual.Role.ShouldEqual(expected.Role);
        actual.Span.Document.ShouldEqual(expected.Span.Document);
        actual.Span.StartLine.ShouldEqual(expected.Span.StartLine);
        actual.Span.StartColumn.ShouldEqual(expected.Span.StartColumn);
        actual.Span.Start.ShouldEqual(_result.SpecificationRefusedValue!.Documents.Documents.Single().Text.IndexOf("specification ConfirmationWorks", StringComparison.Ordinal));
    }
    [Fact] void should_preserve_the_documents() => _result.SpecificationRefusedValue!.Documents.Documents.Single().Text.ShouldContain("when Record\n");

    [Theory]
    [InlineData("when Record amount = 10", "when Unknown amount = 10")]
    [InlineData("then Recorded amount = 10", "then Unknown amount = 10")]
    [InlineData("when Record amount = 10", "when Record amount = 10\n          extra = 1")]
    void should_also_refuse_other_specification_binding_errors(string oldText, string newText)
    {
        var result = Compile(Source.Replace(oldText, newText, StringComparison.Ordinal));
        result.Success.ShouldBeFalse();
        result.Value.ShouldBeNull();
        result.SpecificationRefusedValue.ShouldNotBeNull();
        Recording(result.SpecificationRefusedValue!).Specifications.ShouldBeEmpty();
        result.SpecificationRefusedValue!.RefusedSpecifications.Single().Diagnostics.All(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    }

    [Fact]
    void should_respect_persisted_identities_for_both_refused_and_healthy_specifications()
    {
        var application = ApplicationIdentity.Create("App");
        var refused = SemanticId.Create(SemanticKind.Specification, "persisted-recording");
        var healthy = SemanticId.Create(SemanticKind.Specification, "persisted-confirmation");
        var catalog = SemanticIdentityCatalog.Create(application, [],
        [
            new(SemanticAddress.ForSpecification(SemanticAddress.ForSlice(application, "M", "F", "Recording"), "RecordingWorks"), refused, SemanticIdentityOrigin.Persisted),
            new(SemanticAddress.ForSpecification(SemanticAddress.ForSlice(application, "M", "F", "Confirming"), "ConfirmationWorks"), healthy, SemanticIdentityOrigin.Persisted)
        ], []);
        var result = Compile(Source.Replace("when Record amount = 10", "when Record", StringComparison.Ordinal), identities: catalog);
        result.SpecificationRefusedValue!.RefusedSpecifications.Single().Id.ShouldEqual(refused);
        Confirming(result.SpecificationRefusedValue!).Specifications.Single().Id.ShouldEqual(healthy);
        result.SpecificationRefusedValue!.Documents.IdentityCatalog.ShouldEqual(catalog);
    }

    [Fact]
    void should_not_promote_failed_compilation_context_descriptors()
    {
        var source = Source.Replace("        produces Recorded", "        validate csharp\n          ```csharp\n          return true;\n          ```\n        produces Recorded", StringComparison.Ordinal);
        Compile(source).TypedContextDescriptors.ShouldNotBeEmpty();
        var result = Compile(source.Replace("when Record amount = 10", "when Record", StringComparison.Ordinal));
        result.SpecificationRefusedValue.ShouldNotBeNull();
        result.TypedContextDescriptors.ShouldBeEmpty();
    }

    [Fact]
    void should_prune_broken_specifications_in_nested_features()
    {
        var nested = "module M\n  feature Parent\n" + string.Join("\n", Source.Split('\n').Skip(1).Select(line => "  " + line));
        var baseline = Compile(nested).Value!;
        var result = Compile(nested.Replace("when Record amount = 10", "when Record", StringComparison.Ordinal));
        var slices = result.SpecificationRefusedValue!.Model.Application.Modules.Single().Features.Single().Features.Single().Slices;
        slices[0].Specifications.ShouldBeEmpty();
        slices[1].Specifications.Single().Id.ShouldEqual(baseline.Model.Application.Modules.Single().Features.Single().Features.Single().Slices[1].Specifications.Single().Id);
        result.SpecificationRefusedValue!.RefusedSpecifications.Length.ShouldEqual(1);
    }

    [Fact]
    void should_preserve_refusals_through_sidecar_copies()
    {
        var partial = _result.SpecificationRefusedValue!;
        var copy = partial.WithEnvironment(partial.Environment).WithSidecarsOf(partial);
        copy.RefusedSpecifications.ShouldEqual(partial.RefusedSpecifications);
    }

    [Fact]
    void should_leave_successful_compilation_unchanged()
    {
        var result = Compile(Source);
        result.Success.ShouldBeTrue();
        result.SpecificationRefusedValue.ShouldBeNull();
        result.Value!.RefusedSpecifications.ShouldBeEmpty();
    }
}
