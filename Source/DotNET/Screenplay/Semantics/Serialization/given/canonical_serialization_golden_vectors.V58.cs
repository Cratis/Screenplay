// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Extensions;

namespace Cratis.Screenplay.Semantics.Serialization.given;

/// <summary>
/// Supplies the ESM v58 golden: the v57 source plus a graph projection with its declaring slice.
/// </summary>
public static partial class canonical_serialization_golden_vectors
{
    /// <summary>
    /// Gets the source of the v58 golden.
    /// </summary>
    public static string GraphProjectionScopeSource => WithCompleteParcelLifecycleFacts(CodeProvidedValueSource) + """


        module Ownership
          feature Chart
            slice StateView OwnerChart
              event OwnerAdded
              graph Owners
                node Owner
                  ownerId Uuid key
              projection OwnerChart => graph Owners
                node Owner
                  from OwnerAdded key $eventSourceId
                    ownerId = $eventSourceId
        """;

    /// <summary>
    /// Gets the canonical bytes of the v58 golden.
    /// </summary>
    public static byte[] EsmV58Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v58.json");

    /// <summary>
    /// Creates the v58 golden model.
    /// </summary>
    /// <returns>The model.</returns>
    public static ExecutableSemanticModel CreateSemanticModelV58()
    {
        using var fixture = ExtensionFixture.Locked("acme/wallet", "1", ExtensionComponentContract);
        var previous = ExtensionContractResolver.Current;
        ExtensionContractResolver.Current = fixture.Resolver();
        try
        {
            var result = property_order_binding.AdmitScreens(GraphProjectionScopeSource);
            if (!result.Success) throw new InvalidSemanticContract(string.Join('\n', result.Refusals));
            return result.Compilation!.Model;
        }
        finally
        {
            ExtensionContractResolver.Current = previous;
        }
    }

    static string WithCompleteParcelLifecycleFacts(string source)
    {
        const string identity = "\"9c858901-8a57-4791-81fe-4c455b099bc9\"";
        var fact = $"\n    when append OrderPacked\n      for {identity}";
        return source.Replace(fact, $"{fact}\n      parcelId = {identity}", StringComparison.Ordinal);
    }
}
#endif
