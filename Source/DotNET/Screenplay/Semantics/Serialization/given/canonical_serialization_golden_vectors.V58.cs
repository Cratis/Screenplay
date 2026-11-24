// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Extensions;

namespace Cratis.Screenplay.Semantics.Serialization.given;

/// <summary>
/// Supplies the ESM v58 golden: the v57 model with a negated audience in an availability gate.
/// </summary>
public static partial class canonical_serialization_golden_vectors
{
    /// <summary>
    /// The v57 source with the audience of one gate negated and the lifecycle specifications' OrderPacked payloads complete
    /// (the frozen originals omit the required parcelId; Screenplay#1367).
    /// </summary>
    public static string NegatedAudienceSource => Require(
        Require(
            Require(CodeProvidedValueSource, "available for Plus and Preview", "available for Plus and not Preview"),
            "    when append OrderPacked\n      for \"9c858901-8a57-4791-81fe-4c455b099bc9\"\n    then is Packed",
            "    when append OrderPacked\n      for \"9c858901-8a57-4791-81fe-4c455b099bc9\"\n      parcelId = \"9c858901-8a57-4791-81fe-4c455b099bc9\"\n    then is Packed"),
        "      for \"9c858901-8a57-4791-81fe-4c455b099bc9\"\n    then invokes Carrier.BookPickup\n    then invokes Carrier.CancelPickup",
        "      for \"9c858901-8a57-4791-81fe-4c455b099bc9\"\n      parcelId = \"9c858901-8a57-4791-81fe-4c455b099bc9\"\n    then invokes Carrier.BookPickup\n    then invokes Carrier.CancelPickup");

    static string Require(string source, string text, string replacement) =>
        source.Contains(text, StringComparison.Ordinal)
            ? source.Replace(text, replacement, StringComparison.Ordinal)
            : throw new InvalidSemanticContract($"The v58 golden source no longer contains '{text}'.");

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
            var result = property_order_binding.AdmitScreens(NegatedAudienceSource);
            if (!result.Success) throw new InvalidSemanticContract(string.Join('\n', result.Refusals));
            return WithoutGraphProjectionDeclaringSlices(result.Compilation!.Model, LanguageVersion.V58, SemanticVersion.V58);
        }
        finally
        {
            ExtensionContractResolver.Current = previous;
        }
    }
}
#endif
