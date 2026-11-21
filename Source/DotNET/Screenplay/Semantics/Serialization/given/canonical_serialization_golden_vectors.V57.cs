// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Extensions;

namespace Cratis.Screenplay.Semantics.Serialization.given;

/// <summary>
/// Supplies the ESM v57 golden: the v56 model plus a provided value computed by an opaque code attachment.
/// </summary>
public static partial class canonical_serialization_golden_vectors
{
    /// <summary>
    /// The v56 source with an application-level provided value computed by code.
    /// </summary>
    public static string CodeProvidedValueSource => FreeScreensSource + """


        provide ceiling Int
          ```csharp
          return 50000;
          ```
        """;

    /// <summary>
    /// Gets the canonical bytes of the v57 golden.
    /// </summary>
    public static byte[] EsmV57Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v57.json");

    /// <summary>
    /// Creates the v57 golden model.
    /// </summary>
    /// <returns>The model.</returns>
    public static ExecutableSemanticModel CreateSemanticModelV57()
    {
        using var fixture = ExtensionFixture.Locked("acme/wallet", "1", ExtensionComponentContract);
        var previous = ExtensionContractResolver.Current;
        ExtensionContractResolver.Current = fixture.Resolver();
        try
        {
            var result = property_order_binding.AdmitScreens(CodeProvidedValueSource);
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
