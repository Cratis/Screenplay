// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_portable_policies : given.a_semantic_binder
{
    const string Source =
        """
        policy Access
          require authenticated and (role "Admin" or claim "department" matches department)
        module Portal
          authorize Access
          feature Reports
            slice StateChange FileReport
              command FileReport
                department String
                authorize Access
              specification Authorized
                given caller
                  authenticated
                  role "Admin"
                when FileReport
                  department = "Finance"
                then denied
        """;

    CompilationResult<SemanticCompilation> _result;
    SemanticCommand _command;

    void Because()
    {
        _result = Bind(Source);
        if (_result.Success) _command = _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
    }

    [Fact] void should_bind_the_policy_and_caller() => _result.Success.ShouldBeTrue();
    [Fact] void should_retain_parenthesis_grouping() => ((SemanticLogicalPolicyCondition)_result.Value!.Model.Application.Policies.Single().Condition).Right.ShouldBeOfExactType<SemanticLogicalPolicyCondition>();
    [Fact] void should_compose_the_module_with_the_command() => _command.Authorization.ShouldBeOfExactType<SemanticLogicalAuthorization>();
    [Fact] void should_bind_the_denied_assertion() => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single().ThenDenied.ShouldBeTrue();
}

public class when_binding_an_unresolved_policy_target : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(
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

    [Fact] void should_reject_the_unresolvable_artifact_path() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding && diagnostic.Message.Contains("unknown", StringComparison.Ordinal)).ShouldBeTrue();
}

public class when_binding_an_authorized_specification_without_a_caller : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(
        """
        policy Access
          require authenticated
        module Portal
          feature Reports
            slice StateChange FileReport
              command FileReport
                authorize Access
              specification MissingCaller
                when FileReport
                then denied
        """);

    [Fact] void should_report_missing_identity_context() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.MissingSpecificationCaller).ShouldBeTrue();
}

public class when_binding_an_inline_csharp_policy : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(
        """
        policy Access
          ```csharp
          return true;
          ```
        module Portal
          feature Reports
            slice StateChange FileReport
              command FileReport
                authorize Access
        """);

    [Fact] void should_block_implementation_until_139() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains("#139", StringComparison.Ordinal)).ShouldBeTrue();
}
