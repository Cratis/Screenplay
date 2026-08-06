// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_compiling_a_folder.given;

public class a_folder_of_play_files : Specification
{
    protected DirectoryInfo _root;
    protected PlayFileCompiler _compiler;

    void Establish()
    {
        _root = Directory.CreateTempSubdirectory("playfolder");
        _compiler = new();
    }

    protected void Write(string relativePath, string content)
    {
        var path = Path.Combine(_root.FullName, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    void Destroy() => _root.Delete(true);
}
