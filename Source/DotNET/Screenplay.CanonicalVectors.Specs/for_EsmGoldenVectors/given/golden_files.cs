// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_EsmGoldenVectors.given;

public class golden_files : Specification
{
    protected string _goldenFolder;

    void Establish() => _goldenFolder = Folder();

    static string Folder([CallerFilePath] string path = "")
    {
        var root = Path.GetFullPath("../../../../..", Path.GetDirectoryName(path)!);

        return Path.Combine(root, "Source/DotNET/Screenplay/Semantics/Serialization/Golden");
    }
}
