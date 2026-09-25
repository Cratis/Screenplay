// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_serializing_the_v4_golden_model : Specification
{
    byte[] _expected = [];
    byte[] _written = [];
    ExecutableSemanticModel _model = null!;

    void Establish()
    {
        _expected = canonical_serialization_golden_vectors.SemanticModelV4Bytes;
        _model = canonical_serialization_golden_vectors.CreateSemanticModelV4();
    }

    void Because() => _written = SemanticModelSerializer.Serialize(_model);

    [Fact] void should_match_the_checked_in_v4_bytes() => _written.SequenceEqual(_expected).ShouldBeTrue();
    [Fact] void should_preserve_the_v4_revision() => SemanticModelSerializer.Deserialize(_expected).Revision.ShouldEqual(_model.Revision);
    [Fact] void should_change_the_revision_if_historical_tags_change()
    {
        var application = _model.Application;
        var module = application.Modules.Single();
        var root = module.Features.Single();
        var feature = root.Features.Single();
        var slices = feature.Slices.Select(slice => slice with
        {
            Events = [.. slice.Events.Select(@event => @event.PriorRevisions.IsEmpty ? @event : @event with
            {
                PriorRevisions = [@event.PriorRevisions[0] with { Tags = ["changed"] }, .. @event.PriorRevisions.Skip(1)]
            })]
        });
        var changed = ExecutableSemanticModel.Create(
            LanguageVersion.V4,
            SemanticVersion.V4,
            application with { Modules = [module with { Features = [root with { Features = [feature with { Slices = [.. slices] }] }] }] });
        changed.Revision.ShouldNotEqual(_model.Revision);
    }
}
#endif
