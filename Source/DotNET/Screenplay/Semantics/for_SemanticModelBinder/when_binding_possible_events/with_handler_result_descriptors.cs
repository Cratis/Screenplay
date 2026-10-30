// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_possible_events;

public class with_handler_result_descriptors : given.a_possible_events_binder
{
    CompilationResult<PossibleEventsBinding> _binding;
    SemanticTypedContextDescriptor _descriptor = null!;

    void Because()
    {
        _binding = BindPossible(Source);
        Assert.True(_binding.Success, string.Join('\n', _binding.Diagnostics.Select(diagnostic => diagnostic.Message)));
        _descriptor = _binding.TypedContextDescriptors.Single();
    }

    [Fact] void should_use_result_version_two() => _descriptor.Result.ResultVersion.ShouldEqual(2u);
    [Fact] void should_name_the_events_in_order() => _descriptor.Result.Entries.Select(entry => entry.EventName).ShouldEqual(["Created", "Removed"]);
    [Fact] void should_preserve_destinations() => _descriptor.Result.Entries.Select(entry => entry.DestinationKind).ShouldEqual(["scalar", "one-of"]);
    [Fact] void should_name_the_collection() => _descriptor.Result.Entries[^1].DestinationProperty.ShouldEqual("ids");
    [Fact] void should_use_collection_element_types() => _descriptor.Result.Entries[^1].DestinationType.ModelType.IsCollection.ShouldBeFalse();
    [Fact] void should_include_payload_fields() => _descriptor.Result.Entries[0].Payload.Properties.Single().Name.ShouldEqual("value");
    [Fact] void should_allow_an_empty_event_payload() => _descriptor.Result.Entries[1].Payload.Properties.ShouldBeEmpty();
    [Fact] void should_keep_the_context_at_v1() => _descriptor.ContextVersion.ShouldEqual(1u);
    [Fact] void should_advertise_pending_results() => _binding.ImplementationRequirements.Single().AttachmentResolution.ShouldEqual(SemanticAttachmentResolution.Pending);
    [Fact] void should_keep_v1_serialization_available() => SemanticTypedContextSerializer.Serialize([_descriptor with { Result = null }]).ShouldNotBeEmpty();
    [Fact] void should_refuse_portable_v2_serialization() => Assert.Throws<InvalidOperationException>(() => SemanticTypedContextSerializer.Serialize([_descriptor])).Message.ShouldEqual("Handler result descriptors are not yet portable.");

    [Fact]
    void should_leave_a_v1_handler_without_a_result_descriptor()
    {
        const string source = Prefix + Properties + "        produces Created\n        handler\n          ```csharp\n          return payloads;\n          ```\n";
        var binding = Bind(source);
        binding.TypedContextDescriptors.Single().Result.ShouldBeNull();
    }
}
