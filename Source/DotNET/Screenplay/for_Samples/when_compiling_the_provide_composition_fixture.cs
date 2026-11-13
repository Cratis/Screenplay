// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;

namespace Cratis.Screenplay.for_Samples;

/// <summary>
/// Pins the multi-file preview fixture for <c>provide</c> (#1300): a module-level value and a slice that
/// overrides it compile as one application with no errors or warnings, and the files are all reached.
/// </summary>
public class when_compiling_the_provide_composition_fixture : Specification
{
    ApplicationCompilation<Cratis.Screenplay.Syntax.ApplicationSyntax> _compilation = null!;

    void Because()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !Directory.Exists(Path.Combine(root.FullName, "Documentation", "screenplay", "fixtures")))
        {
            root = root.Parent;
        }

        _compilation = new PlayFileCompiler().CompileFolder(Path.Combine(root!.FullName, "Documentation", "screenplay", "fixtures", "provide-composition"));
    }

    [Fact] void should_compile_without_errors_or_warnings() =>
        _compilation.Result.Diagnostics
            .Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
            .ShouldBeEmpty();

    [Fact] void should_reach_every_file() => _compilation.Sources.Count().ShouldEqual(4);

    [Fact] void should_declare_the_module_value_and_the_slice_override() =>
        _compilation.Result.Value!.Modules.Single().Provides.Count().ShouldEqual(1);
}
