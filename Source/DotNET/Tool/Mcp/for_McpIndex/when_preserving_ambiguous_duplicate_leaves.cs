// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Tool.Mcp.for_McpIndex;

public class when_preserving_ambiguous_duplicate_leaves : Specification
{
    const string Source = """
        module Billing
          feature Accounts
            slice StateChange Register
              command Register
                produces Opened
              event Opened
        """;
    McpSnapshot _snapshot = null!;
    McpSyntaxIndex _index = null!;

    void Establish() => _snapshot = new([given.synthetic_model.Document("first", Source), given.synthetic_model.Document("second", Source)]);

    void Because() => _index = _snapshot.Index;

    [Fact] void should_keep_both_duplicate_events() => _index.Find("Billing.Accounts.Register.Opened", "Event").Length.ShouldEqual(2);
    [Fact] void should_keep_both_ambiguous_candidates_for_each_reference() => _index.References.All(reference => _index.Resolve(reference).Length == 2).ShouldBeTrue();
    [Fact] void should_keep_both_incoming_ambiguous_references() => _index.Incoming(_index.Find("Billing.Accounts.Register.Opened", "Event")[0]).Count().ShouldEqual(2);
    [Fact] void should_preserve_all_explicitly_qualified_candidates() => _index.Resolve(new("Accounts.Register.Opened", ["Event"], [], SourceLocation.Start)).Length.ShouldEqual(2);
    [Fact] void should_resolve_each_source_occurrence() => _index.References.Count().ShouldEqual(2);
    [Fact] void should_keep_folder_diagnostics() => _snapshot.Compilation.Success.ShouldBeFalse();
    [Fact] void should_index_outgoing_references_from_both_owner_occurrences() => _index.Outgoing("Billing.Accounts.Register.Register").Count().ShouldEqual(2);
    [Fact] void should_expose_both_resolved_source_occurrences() => _index.ResolvedReferences.Count(resolution => resolution.Candidates.Length == 2).ShouldEqual(2);
}
