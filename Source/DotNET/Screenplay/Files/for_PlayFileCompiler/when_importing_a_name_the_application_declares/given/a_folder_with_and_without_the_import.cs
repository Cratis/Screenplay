// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_importing_a_name_the_application_declares.given;

public class a_folder_with_and_without_the_import : Specification
{
    protected const string Concepts =
        """
        concept OrderId : Uuid
        concept ItemId : String
        concept Channel : Enum
          web
          store
        """;

    protected DirectoryInfo _withImport;
    protected DirectoryInfo _withoutImport;
    protected ApplicationCompilation<ApplicationSyntax> _importing;
    protected ApplicationCompilation<ApplicationSyntax> _plain;

    protected IEnumerable<Diagnostic> ValidationWithImport =>
        _importing.Result.Diagnostics.Where(diagnostic => diagnostic.Code != DiagnosticCodes.RedundantImport);

    protected IEnumerable<Diagnostic> ValidationWithoutImport => _plain.Result.Diagnostics;

    void Establish()
    {
        _withImport = Directory.CreateTempSubdirectory("playimport");
        _withoutImport = Directory.CreateTempSubdirectory("playimport");
    }

    protected void CompileBoth()
    {
        var compiler = new PlayFileCompiler();
        _importing = compiler.CompileFolder(_withImport.FullName);
        _plain = compiler.CompileFolder(_withoutImport.FullName);
    }

    protected void Application(string import)
    {
        File.WriteAllText(Path.Combine(_withImport.FullName, "application.play"), $"domain Repro\nimport {import}\n\n{Concepts}\n");
        File.WriteAllText(Path.Combine(_withoutImport.FullName, "application.play"), $"domain Repro\n\n{Concepts}\n");
    }

    protected void Write(string relativePath, string content)
    {
        foreach (var root in new[] { _withImport, _withoutImport })
        {
            var path = Path.Combine(root.FullName, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
        }
    }

    void Destroy()
    {
        _withImport.Delete(true);
        _withoutImport.Delete(true);
    }
}
