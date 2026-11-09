// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

/// <summary>
/// Supplies the ESM v43 golden: every v42 construct plus set-once projection mappings and ordered children collections.
/// </summary>
public static partial class canonical_serialization_golden_vectors
{
    /// <summary>
    /// The source of the v43 golden: the v42 golden plus a projection with set-once mappings and ordered children collections.
    /// </summary>
    public const string ProjectionParitySource = ScreensSource + """

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

    /// <summary>
    /// Gets the canonical bytes of the v43 golden.
    /// </summary>
    public static byte[] EsmV43Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v43.json");

    /// <summary>
    /// Creates the v43 golden model.
    /// </summary>
    /// <returns>The model.</returns>
    public static ExecutableSemanticModel CreateSemanticModelV43()
    {
        var result = property_order_binding.AdmitScreens(ProjectionParitySource);
        if (!result.Success) throw new InvalidSemanticContract(string.Join('\n', result.Refusals));
        return result.Compilation!.Model;
    }
}
#endif
