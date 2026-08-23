// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.for_StableIdentityContracts;

public class when_deriving_a_query_argument_identity : Specification
{
    SemanticAddress _argument;
    SemanticId _argumentId;
    SemanticId _propertyId;

    void Because()
    {
        var application = ApplicationIdentity.Create("Projects");
        var slice = SemanticAddress.ForSlice(application, "Projects", "Projects", "Summaries");
        var query = SemanticAddress.ForQuery(slice, "ProjectById");
        _argument = SemanticAddress.ForQueryArgument(query, "id");
        _argumentId = SemanticId.Create(_argument);
        var readModel = SemanticAddress.ForReadModel(slice, "ProjectById");
        _propertyId = SemanticId.Create(SemanticAddress.ForProperty(readModel, "id"));
    }

    [Fact] void should_have_a_query_argument_kind() => _argument.Kind.ShouldEqual(SemanticKind.QueryArgument);
    [Fact] void should_retain_the_query_owner_kind() => _argument.OwnerKind.ShouldEqual(SemanticKind.Query);
    [Fact] void should_be_nested_below_the_query() => _argument.Parts[^3].Key.ShouldEqual("ProjectById");
    [Fact] void should_not_collide_with_a_real_property_of_the_same_names() => _argumentId.ShouldNotEqual(_propertyId);
}
#endif
