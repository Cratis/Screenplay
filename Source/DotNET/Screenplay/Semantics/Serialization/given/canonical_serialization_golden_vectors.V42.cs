// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    /// <summary>
    /// The source of the v42 golden: the v41 golden plus a projection with set-once mappings and ordered children collections.
    /// </summary>
    public const string ProjectionParitySource = GraphForEachSource + """

        module Rosters
          feature Entries
            slice StateView RosterEntries
              event RosterOpened
                title PersonName
              event RosterEntryAdded
                entryId GroupId
                title PersonName

              readmodel RosterView
                rosterId PersonId key
                title PersonName
                openedBy PersonName optional
                latest RosterEntry[]
                ranked RosterEntry[]

              projection Rosters => RosterView
                from RosterOpened key $eventSourceId
                  rosterId = $eventSourceId
                  title = title
                  set once openedBy = $causedBy.name
                children latest identified by entryId
                  insert first
                  from RosterEntryAdded key entryId
                    parent $eventSourceId
                children ranked identified by entryId
                  sort by title descending
                  from RosterEntryAdded key entryId
                    parent $eventSourceId

        type RosterEntry
          entryId GroupId
          title   PersonName
        """;

    public static byte[] EsmV42Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v42.json");

    public static ExecutableSemanticModel CreateSemanticModelV42()
    {
        var result = property_order_binding.BindDefault(ProjectionParitySource);
        if (!result.Success) throw new InvalidSemanticContract(string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        return result.Value!.Model;
    }
}
#endif
