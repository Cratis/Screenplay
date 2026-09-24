// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler;

public class when_merging_scoped_authorization : when_compiling_a_folder.given.a_folder_of_play_files
{
    ApplicationCompilation<ApplicationSyntax> _compilation;
    ModuleSyntax _module;

    void Establish()
    {
        Write("application.play", """
            policy Access
              require authenticated
            policy Staff
              require authenticated
            policy Finance
              require authenticated
            policy Extra
              require authenticated
            """);
        Write(Path.Combine("Portal", "Portal.play"), """
            module Portal
              authorize Access
              feature Orders
                authorize Staff
            """);
        Write(Path.Combine("Portal", "Orders", "Orders.play"), """
            module Portal
              authorize Finance
              feature Orders
                authorize Finance
            """);
        Write(Path.Combine("Portal", "Orders", "Returns", "Returns.play"), """
            module Portal
              authorize Access
              feature Orders
                authorize Staff
                feature Returns
                  authorize Extra
            """);
    }

    void Because()
    {
        _compilation = _compiler.CompileFolder(_root.FullName);
        _module = _compilation.Result.Value!.Modules.Single();
    }

    [Fact] void should_compose_distinct_module_gates() => _module.Authorize!.References().Select(_ => _.Name).ShouldContainOnly("Access", "Finance");
    [Fact] void should_require_both_module_gates() => ((LogicalPolicyRequirementSyntax)_module.Authorize!.Requirement).Operator.ShouldEqual(LogicalOperator.And);
    [Fact] void should_compose_distinct_feature_gates() => _module.Features.Single().Authorize!.References().Select(_ => _.Name).ShouldContainOnly("Staff", "Finance");
    [Fact] void should_require_both_feature_gates() => ((LogicalPolicyRequirementSyntax)_module.Features.Single().Authorize!.Requirement).Operator.ShouldEqual(LogicalOperator.And);
    [Fact] void should_keep_the_nested_gate() => _module.Features.Single().Features.Single().Authorize!.References().Single().Name.ShouldEqual("Extra");
    [Fact] void should_report_both_duplicates() => _compilation.Result.Diagnostics.Select(_ => _.Code).ShouldContainOnly(DiagnosticCodes.DuplicateAuthorizationAcrossFiles, DiagnosticCodes.DuplicateAuthorizationAcrossFiles);
    [Fact] void should_report_warnings() => _compilation.Result.Diagnostics.All(_ => _.Severity == DiagnosticSeverity.Warning).ShouldBeTrue();
    [Fact] void should_keep_compilation_successful() => _compilation.Result.Success.ShouldBeTrue();
}
