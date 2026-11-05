// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_possible_events;

public class when_publishing_alongside_clock_operands : given.a_possible_events_binder
{
    const string Clock = "        dueDate Date\n        validate\n          require dueDate >= today - 1 days\n";

    ExecutableSemanticModel _model;
    byte[] _bytes;

    void Because()
    {
        var binding = BindPossible(Prefix + Properties + Clock + Pending);
        Assert.True(binding.Success, string.Join('\n', binding.Diagnostics.Select(diagnostic => diagnostic.Message)));
        _model = binding.Value.Model;
        _bytes = SemanticModelCanonicalJson.Serialize(_model);
    }

    SemanticCommand Command() => _model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();

    [Fact] void should_select_language_v37() => _model.LanguageVersion.ShouldEqual(LanguageVersion.V37);
    [Fact] void should_select_semantic_v37() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V37);
    [Fact] void should_carry_the_v35_handler_result_contract() => Command().Handler!.ResultVersion.ShouldEqual(2u);
    [Fact] void should_carry_the_possible_events() => Command().Handler!.PossibleEvents.Length.ShouldEqual(2);
    [Fact] void should_write_the_possible_events() => Encoding.UTF8.GetString(_bytes).ShouldContain("\"possibleEvents\"");
    [Fact] void should_write_the_clock_operand() => Encoding.UTF8.GetString(_bytes).ShouldContain("\"clock\"");
    [Fact] void should_strict_read_both_features() => SemanticModelSerializer.Deserialize(_bytes).Revision.ShouldEqual(_model.Revision);
    [Fact] void should_keep_canonical_bytes() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(_bytes)).ShouldEqual(_bytes);
}
