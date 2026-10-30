// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_possible_events;

public class with_result_only_model_types : given.a_possible_events_binder
{
    SemanticTypedContextDescriptor _descriptor = null!;

    void Because()
    {
        var binding = BindPossible("concept Code : String\ntype Details\n  code Code\n" + Source.Replace("value String", "value Details", StringComparison.Ordinal));
        Assert.True(binding.Success, string.Join('\n', binding.Diagnostics.Select(diagnostic => diagnostic.Message)));
        _descriptor = binding.TypedContextDescriptors.Single();
    }

    [Fact] void should_include_payload_types_in_the_closure() => _descriptor.Types.Select(type => type.Name).ShouldEqual(["Details", "Code"]);
    [Fact] void should_keep_the_nested_type_reference() => _descriptor.Types[0].Properties.Single().Type.Target.ShouldEqual(_descriptor.Types[1].Id);
}
