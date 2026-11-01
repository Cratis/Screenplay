// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalVectors.Specs.LegacyReader;

internal static class Program
{
    static void Main(string[] args)
    {
        var document = UnknownPreservingSemanticModelReader.Read(File.ReadAllBytes(args[0]), new(34, 0, 1));
        var unknown = document.Unknowns.Single();
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Version = document.Model.SemanticVersion.ToString(),
            Revision = document.Revision.ToString(),
            Bytes = Convert.ToBase64String(document.CopyBytes()),
            Projection = Convert.ToBase64String(SemanticModelSerializer.Serialize(document.Model)),
            unknown.Name,
            Owner = unknown.Owner.ToString(),
            Value = Convert.ToBase64String(unknown.Value.ToArray())
        }));
    }
}
