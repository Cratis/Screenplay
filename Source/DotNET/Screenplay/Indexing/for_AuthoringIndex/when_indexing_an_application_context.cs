// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Indexing.for_AuthoringIndex;

public class when_indexing_an_application_context : Specification
{
    [Fact]
    void should_index_members_in_context_scope_without_claiming_execution()
    {
        var index = new AuthoringSnapshot(new Dictionary<string, string>
        {
            ["application.play"] = "context\n  businessTimeZone time zone\n"
        }).Index;
        var block = index.Declarations.Single(declaration => declaration.Kind == "Context");
        block.Name.ShouldEqual("context");
        var member = index.Declarations.Single(declaration => declaration.Kind == "ContextMember");
        member.Address.ShouldEqual("context.businessTimeZone");
        member.Location.Line.ShouldEqual(2);
        var details = JsonSerializer.SerializeToElement(member.Details);
        details.GetProperty("syntaxOnly").GetBoolean().ShouldBeTrue();
        details.GetProperty("executionReadiness").GetString().ShouldContain("#824");
    }
}
