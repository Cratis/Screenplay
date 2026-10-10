// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Files.for_PlayFolderMerge;

public class when_merging_nested_identity_expressions : Specification
{
    CompilationResult<ApplicationSyntax> _result;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        var policy = compiler.Parse("policy P\n  require claim \"x\" matches id", "policy.play");
        var location = new SourceLocation(2, 3).In("policy.play");
        var original = policy.Value!.Policies.Single();
        var condition = (ClaimConditionSyntax)original.Condition!;
        var expression = new ListExpressionSyntax(
            [
                new IdentityExpressionSyntax("department", location),
                new ObjectExpressionSyntax([new("nested", new IdentityExpressionSyntax("missingObject", location), location)], location),
                new TemplateExpressionSyntax([new TemplateInterpolationSyntax(new IdentityExpressionSyntax("missingTemplate", location), location)], location)
            ],
            location);
        policy = policy with { Value = policy.Value with { Policies = [original with { Condition = condition with { Matches = expression } }] } };
        var metadata = compiler.Parse("identity\n  department String from claim \"department\"", "application.play");
        _result = PlayFolderMerge.Merge([policy, metadata]);
    }

    [Fact] void should_find_identity_reads_in_objects_and_templates_nested_in_a_list() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownContextIdentityProperty).Select(diagnostic => diagnostic.Message.Split('\'')[1]).ShouldEqual(["missingObject", "missingTemplate"]);
    [Fact] void should_not_warn_for_the_detail_declared_in_another_file() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownContextIdentityProperty && diagnostic.Message.Contains("'department'", StringComparison.Ordinal)).ShouldBeFalse();
}
