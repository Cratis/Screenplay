// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_EsmGoldenVectors;

public class when_requesting_unknown_versions : Specification
{
    Exception[] _errors;

    void Because() => _errors = [.. new[] { -1, 0, EsmGoldenVectors.Versions[^1] + 1 }.Select(version => Catch.Exception(() => EsmGoldenVectors.Get(version)))];

    [Fact] void should_refuse_every_unknown_version() => _errors.All(error => error is EsmGoldenVectorNotFound).ShouldBeTrue();
}
