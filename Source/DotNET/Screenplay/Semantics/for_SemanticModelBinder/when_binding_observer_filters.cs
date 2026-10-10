// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_observer_filters : given.a_semantic_binder
{
    const string Source = "eventsource Account\n  identifier String\n  stream Notes\nmodule M\n  feature F\n    slice Automation S\n      event Recorded\n      reaction Follow\n        from Account.Notes\n        when Recorded\n";

    [Fact]
    void should_bind_source_and_stream_to_catalog_identities()
    {
        var result = Bind(Source);
        Assert.True(result.Success, string.Join(';', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var application = result.Value!.Model.Application;
        result.Value.Model.SemanticVersion.ShouldEqual(SemanticVersion.V10);
        application.Modules[0].Features[0].Slices[0].Reactions[0].From.ShouldEqual(new SemanticObserverFilter(application.EventSources[0].Id, application.EventSources[0].Streams[0].Id));
    }

    [Fact]
    void should_bind_a_source_only_filter()
    {
        var result = Bind(Source.Replace("from Account.Notes", "from Account", StringComparison.Ordinal));
        result.Success.ShouldBeTrue();
        result.Value!.Model.Application.Modules[0].Features[0].Slices[0].Reactions[0].From!.Stream.ShouldBeNull();
    }
}
