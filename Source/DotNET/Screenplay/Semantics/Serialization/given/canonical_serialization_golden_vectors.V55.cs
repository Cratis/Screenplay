// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Extensions;

namespace Cratis.Screenplay.Semantics.Serialization.given;

/// <summary>
/// Supplies the ESM v55 golden: every v54 construct plus a provided value computed by an inline code block.
/// </summary>
public static partial class canonical_serialization_golden_vectors
{
    /// <summary>
    /// The source of the v55 golden: the v54 golden plus an application-level provided value computed by code.
    /// </summary>
    public const string CodeProvideSource = GraphCollectSource + """


        provide ceiling Int
          ```csharp
          return 50000;
          ```
        """;

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
            var result = property_order_binding.AdmitScreens(CodeProvideSource);
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
