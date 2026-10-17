// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

public static class feature_corpus_vectors
{
    public static byte[] Read(string path)
    {
        var parts = path.Split('/');
        var directory = string.Join('.', parts[..^1]).Replace('-', '_');
        var name = $"Cratis.Screenplay.FeatureCorpus.{directory}.{parts[^1]}";
        using var stream = typeof(feature_corpus_vectors).Assembly.GetManifestResourceStream(name) ??
            throw new InvalidSemanticContract($"Feature corpus vector '{name}' is missing.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);

        return buffer.ToArray();
    }
}
#endif
