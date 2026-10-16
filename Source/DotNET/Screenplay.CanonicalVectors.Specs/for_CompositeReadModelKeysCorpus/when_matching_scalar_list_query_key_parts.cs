// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_CompositeReadModelKeysCorpus;

public class when_matching_scalar_list_query_key_parts : given.a_flat_key_model
{
    ExecutableSemanticModel _model;
    SemanticSpecificationRun _run;
    byte[] _bytes;

    void Because()
    {
        var source = Specification("""
            given readmodel Row
              first = "a"
              second = "b"
              quantity = 1
            given readmodel Row
              first = "a"
              second = "c"
              quantity = 2
            given readmodel Row
              first = "other"
              second = "b"
              quantity = 3
            when query All
              first = "a"
            then result
              first = "a"
              second = "b"
              quantity = 1
            then result
              first = "a"
              second = "c"
              quantity = 2
            """).Replace("query All => Row[]", "query All => Row[]\n        by first String", StringComparison.Ordinal);
        var compiled = Compile(source);
        Assert.True(compiled.Success, string.Join('\n', compiled.Diagnostics.Select(diagnostic => diagnostic.Message)));
        _bytes = SemanticModelSerializer.Serialize(compiled.Value!.Model);
        _model = SemanticModelSerializer.Deserialize(_bytes);
        var specification = _model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single();
        _run = new SemanticSpecificationRunner().Run(SemanticExecutionPlan.Compile(_model).Plan!, specification.Id);
    }

    [Fact] void should_select_the_flat_key_version() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V27);
    [Fact] void should_preserve_the_strict_canonical_round_trip() => SemanticModelSerializer.Serialize(_model).ShouldEqual(_bytes);
    [Fact] void should_match_the_requested_part_without_comparing_it_to_the_whole_key() => Assert.True(_run.Passed, string.Join('\n', _run.Failures));
}
