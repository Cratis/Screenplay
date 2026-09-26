// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_SemanticTypedContextSerializer;

public class when_loading_the_packaged_context_vectors : Specification
{
    [Fact] void should_package_the_bound_context_vector() => AssertPackaged("typed-contexts-v1.json");
    [Fact] void should_package_the_unbound_handler_vector() => AssertPackaged("unbound-handler-context-v1.json");

    static void AssertPackaged(string name)
    {
        using var stream = typeof(when_loading_the_packaged_context_vectors).Assembly.GetManifestResourceStream($"Cratis.Screenplay.CanonicalVectors.Golden.{name}");
        (stream?.Length > 0).ShouldBeTrue();
    }
}
