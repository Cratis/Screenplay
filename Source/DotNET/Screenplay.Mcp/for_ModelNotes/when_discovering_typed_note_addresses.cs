// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_discovering_typed_note_addresses : given.a_split_notes_workspace
{
    JsonElement _proposal;
    JsonElement _note;

    void Establish() => OpenSplit();

    void Because()
    {
        var target = Arguments(new
        {
            kind = "Module",
            parts = new[]
            {
                new { kind = "Application", key = Opened.GetProperty("applicationIdentity").GetString()! },
                new { kind = "Module", key = "Orders" }
            }
        });
        _proposal = ProposeNote(target);
        _note = Review(_proposal);
    }

    [Fact] void should_report_the_canonical_application_identity() => Opened.GetProperty("applicationIdentity").GetString().ShouldEqual(Split.IdentityCatalog.Application.ToString());
    [Fact] void should_accept_the_discovered_typed_address() => _proposal.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_anchor_to_the_requested_module() => _note.GetProperty("lastAddress").GetProperty("kind").GetString().ShouldEqual("Module");
    [Fact] void should_not_substitute_the_discovered_address() => _proposal.GetProperty("substitutions").GetArrayLength().ShouldEqual(0);
}
