// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.for_SemanticMatchPattern;

static class PortableMatchVectors
{
    internal static JsonDocument Read(string name) => JsonDocument.Parse(File.ReadAllText(Path.Combine(Root(), "Source/Screenplay/Compiler/Conformance", name)));

    internal static string Root([CallerFilePath] string path = "")
    {
        var root = Directory.GetParent(path);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "AGENTS.md"))) root = root.Parent;

        return root!.FullName;
    }
}
