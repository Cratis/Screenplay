// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.CanonicalVectors.Specs.given;

public static class canonical_serialization_golden_bytes
{
    const string SemanticModelResource = "Cratis.Screenplay.CanonicalVectors.Golden.full-esm-v1.json";
    const string SemanticModelV2Resource = "Cratis.Screenplay.CanonicalVectors.Golden.full-esm-v2.json";
    const string SemanticModelV3Resource = "Cratis.Screenplay.CanonicalVectors.Golden.full-esm-v3.json";
    const string IdentityCatalogResource = "Cratis.Screenplay.CanonicalVectors.Golden.full-identity-catalog-v1.json";

    public static byte[] SemanticModel => Read(SemanticModelResource);
    public static byte[] SemanticModelV2 => Read(SemanticModelV2Resource);
    public static byte[] SemanticModelV3 => Read(SemanticModelV3Resource);
    public static byte[] IdentityCatalog => Read(IdentityCatalogResource);

    static byte[] Read(string name)
    {
        using var stream = typeof(canonical_serialization_golden_bytes).Assembly.GetManifestResourceStream(name) ??
            throw new InvalidOperationException($"Embedded canonical vector '{name}' was not found.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
