// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Api.for_ApiDescription;

public class when_describing_authorization : given.a_lending_model
{
    ApiDescription _description;

    void Because() => _description = ApiDescription.For(Model);

    ApiAuthorization Of(string name) => _description.Operations.Single(operation => operation.Name == name).Authorization;

    [Fact] void should_open_an_operation_without_authorization_to_every_caller() => Of("ShelveBook").Anonymous.ShouldBeTrue();
    [Fact] void should_require_the_declared_policy() => Of("AddBook").Requires.ShouldEqual("Librarian");
    [Fact] void should_keep_alternatives_as_written() => Of("RetireBook").Requires.ShouldEqual("Librarian or Member");
    [Fact] void should_list_the_policies_referred_to() => Of("RetireBook").Policies.ShouldContainOnly(["Librarian", "Member"]);
    [Fact] void should_not_be_anonymous_when_a_policy_is_required() => Of("AllBooks").Anonymous.ShouldBeFalse();
}
