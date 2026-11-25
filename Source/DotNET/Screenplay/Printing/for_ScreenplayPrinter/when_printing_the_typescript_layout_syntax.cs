// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Printing.for_ScreenplayPrinter;

// Cross-compiler printing: the TypeScript compiler writes the syntax of layouts, templates and contributions
// as exact SyntaxJson; this prints that syntax canonically and holds the print to a committed file the
// TypeScript compiler reads back. Set SCREENPLAY_REGENERATE_PRINTED_LAYOUTS=1 to rewrite the file, then
// review the diff.
public class when_printing_the_typescript_layout_syntax : Specification
{
    const string Variable = "SCREENPLAY_REGENERATE_PRINTED_LAYOUTS";

    ApplicationSyntax _golden;
    string _printed;
    CompilationResult<ApplicationSyntax> _reparsed;

    void Because()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Conformance(), "layouts-templates-contributions.syntax.json")));
        _golden = (ApplicationSyntax)SyntaxJson.Deserialize(document.RootElement);
        _printed = new ScreenplayPrinter().Print(_golden);
        if (Environment.GetEnvironmentVariable(Variable) == "1")
        {
            File.WriteAllText(PrintedPath(), _printed);
        }

        _reparsed = new ScreenplayCompiler().Parse(_printed);
    }

    [Fact] void should_not_be_regenerating() => Environment.GetEnvironmentVariable(Variable).ShouldNotEqual("1");
    [Fact] void should_print_the_layout() => _printed.ShouldContain("layout");
    [Fact] void should_print_the_contributions() => _printed.ShouldContain("contribute to");
    [Fact] void should_print_what_the_typescript_compiler_reads_back() => _printed.ShouldEqual(File.ReadAllText(PrintedPath()));
    [Fact] void should_reparse_the_print_without_errors() => _reparsed.Success.ShouldBeTrue();
    [Fact] void should_reparse_to_the_same_syntax() => SyntaxJson.StructurallyEqual(_golden, _reparsed.Value!).ShouldBeTrue();

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation")))
        {
            directory = directory.Parent;
        }

        return directory!.FullName;
    }

    static string Conformance() => Path.Combine(Root(), "Source", "Screenplay", "Compiler", "Conformance");

    static string PrintedPath() => Path.Combine(Conformance(), "layouts-templates-contributions.printed.txt");
}
