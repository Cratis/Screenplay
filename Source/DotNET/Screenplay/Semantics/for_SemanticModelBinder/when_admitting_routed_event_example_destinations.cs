// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_admitting_routed_event_example_destinations : given.a_semantic_binder
{
    const string Source = """
        eventsource Account
          identifier String
          stream Ledger
        example Prior : E
          for "account"
          stream Account.Ledger
        module M
          feature F
            slice StateChange Record
              event E
        """;

    [Fact]
    void should_type_an_unused_routed_example_without_a_producer_by_its_source()
    {
        var result = Bind(Source);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        result.Value!.Model.SemanticVersion.ShouldEqual(EventRoutesVersion.Semantic);
    }

    [Fact]
    void should_type_an_unused_routed_example_by_its_source_instead_of_a_differently_typed_producer()
    {
        var result = Bind(Source + "\n      command Record\n        id Uuid identifier\n        produces E\n          for id\n");
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    }

    [Fact]
    void should_use_the_unambiguous_producer_fallback_when_the_routed_source_has_no_identifier()
    {
        var result = Bind(Source.Replace("  identifier String\n", string.Empty, StringComparison.Ordinal) +
            "\n      command Record\n        id String identifier\n        produces E\n          for id\n");
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    }
}
