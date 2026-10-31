// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_qualified_capture_sources;

public class with_resolvable_sources : given.a_capture_source
{
    CompilationResult<SemanticCompilation>[] _results;

    void Because()
    {
        var sources = new[]
        {
            Source("Products.Catalog.Facts.ProductPublished"),
            Source("Catalog.Facts.ProductPublished"),
            Source("Facts.ProductPublished"),
            Source("ProductPublished")
        };
        _results = [.. sources.Select(source => Bind(new ScreenplayCompiler().Parse(source).Value))];
    }

    [Fact] void should_bind_every_resolvable_spelling() => _results.All(result => result.Success).ShouldBeTrue();
    [Fact] void should_not_report_diagnostics() => _results.SelectMany(result => result.Diagnostics).ShouldBeEmpty();
    [Fact]
    void should_consume_the_declared_event_identity() => _results.All(result =>
    {
        var slice = result.Value.Model.Application.Modules.Single().Features.Single().Slices.Single();
        return slice.Captures.Single().EventsSource.Events.Single() == slice.Events.Single(@event => @event.Name == "ProductPublished").Id;
    }).ShouldBeTrue();
}
