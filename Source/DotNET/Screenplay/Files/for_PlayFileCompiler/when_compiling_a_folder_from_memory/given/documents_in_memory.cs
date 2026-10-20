// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_compiling_a_folder_from_memory.given;

public class documents_in_memory : Specification
{
    protected const string Command = """
        module Banking
          feature Accounts
            slice StateChange Record
              command Record
                stream Account.Ledger
                  streamId
                    account = "a"
                    period = "2026-10"
        """;
    protected const string EventSource = """
        eventsource Account
          stream Ledger
            streamId
              account String
              period String
        """;
    protected IPlayFileCompiler _compiler;
    protected PlayFileContent[] _documents;
    protected DirectoryInfo _root;

    void Establish()
    {
        _compiler = new PlayFileCompiler();
        _documents = [new("z-sources.play", EventSource), new("commands/record.play", Command)];
        _root = Directory.CreateTempSubdirectory("play-memory");
    }

    protected ApplicationCompilation<ApplicationSyntax> FromDisk(string? entry = null)
    {
        foreach (var document in _documents)
        {
            var path = Path.Combine(_root.FullName, document.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, document.Content);
        }

        return entry is null ? _compiler.CompileFolder(_root.FullName) : _compiler.CompileApplication(Path.Combine(_root.FullName, entry));
    }

    void Destroy() => _root.Delete(true);
}
