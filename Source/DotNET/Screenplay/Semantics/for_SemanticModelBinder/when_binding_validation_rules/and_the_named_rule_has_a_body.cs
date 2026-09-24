// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_validation_rules;

public class and_the_named_rule_has_a_body : given.a_validated_command
{
    void Because() => _result = BindRules("name rule BeUnique", "  file Validations/BeUnique.cs");

    [Fact] void should_bind_v3() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V3);
    [Fact] void should_bind_the_predicate() => Rule.Kind.ShouldEqual(SemanticValidationRuleKind.RulePredicate);
    [Fact] void should_preserve_the_name() => Rule.Name.ShouldEqual("BeUnique");
    [Fact] void should_reference_the_attachment() => Rule.RequirementId.ShouldEqual(_result.ImplementationRequirements.Single().RequirementId);
    [Fact] void should_require_pure_capability() => _result.ImplementationRequirements.Single().RequiredCapability.ShouldEqual("pure");
    [Fact] void should_list_the_named_rule() => _result.ImplementationRequirements.Single().Role.ShouldEqual(SemanticImplementationRole.RulePredicate);
    [Fact] void should_address_the_named_rule() => _result.ImplementationRequirements.Single().Member.ShouldEqual("name/BeUnique");
}
