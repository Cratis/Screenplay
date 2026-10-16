// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_CompositeReadModelKeysCorpus;

public class when_refusing_invalid_projection_parts : Specification
{
    [Theory]
    [InlineData("from Recorded", "PLAY0661")]
    [InlineData("from Recorded key first", "PLAY0661")]
    [InlineData("from Recorded\n  key first", "PLAY0661")]
    [InlineData("from Recorded\n  key\n    first = first", "PLAY0661")]
    [InlineData("from Recorded\n  key\n    first = first\n    second = second\n    unknown = first", "PLAY0662")]
    [InlineData("from Recorded\n  key\n    first = first\n    first = first\n    second = second", "PLAY0662")]
    [InlineData("from Recorded\n  key\n    first = amount\n    second = second", "PLAY0662")]
    [InlineData("from Recorded\n  key\n    first = first + second\n    second = second", "PLAY0662")]
    [InlineData("remove with Removed", "PLAY0661")]
    [InlineData("remove with Removed key first", "PLAY0661")]
    [InlineData("remove with Removed\n  parent first", "PLAY0661")]
    [InlineData("remove with Removed\n  key\n    first = first", "PLAY0661")]
    [InlineData("remove with Removed\n  key\n    first = first\n    second = second\n    unknown = first", "PLAY0662")]
    [InlineData("remove with Removed\n  key\n    first = amount\n    second = second", "PLAY0662")]
    [InlineData("remove with Removed\n  key first", "PLAY0661")]
    [InlineData("remove with Removed\n  key\n    first = first\n    first = first\n    second = second", "PLAY0662")]
    [InlineData("remove with Removed\n  key\n    first = first + second\n    second = second", "PLAY0662")]
    public void should_refuse_the_projection_with_its_key_diagnostic(string block, string expected)
    {
        var source = """
            module M
              feature F
                slice StateView S
                  event Recorded
                    first String
                    second String
                    amount Int
                  event Removed
                    first String
                    second String
                    amount Int
                  readmodel Row
                    first String key
                    second String key
                  projection Rows => Row
            """ + "\n" + string.Join('\n', block.Split('\n').Select(line => $"        {line}"));
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("ProjectionParts"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("application"), "application", "application.play", source);
        var result = new SemanticModelCompiler().Compile("ProjectionParts", SemanticDocumentSet.Create([document], catalog));
        result.Success.ShouldBeFalse();
        result.Value.ShouldBeNull();
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Code).ShouldContainOnly(expected);
    }
}
