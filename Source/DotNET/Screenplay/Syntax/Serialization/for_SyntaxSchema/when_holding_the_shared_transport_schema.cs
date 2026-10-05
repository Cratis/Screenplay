// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxSchema;

public class when_holding_the_shared_transport_schema
{
    [Fact]
    void should_hold_every_kind_and_member_to_the_compiler()
    {
        var definitions = SyntaxSchema.For(nameof(ApplicationSyntax)).GetProperty("$defs").GetRawText();
        var rendered = "// Copyright (c) Cratis. All rights reserved.\n" +
            "// Licensed under the MIT license. See LICENSE file in the project root for full license information.\n\n" +
            "// Generated from C# SyntaxSchema; the native specification holds every kind and member to it.\n" +
            $"export const syntaxDefinitions = {definitions} as const;\n";
        var path = Path.Combine(Root(), "Source", "Screenplay", "Compiler", "Syntax", "SyntaxDefinitions.ts");
        var regenerating = Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_TRANSPORT_SCHEMA") == "1";
        if (regenerating) File.WriteAllText(path, rendered);
        Assert.False(regenerating, "Review the generated schema and rerun without SCREENPLAY_REGENERATE_TRANSPORT_SCHEMA.");
        File.ReadAllText(path).ShouldEqual(rendered);
    }

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;

        return directory!.FullName;
    }
}
