// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Extensions;

namespace Cratis.Screenplay.Semantics.Serialization.given;

/// <summary>
/// Supplies the ESM v52 golden: every v51 construct plus a screen that places a component an extension provides.
/// </summary>
public static partial class canonical_serialization_golden_vectors
{
    /// <summary>
    /// The contract of the extension the v52 golden places a component from.
    /// </summary>
    public const string ExtensionComponentContract = """
        extension Wallet
          display name "Wallet badge"
          description "A badge that shows the wallet balance"
          icon "assets/icon.svg"
          requires platform web

          ability Credit idempotent
            note String

          component Badge
            kind react
            platform web
            placeholder "placeholders/badge.html"
            property label String
            raises tapped
              value String

          frontend "frontend/index.js"
        """;

    /// <summary>
    /// The source of the v52 golden: the v51 golden plus a screen that places <c>Wallet.Badge</c>.
    /// </summary>
    public const string ExtensionComponentSource = AbilityInvocationSource + """


        system Wallet
          extension "acme/wallet" version 1

        module Storefront
          feature Basket
            slice StateChange TapBadge
              event BadgeTapped
                badgeId Uuid
              command TapBadge
                badgeId Uuid identifier
                produces BadgeTapped
                  for badgeId
                  badgeId = badgeId
            slice StateView BasketScreen
              screen Basket
                component Wallet.Badge badge
                  property label = "Wallet"
                  on tapped
                    execute TapBadge
        """;

    /// <summary>
    /// Gets the canonical bytes of the v52 golden.
    /// </summary>
    public static byte[] EsmV52Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v52.json");

    /// <summary>
    /// Creates the v52 golden model.
    /// </summary>
    /// <returns>The model.</returns>
    public static ExecutableSemanticModel CreateSemanticModelV52()
    {
        using var fixture = ExtensionFixture.Locked("acme/wallet", "1", ExtensionComponentContract);
        var previous = ExtensionContractResolver.Current;
        ExtensionContractResolver.Current = fixture.Resolver();
        try
        {
            var result = property_order_binding.AdmitScreens(ExtensionComponentSource);
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
