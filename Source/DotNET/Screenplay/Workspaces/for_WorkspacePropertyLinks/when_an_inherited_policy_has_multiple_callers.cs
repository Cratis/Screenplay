// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_an_inherited_policy_has_multiple_callers : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink _link;

    void Establish() => _links = Create("""
        policy Owns
          require claim "owner" matches ownerId
        module M
          authorize Owns
          feature F
            slice StateChange S
              command Update
                ownerId String
              command Delete
                ownerId String
            feature Ungated
              slice StateChange T
                command Other
                  ownerId String
        module Elsewhere
          feature F
            slice StateChange S
              command Other
                ownerId String
        """);
    void Because() => _link = _links.Incoming(_links.Find("M.F.S.Update.ownerId").Single()).Single();

    [Fact] void should_keep_all_authorized_callers() => _link.Candidates.Length.ShouldEqual(3);
    [Fact] void should_report_ambiguity() => _link.Resolution.ShouldEqual("ambiguous");
    [Fact] void should_not_link_an_ungated_command() => _links.Incoming(_links.Find("Elsewhere.F.S.Other.ownerId").Single()).ShouldBeEmpty();
}
