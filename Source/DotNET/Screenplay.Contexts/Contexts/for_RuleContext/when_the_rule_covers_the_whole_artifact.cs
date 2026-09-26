// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Screenplay.Contexts.for_RuleContext;

public class when_the_rule_covers_the_whole_artifact : Specification
{
    dynamic _command;
    RuleContext _context;

    void Establish()
    {
        _command = new ExpandoObject();
        _command.orgNumber = "918273645";
    }

    void Because() => _context = new(_command, _command, string.Empty, TenantId.Default, CausedBy.NotSet, DateTimeOffset.UtcNow);

    [Fact] void should_cover_the_whole_artifact() => _context.IsWholeArtifact.ShouldBeTrue();
    [Fact] void should_carry_the_artifact_as_the_value() => ((object)_context.Value).ShouldEqual((object)_context.Artifact);
}
