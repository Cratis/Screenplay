// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Screenplay.Contexts.for_RuleContext;

public class when_the_rule_covers_a_property : Specification
{
    dynamic _command;
    RuleContext _context;

    void Establish()
    {
        _command = new ExpandoObject();
        _command.orgNumber = "918273645";
    }

    void Because() => _context = new(_command, _command.orgNumber, "orgNumber", TenantId.Default, CausedBy.NotSet, DateTimeOffset.UtcNow);

    [Fact] void should_not_cover_the_whole_artifact() => _context.IsWholeArtifact.ShouldBeFalse();
    [Fact] void should_carry_the_value_of_the_property() => ((string)_context.Value).ShouldEqual("918273645");
    [Fact] void should_carry_the_path_of_the_property() => _context.Property.ShouldEqual("orgNumber");
}
