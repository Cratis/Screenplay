// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.RegularExpressions;
using Cratis.Screenplay.Extensions;

namespace Cratis.Screenplay.Semantics.Serialization.given;

/// <summary>
/// Supplies the ESM v59 golden: the v58 model with availability gates on a screen action and a screen data element.
/// </summary>
public static partial class canonical_serialization_golden_vectors
{
    /// <summary>
    /// The v58 source with one gate on the OpenTicket action and one on the data the board shows.
    /// </summary>
    public static string ElementGateSource => WithGate(
        WithGate(NegatedAudienceSource, "label \"Open\"", "available for Plus and not Preview", ""),
        "data Ticket[] via query AllTickets",
        "available for Plus",
        "  ");

    /// <summary>
    /// Gets the canonical bytes of the v59 golden.
    /// </summary>
    public static byte[] EsmV59Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v59.json");

    /// <summary>
    /// Creates the v59 golden model.
    /// </summary>
    /// <returns>The model.</returns>
    public static ExecutableSemanticModel CreateSemanticModelV59()
    {
        using var fixture = ExtensionFixture.Locked("acme/wallet", "1", ExtensionComponentContract);
        var previous = ExtensionContractResolver.Current;
        ExtensionContractResolver.Current = fixture.Resolver();
        try
        {
            var result = property_order_binding.AdmitScreens(ElementGateSource);
            if (!result.Success) throw new InvalidSemanticContract(string.Join('\n', result.Refusals));
            return result.Compilation!.Model;
        }
        finally
        {
            ExtensionContractResolver.Current = previous;
        }
    }

    // Writes the gate on its own line after a line, indented from that line by the extra spaces given.
    static string WithGate(string source, string line, string gate, string extra)
    {
        var pattern = new Regex("^( *)" + Regex.Escape(line) + "\n", RegexOptions.Multiline | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(5));
        return pattern.IsMatch(source)
            ? pattern.Replace(source, match => $"{match.Value}{match.Groups[1].Value}{extra}{gate}\n", 1)
            : throw new InvalidSemanticContract($"The v59 golden source no longer contains '{line}'.");
    }
}
#endif
