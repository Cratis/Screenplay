// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Extensions;

namespace Cratis.Screenplay.Semantics.Serialization.given;

/// <summary>
/// Supplies the ESM v56 golden: every v55 construct plus screens declared free of any slice and the slices that adopt them.
/// </summary>
public static partial class canonical_serialization_golden_vectors
{
    /// <summary>
    /// The source of the v56 golden: the v55 golden plus free screens at the top level and in a module, and a slice adopting both.
    /// </summary>
    public static string FreeScreensSource => ExpectedTagsSource + """


        shape Parcel
          description "What the parcel screens read"
          number String
          weight Decimal

        screen ParcelBoard
          title "Parcels"
          data Parcel[] as waiting
          data Parcel[] as shipped
          table waiting
            column number label "Waiting"
            column weight
          table shipped
            column number

        module Logistics
          screen ParcelDetails
            title "Parcel"
            data Parcel
            summary Parcel
              field number label "Number"

          feature Parcels
            slice StateView ParcelViews
              event ParcelRegistered
                parcelId Uuid
                number String
                weight Decimal
              readmodel ParcelRow
                parcelId Uuid key
                number String
                weight Decimal
              projection ParcelRows => ParcelRow
                from ParcelRegistered key parcelId
                  number = number
                  weight = weight
              query AllParcels => ParcelRow[]
              query ParcelByKey => ParcelRow optional
                by parcelId Uuid
              uses screen ParcelBoard
                data waiting via query AllParcels
                data shipped via query AllParcels
              uses screen ParcelDetails
                data Parcel via query ParcelByKey
        """;

    /// <summary>
    /// Gets the canonical bytes of the v56 golden.
    /// </summary>
    public static byte[] EsmV56Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v56.json");

    /// <summary>
    /// Creates the v56 golden model.
    /// </summary>
    /// <returns>The model.</returns>
    public static ExecutableSemanticModel CreateSemanticModelV56()
    {
        using var fixture = ExtensionFixture.Locked("acme/wallet", "1", ExtensionComponentContract);
        var previous = ExtensionContractResolver.Current;
        ExtensionContractResolver.Current = fixture.Resolver();
        try
        {
            var result = property_order_binding.AdmitScreens(FreeScreensSource);
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
