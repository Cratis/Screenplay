// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_describing_scoped_policy_uses : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind("""
        policy Access
          ```csharp
          return true;
          ```
        policy FeatureAccess
          ```csharp
          return true;
          ```
        module Billing
          authorize Access
          feature Accounts
            authorize (FeatureAccess or FeatureAccess) and FeatureAccess
            slice StateChange Commands
              command Deposit
                amount Decimal
              command Withdraw
                amount Decimal
                authorize FeatureAccess or FeatureAccess
        """);

    [Fact] void should_bind_the_scoped_authorization() => _result.Success.ShouldBeTrue();
    [Fact] void should_publish_one_use_site_per_command_even_with_repeated_nested_references()
    {
        var descriptors = _result.TypedContextDescriptors.Where(value => value.Role == SemanticImplementationRole.PolicyPredicate).ToArray();
        descriptors.Length.ShouldEqual(4);
        descriptors.GroupBy(value => value.RequirementId).Count().ShouldEqual(2);
        descriptors.GroupBy(value => value.RequirementId).All(group => group.Select(value => value.OperationId).Distinct().Count() == 2).ShouldBeTrue();
    }
    [Fact] void should_mark_a_subject_without_an_identifier_unavailable() => _result.TypedContextDescriptors[0].Members[1].Source.Kind.ShouldEqual(SemanticContextSourceKinds.Unavailable);
}
