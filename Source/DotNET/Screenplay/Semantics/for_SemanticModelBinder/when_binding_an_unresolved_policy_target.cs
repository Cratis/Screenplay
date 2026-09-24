// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_an_unresolved_policy_target : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;
    CompilationResult<SemanticCompilation> _collection;

    void Because()
    {
        _result = Bind(
            """
            policy Access
              require claim "department" matches unknown
            module Portal
              feature Reports
                slice StateChange FileReport
                  command FileReport
                    department String
                    authorize Access
            """);
        _collection = Bind(
            """
            type Details
              department String
            policy Access
              require claim "department" matches details.department
            module Portal
              feature Reports
                slice StateChange FileReport
                  command FileReport
                    details Details[]
                    authorize Access
            """);
    }

    [Fact] void should_reject_the_unresolvable_artifact_path() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding && diagnostic.Message.Contains("unknown", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_reject_a_collection_traversal() => _collection.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding && diagnostic.Message.Contains("details.department", StringComparison.Ordinal)).ShouldBeTrue();
}
