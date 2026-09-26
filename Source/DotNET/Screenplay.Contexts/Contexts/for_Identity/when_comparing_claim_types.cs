// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts.for_Identity;

public class when_comparing_claim_types : Specification
{
    readonly Identity _identity = new("id", "name", "user", true, ["Editor"], [new("DEPARTMENT", "Finance"), new("department", "Engineering")]);

    [Fact] void should_find_claim_types_without_casing() => _identity.HasClaim("Department").ShouldBeTrue();
    [Fact] void should_preserve_multiple_claim_values() => _identity.ClaimValues("department").ShouldContainOnly("Finance", "Engineering");
    [Fact] void should_keep_role_names_case_sensitive() => _identity.HasRole("editor").ShouldBeFalse();
}
