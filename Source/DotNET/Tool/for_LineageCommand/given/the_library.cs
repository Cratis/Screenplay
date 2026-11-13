// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_LineageCommand.given;

public class the_library : for_ModelCheck.given.a_model
{
    protected const string Title = "Lending.Catalog.AddBook.AddBook.title";

    protected int ExitCode;

    void Establish() => File.WriteAllText(Path.Combine(Root, "application.play"), File.ReadAllText(Path.Combine(Repository(), "Samples", "Library", "library.play")));

    static string Repository([System.Runtime.CompilerServices.CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Samples"))) directory = directory.Parent;

        return directory!.FullName;
    }
}
