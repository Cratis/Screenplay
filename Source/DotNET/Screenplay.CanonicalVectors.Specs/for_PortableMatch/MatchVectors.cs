// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_PortableMatch;

internal static class MatchVectors
{
    internal static JsonDocument Read(string name, [CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;

        return JsonDocument.Parse(File.ReadAllText(Path.Combine(directory!.FullName, "Source/Screenplay/Compiler/Conformance", name)));
    }
}
