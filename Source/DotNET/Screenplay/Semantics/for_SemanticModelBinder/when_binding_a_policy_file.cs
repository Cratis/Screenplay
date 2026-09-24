// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_a_policy_file : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _attached;
    CompilationResult<SemanticCompilation> _declarative;

    void Because()
    {
        _attached = Bind("policy Access\n  file Policies/Access.cs");
        _declarative = Bind("policy Access\n  require authenticated");
    }

    [Fact] void should_bind_a_file_attachment() => _attached.Success.ShouldBeTrue();
    [Fact] void should_reference_the_attachment() => ((SemanticOpaquePolicyCondition)_attached.Value!.Model.Application.Policies.Single().Condition).RequirementId.ShouldEqual(_attached.ImplementationRequirements.Single().RequirementId);
    [Fact] void should_list_the_file_attachment() => _attached.ImplementationRequirements.Single().File.ShouldEqual("Policies/Access.cs");
    [Fact] void should_type_the_policy_role() => _attached.ImplementationRequirements.Single().Role.ShouldEqual(SemanticImplementationRole.PolicyPredicate);
    [Fact] void should_not_list_the_declarative_alternative() => _declarative.ImplementationRequirements.ShouldBeEmpty();
    [Fact] void should_bind_the_declarative_alternative() => _declarative.Success.ShouldBeTrue();
    [Fact] void should_keep_the_declarative_policy() => _declarative.Value!.Model.Application.Policies.Single().Name.ShouldEqual("Access");
}
