// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_CompositeReadModelKeysCorpus;

public class when_describing_affected_instances : given.a_flat_key_model
{
    SemanticAffectedProjectionInstance[] _instances;

    void Because()
    {
        var compiled = Compile(Declarations + "\n" + """
                  projection Rows => Row
                    from Recorded
                      key
                        first = first
                        second = second
                      quantity = 1
            """);
        Assert.True(compiled.Success, string.Join('\n', compiled.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var application = compiled.Value!.Model.Application;
        var projection = application.Modules.Single().Features.Single().Slices.Single().Projections.Single();
        _instances = [.. projection.GetAffectedInstances(application)];
    }

    [Fact] void should_describe_one_keyed_instance() => _instances.Single().Match.ShouldEqual(SemanticAffectedProjectionMatch.OneByKey);
    [Fact] void should_keep_both_named_key_parts() => ((SemanticProjectionPartsKey)_instances.Single().Key!).Parts.Length.ShouldEqual(2);
}
