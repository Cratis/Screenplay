// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_a_policy_that_names_a_query_filter : given.a_semantic_binder
{
    const string Source =
        """
        policy Access
          require claim "status" matches status
        module Portal
          feature Reports
            slice StateView Browse
              readmodel Report
                reportId String
                status String
              query Reports => Report[]
                filter status String
                authorize Access
        """;

    CompilationResult<SemanticCompilation> _filter;
    CompilationResult<SemanticCompilation> _key;
    CompilationResult<SemanticCompilation> _unknown;
    CompilationResult<SemanticCompilation> _command;
    CompilationResult<SemanticCompilation> _composed;
    CompilationResult<SemanticCompilation> _inherited;

    void Because()
    {
        _filter = Bind(Source);
        _key = Bind(Source.Replace("filter status String", "by status String", StringComparison.Ordinal));
        _unknown = Bind(Source.Replace("matches status", "matches unknown", StringComparison.Ordinal));
        _command = Bind(
            """
            policy Access
              require claim "status" matches status
            module Portal
              feature Reports
                slice StateChange FileReport
                  command FileReport
                    reportId String
                    authorize Access
            """);
        _composed = Bind(Source.Replace("policy Access", "policy SignedIn\n  require authenticated\npolicy Access", StringComparison.Ordinal)
            .Replace("authorize Access", "authorize SignedIn and Access", StringComparison.Ordinal));
        _inherited = Bind(Source.Replace("module Portal", "module Portal\n  authorize Access", StringComparison.Ordinal)
            .Replace("        authorize Access", string.Empty, StringComparison.Ordinal));
    }

    [Fact] void should_refuse_the_filter_policy() => _filter.Success.ShouldBeFalse();
    [Fact] void should_keep_the_diagnostic_code() => _filter.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidSemanticBinding);
    [Fact] void should_explain_the_filter_refusal() => _filter.Diagnostics.Single().Message.ShouldEqual("Policy 'Access' artifact path 'status' names query filter 'status'. Policies read only the query's 'by' argument, not filters. Declare 'status' with 'by' to authorize by it.");
    [Fact] void should_still_bind_a_policy_on_the_by_argument() => _key.Success.ShouldBeTrue();
    [Fact] void should_bind_the_key_without_diagnostics() => _key.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_the_unknown_path_code() => _unknown.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidSemanticBinding);
    [Fact] void should_keep_the_unknown_path_message() => _unknown.Diagnostics.Single().Message.ShouldEqual("Policy 'Access' artifact path 'unknown' does not resolve against the authorized command properties or query arguments.");
    [Fact] void should_keep_the_command_path_message() => _command.Diagnostics.Single().Message.ShouldEqual("Policy 'Access' artifact path 'status' does not resolve against the authorized command properties or query arguments.");
    [Fact] void should_explain_the_filter_refusal_in_a_composed_gate() => _composed.Diagnostics.Single().Message.ShouldEqual(_filter.Diagnostics.Single().Message);
    [Fact] void should_explain_the_filter_refusal_in_an_inherited_gate() => _inherited.Diagnostics.Single().Message.ShouldEqual(_filter.Diagnostics.Single().Message);
}
