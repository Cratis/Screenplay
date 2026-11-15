// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Extensions;

namespace Cratis.Screenplay.Semantics.Serialization.given;

/// <summary>
/// Supplies the ESM v55 golden: every v54 construct plus exact ordered tag expectations on then events.
/// </summary>
public static partial class canonical_serialization_golden_vectors
{
    /// <summary>
    /// The source of the v55 golden: the v54 golden whose ReportShip slice asserts the tags of the event it appends.
    /// </summary>
    public static readonly string ExpectedTagsSource = GraphCollectSource.Replace(
        "        produces ShipReported\n          for shipId\n          shipId = shipId\n\n    slice StateView Fleet",
        """
                    produces ShipReported
                      for shipId
                      tag audit
                      shipId = shipId

                  specification ReportedShipsCarryTheirTags
                    when ReportShip
                      shipId = "9c858901-8a57-4791-81fe-4c455b099bc9"
                    then ShipReported
                      for "9c858901-8a57-4791-81fe-4c455b099bc9"
                      tag audit
                      shipId = "9c858901-8a57-4791-81fe-4c455b099bc9"

                slice StateView Fleet
            """.Replace("\r", string.Empty), StringComparison.Ordinal);

    /// <summary>
    /// Gets the canonical bytes of the v55 golden.
    /// </summary>
    public static byte[] EsmV55Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v55.json");

    /// <summary>
    /// Creates the v55 golden model.
    /// </summary>
    /// <returns>The model.</returns>
    public static ExecutableSemanticModel CreateSemanticModelV55()
    {
        using var fixture = ExtensionFixture.Locked("acme/wallet", "1", ExtensionComponentContract);
        var previous = ExtensionContractResolver.Current;
        ExtensionContractResolver.Current = fixture.Resolver();
        try
        {
            var result = property_order_binding.AdmitScreens(ExpectedTagsSource);
            if (!result.Success) throw new InvalidSemanticContract(string.Join('\n', result.Refusals));
            return result.Compilation!.Model;
        }
        finally
        {
            ExtensionContractResolver.Current = previous;
        }
    }
}
#endif
