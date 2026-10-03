// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_matching_handler_hint_whitespace
{
    const string Prefix = "module M\n  feature F\n    slice StateChange S\n      command C\n        handler\n          implementation\n            hint ";

    [Theory]
    [MemberData(nameof(Vectors))]
    public void should_share_parser_and_typed_hint_blankness(string name, string authored, string decoded, bool blank)
    {
        name.ShouldNotBeEmpty();
        var parsed = new ScreenplayCompiler().Parse(Prefix + '"' + authored + '"');
        string.Join(',', parsed.Diagnostics.Select(diagnostic => diagnostic.Code)).ShouldEqual(blank ? "PLAY0493" : string.Empty);
        var serialization = Catch.Exception(() => SyntaxJson.Serialize(new ImplementationHintSyntax(decoded, SourceLocation.Start)));
        if (blank)
        {
            serialization.ShouldBeOfExactType<InvalidSyntaxJson>();
        }
        else
        {
            serialization.ShouldBeNull();
            parsed.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Handler!.Implementation!.Hints.Single().Text.ShouldEqual(decoded);
        }
    }

    public static TheoryData<string, string, string, bool> Vectors()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root(), "Source", "Screenplay", "Compiler", "Conformance", "handler-hint-whitespace.json")));
        var vectors = new TheoryData<string, string, string, bool>();
        foreach (var vector in document.RootElement.EnumerateArray())
        {
            vectors.Add(vector.GetProperty("name").GetString()!, vector.GetProperty("authored").GetString()!, vector.GetProperty("decoded").GetString()!, vector.GetProperty("blank").GetBoolean());
        }

        return vectors;
    }

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation")))
        {
            directory = directory.Parent;
        }

        return directory!.FullName;
    }
}
