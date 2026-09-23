// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_reading_malformed_constraints : a_valid_semantic_model
{
    string _json;
    Exception _emptyArray;
    Exception _unknownKind;
    Exception _unknownScope;
    Exception _missingMember;
    Exception _unknownMember;

    void Establish()
    {
        var slice = _application.Modules.Single().Features.Single().Slices.Single(_ => _.Commands.Length > 0);
        var constrained = ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            ReplaceSlice(slice with
            {
                Constraints = [new("OneRegistrationPerProject", SemanticConstraintKind.UniqueEventOccurrence, SemanticConstraintScope.EventSequence, [new(_eventId, [])], [], false, null)]
            }));
        _json = Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(constrained));
    }

    void Because()
    {
        // The revision has a fixed length, so the member sits at the same offset in the model without constraints.
        var unconstrained = Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(_model));
        _emptyArray = Read(unconstrained.Insert(_json.IndexOf(",\"constraints\":[", StringComparison.Ordinal), ",\"constraints\":[]"));
        _unknownKind = Read(_json.Replace("\"uniqueEventOccurrence\"", "\"uniqueEvent\"", StringComparison.Ordinal));
        _unknownScope = Read(_json.Replace("\"eventSequence\"", "\"eventSource\"", StringComparison.Ordinal));
        _missingMember = Read(_json.Replace(",\"ignoreCasing\":false", string.Empty, StringComparison.Ordinal));
        _unknownMember = Read(_json.Replace("\"ignoreCasing\":false", "\"ignoreCasing\":false,\"removedWith\":[]", StringComparison.Ordinal));
    }

    [Fact] void should_reject_an_empty_constraints_array_as_noncanonical() => _emptyArray.Message.ShouldContain("not canonical");
    [Fact] void should_reject_an_unknown_kind() => _unknownKind.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_an_unknown_scope() => _unknownScope.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_missing_member() => _missingMember.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_an_unknown_member() => _unknownMember.ShouldBeOfExactType<InvalidSemanticContract>();

    static Exception Read(string json) => Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json)));
}
