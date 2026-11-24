// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_a_graph_projection_in_the_default_module : given.a_graph_projection_source
{
    ExecutableSemanticModel _model;

    void Because()
    {
        var source = string.Join('\n', Source.Split('\n').Skip(2).Select(line => line[2..]));
        var parsed = new ScreenplayCompiler().Parse(source, "placed.play", new PlayPlacement(["Default"]));
        Assert.True(parsed.Success, string.Join('\n', parsed.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var catalog = SemanticIdentityCatalog.Empty(_applicationIdentity);
        const string Key = "placed-document";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(Key), Key, "placed.play", source);
        var result = _binder.Bind("Projects", parsed.Value!, SemanticDocumentSet.Create([document], catalog));
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        _model = result.Value!.Model;
    }

    [Fact] void should_name_the_declaring_slice() => _model.Application.GraphProjections.Single().DeclaringSlice.ShouldEqual(_model.Application.Modules.Single().Features.Single().Slices.Single().Id);
    [Fact] void should_keep_the_synthetic_module_name() => _model.Application.Modules.Single().Name.ShouldEqual("Default");
}
