// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_additional_descriptions : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _plain;
    CompilationResult<SemanticCompilation> _described;

    void Because()
    {
        const string source = """
            concept Value : String
            policy Access
              require authenticated
            module M
              form Input for Record
                field value
              feature F
                slice StateChange S
                  event Recorded
                    value Value
                  command Record
                    value Value
                    produces Recorded
                      value = value
                  constraint Unique
                    unique value on Recorded
                  readmodel View
                    value Value
                  projection View => View
                    from Recorded
                  query ByValue => View optional
                    by value Value
                  screen Details
                    title "Values"
            """;
        _plain = Bind(source);
        var described = source.Replace("concept Value : String", "concept Value : String\n  description \"Value\"", StringComparison.Ordinal)
            .Replace("policy Access", "policy Access\n  description \"Policy\"", StringComparison.Ordinal)
            .Replace("  form Input for Record", "  form Input for Record\n    description \"Form\"", StringComparison.Ordinal)
            .Replace("      constraint Unique", "      constraint Unique\n        description \"Constraint\"", StringComparison.Ordinal)
            .Replace("      projection View => View", "      projection View => View\n        description \"Projection\"", StringComparison.Ordinal)
            .Replace("      screen Details", "      screen Details\n        description \"Screen\"", StringComparison.Ordinal);
        _described = Bind(described);
    }

    [Fact] void should_bind_the_described_model() => _described.Success.ShouldBeTrue();
    [Fact] void should_preserve_executable_bytes() => _described.Value!.Model.Revision.ShouldEqual(_plain.Value!.Model.Revision);
    [Fact] void should_preserve_the_model_version() => _described.Value!.Model.SemanticVersion.ShouldEqual(_plain.Value!.Model.SemanticVersion);
    [Fact] void should_report_only_the_four_bound_description_kinds() => _described.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.ReportOnlySemanticSyntax).ShouldEqual(4);
}
