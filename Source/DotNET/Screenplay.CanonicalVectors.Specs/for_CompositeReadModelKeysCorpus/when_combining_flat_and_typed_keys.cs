// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_CompositeReadModelKeysCorpus;

public class when_combining_flat_and_typed_keys : given.a_flat_key_model
{
    SemanticCompilation _compilation;
    SemanticSpecificationRun _run;

    void Because()
    {
        var source = "type Identity\n  first String\n  second String\n" + Declarations + "\n" + """
                  readmodel TypedRow
                    identity Identity key
                    quantity Int
                  query Typed => TypedRow optional
                    by identity Identity
                  specification TypedLookup
                    given readmodel TypedRow
                      identity = {"first":"a","second":"b"}
                      quantity = 3
                    when query Typed
                      identity = {"second":"b","first":"a"}
                    then result
                      quantity = 3
            """;
        var compiled = Compile(source);
        Assert.True(compiled.Success, string.Join('\n', compiled.Diagnostics.Select(diagnostic => diagnostic.Message)));
        _compilation = compiled.Value!;
        var model = _compilation.Model;
        var specification = model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single();
        _run = new SemanticSpecificationRunner().Run(SemanticExecutionPlan.Compile(model).Plan!, specification.Id);
    }

    [Fact] void should_select_the_flat_key_version() => _compilation.Model.SemanticVersion.ShouldEqual(SemanticVersion.V27);
    [Fact] void should_keep_both_key_shapes() => _compilation.Model.Application.Modules.Single().Features.Single().Slices.Single().ReadModels.Select(model => model.Properties.Count(property => property.IsIdentifier)).ShouldContainOnly(1, 2);
    [Fact] void should_execute_the_typed_single_key_lookup() => Assert.True(_run.Passed, string.Join('\n', _run.Failures));
    [Fact] void should_preserve_the_strict_canonical_round_trip() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(_compilation.Model))).ShouldEqual(SemanticModelSerializer.Serialize(_compilation.Model));
}
