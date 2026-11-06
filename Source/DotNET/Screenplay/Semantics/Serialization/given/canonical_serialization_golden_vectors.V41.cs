// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    /// <summary>
    /// The source of the v41 golden: the v40 golden plus a graph projection whose handlers run once per item of an event collection.
    /// </summary>
    public const string GraphForEachSource = GraphQuerySource + """

            slice StateView ModelScores
              event ModelDefined
                slots ModelSlot[]
              event ModelRetired
                slots ModelSlot[]

              graph Scoring
                node Model
                  modelId PersonId key
                node Series
                  seriesId GroupId key
                edge SCORES source Model target Series identified by slot
                  slot PersonName

              projection ModelScores => graph Scoring
                node Model
                  from ModelDefined key $eventSourceId
                node Series
                  from ModelDefined foreach slots key slots.series
                edge SCORES
                  from ModelDefined foreach slots source $eventSourceId target slots.series identified by slots.slot
                    slot = slots.slot
                  remove with ModelRetired foreach slots source $eventSourceId target slots.series identified by slots.slot

              specification EachSlotIsAnEdge
                given ModelDefined
                  for "6fa459ea-ee8a-4ca4-894e-db77e160355e"
                  slots
                    item
                      slot = "fuel"
                      series = "0f8fad5b-d9cb-469f-a165-70867728950e"
                then graph Scoring
                  has edge SCORES from "6fa459ea-ee8a-4ca4-894e-db77e160355e" to "0f8fad5b-d9cb-469f-a165-70867728950e" identified by "fuel"
                    slot = "fuel"

        type ModelSlot
          slot   PersonName
          series GroupId
        """;

    public static byte[] EsmV41Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v41.json");

    public static ExecutableSemanticModel CreateSemanticModelV41()
    {
        var result = property_order_binding.BindDefault(GraphForEachSource);
        if (!result.Success) throw new InvalidSemanticContract(string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        return result.Value!.Model;
    }
}
#endif
