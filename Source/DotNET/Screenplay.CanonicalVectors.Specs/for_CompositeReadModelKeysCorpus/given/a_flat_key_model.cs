// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_CompositeReadModelKeysCorpus.given;

public class a_flat_key_model : Specification
{
    protected const string Declarations = """
        module M
          feature F
            slice StateView S
              event Recorded
                first String
                second String
              event Removed
              readmodel Row
                first String key
                second String key
                quantity Int
              query Find => Row optional
                by
                  first String
                  second String
              query All => Row[]
        """;

    protected static CompilationResult<SemanticCompilation> Compile(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("FlatKeys"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("application"), "application", "application.play", source);

        return new SemanticModelCompiler().Compile("FlatKeys", SemanticDocumentSet.Create([document], catalog));
    }

    protected static CompilationResult<SemanticCompilation> Bind(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("FlatKeys"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("application"), "application", "application.play", source);
        var syntax = new ScreenplayCompiler().Parse(source).Value!;

        return new SemanticModelBinder().Bind("FlatKeys", syntax, SemanticDocumentSet.Create([document], catalog));
    }

    protected static string Specification(string steps) => Declarations + "\n      specification Check\n" + Indent(steps, 8);

    protected static string Indent(string text, int spaces) => string.Join('\n', text.Split('\n').Select(line => new string(' ', spaces) + line));
}
