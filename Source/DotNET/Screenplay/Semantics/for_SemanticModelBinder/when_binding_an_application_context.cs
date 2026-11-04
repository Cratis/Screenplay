// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_an_application_context : given.a_semantic_binder
{
    [Fact]
    void should_bind_declared_time_zone_members_at_v37()
    {
        var result = Bind("context\n  businessTimeZone time zone\n  customerTimeZone time zone\n");
        result.Success.ShouldBeTrue();
        result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V37);
        result.Value.Model.Application.ContextMembers.ShouldContainOnly("businessTimeZone", "customerTimeZone");
    }

    [Fact]
    void should_bind_an_empty_declaration_without_selecting_v37()
    {
        var result = Bind("context\n");
        result.Success.ShouldBeTrue();
        result.Value!.Model.SemanticVersion.ShouldNotEqual(SemanticVersion.V37);
        result.Value.Model.Application.ContextMembers.ShouldBeEmpty();
    }
}
