// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelCompiler;

public class when_compiling_with_case_refusals : given.a_specification_refusal_compiler
{
    const string Cases = """
        specification RecordingWorks
                parameter unused Details
                case Small
                  unused
                    name = "Small"
                    count = 1
                case Large
                  unused
                    name = "Large"
                    count = 2
        """;
    CompilationResult<SemanticCompilation> _result;
    SemanticCompilation _baseline;

    void Because()
    {
        var source = "type Details\n  name String\n  count Int\n" + Source.Replace("specification RecordingWorks", Cases, StringComparison.Ordinal);
        _baseline = Compile(source).Value!;
        _result = Compile(source.Replace("            count = 1\n", "", StringComparison.Ordinal));
    }

    [Fact] void should_keep_the_existing_failure_contract() => _result.Success.ShouldBeFalse();
    [Fact] void should_leave_the_existing_value_absent() => _result.Value.ShouldBeNull();
    [Fact] void should_offer_the_safe_compilation() => _result.SpecificationRefusedValue.ShouldNotBeNull();
    [Fact] void should_keep_only_the_valid_row() => Recording(_result.SpecificationRefusedValue!).Specifications.Select(specification => specification.Name).ShouldContainOnly("RecordingWorks_Large");
    [Fact] void should_refuse_the_expanded_row_identity() => _result.SpecificationRefusedValue!.RefusedSpecifications.Single().Id.ShouldEqual(Recording(_baseline).Specifications.Single(specification => specification.Name == "RecordingWorks_Small").Id);
    [Fact] void should_keep_the_valid_row_origin() => _result.SpecificationRefusedValue!.SpecificationOrigins.Values.Select(origin => origin.Case!.Name).ShouldContainOnly("Large");
    [Fact] void should_keep_the_invalid_row_origin_on_the_refusal() => _result.SpecificationRefusedValue!.RefusedSpecifications.Single().Origin!.Case!.Name.ShouldEqual("Small");
    [Fact] void should_keep_the_case_error() => _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.Code == DiagnosticCodes.MissingStructuredValueMember);
    [Fact] void should_keep_the_healthy_slice() => Confirming(_result.SpecificationRefusedValue!).Specifications.Single().Id.ShouldEqual(Confirming(_baseline).Specifications.Single().Id);
}
