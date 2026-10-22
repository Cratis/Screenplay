// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_typed_capture_keys : given.a_semantic_binder
{
    internal const string Source = """
        concept OrderId : Uuid
        module Sales
          feature Orders
            slice Translate ReceiveOrders
              direction inbound
              event OrderPlaced from "storefront"
                orderId OrderId
              event OrderReceived
                orderId OrderId
              capture OrderIntake
                source events
                  from OrderPlaced
                key orderId
                append OrderReceived
                  orderId = $.orderId
              specification AReceivedOrderHasItsTypedIdentity
                given OrderReceived
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                then readmodel OrderStateEntry
                  orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
            slice StateView OrderState
              readmodel OrderStateEntry
                orderId OrderId key
              projection OrderStateProjection => OrderStateEntry
                from OrderReceived
                  key $eventSourceId
                  orderId = $eventSourceId
        """;

    [Fact]
    void should_type_the_capture_and_given_and_execute_against_a_typed_read_model()
    {
        var result = Bind(Source);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(value => value.Message)));
        var model = result.Value!.Model;
        model.SemanticVersion.Major.ShouldEqual(32u);
        var slice = model.Application.Modules.Single().Features.Single().Slices[0];
        var type = SemanticTypeReference.ForConcept(model.Application.Concepts.Single().Id);
        slice.Captures.Single().Appends.Single().EventSourceType.ShouldEqual(type);
        slice.Specifications.Single().GivenEvents.Single().EventSource!.Type.ShouldEqual(type);
        var bytes = SemanticModelSerializer.Serialize(model);
        SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(bytes)).ShouldEqual(bytes);
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        var run = new SemanticSpecificationRunner().Run(plan, slice.Specifications.Single().Id);
        Assert.True(run.Passed, string.Join('\n', run.Failures));
    }

    [Fact]
    void should_reject_typed_capture_keys_under_an_earlier_schema()
    {
        var result = Bind(Source);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(value => value.Message)));
        var model = result.Value!.Model;
        Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V31, SemanticVersion.V31, model.Application))
            .ShouldBeOfExactType<InvalidSemanticContract>();
        EsmSchemaV31Support.Supports(LanguageVersion.V32, SemanticVersion.V32).ShouldBeFalse();
    }

    [Theory]
    [InlineData("source webhook", false)]
    [InlineData("source poll", false)]
    [InlineData("source events\n          from OrderPlaced", true)]
    void should_leave_untyped_sources_and_conflicting_commands_at_the_legacy_type(string source, bool conflict)
    {
        var text = Source[..Source.IndexOf("      specification", StringComparison.Ordinal)]
            .Replace("source events\n          from OrderPlaced", source, StringComparison.Ordinal);
        if (conflict) text += "\n      command ReceiveOrder\n        id String identifier\n        produces OrderReceived\n          orderId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n";
        var result = Bind(text);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(value => value.Message)));
        var model = result.Value!.Model;
        model.SemanticVersion.Major.ShouldBeLessThan(32u);
        model.Application.Modules.Single().Features.Single().Slices.Single().Captures.Single().Appends.Single()
            .EventSourceType.ShouldEqual(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text));
    }

    [Fact]
    void should_leave_disagreeing_capture_keys_at_text()
    {
        var source = Source[..Source.IndexOf("      specification", StringComparison.Ordinal)] + """

              event OtherOrderPlaced from "storefront"
                orderId String
              capture OtherIntake
                source events
                  from OtherOrderPlaced
                key orderId
                append OrderReceived
                  orderId = $.orderId
        """;
        var result = Bind(source);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(value => value.Message)));
        var model = result.Value!.Model;
        model.SemanticVersion.Major.ShouldBeLessThan(32u);
        model.Application.Modules.Single().Features.Single().Slices.Single().Captures.SelectMany(capture => capture.Appends)
            .All(append => append.EventSourceType == SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text)).ShouldBeTrue();
    }
    [Theory]
    [InlineData("OrderId", 32u)]
    [InlineData("String", 9u)]
    void should_require_all_consumed_events_to_agree_on_the_key(string type, uint version)
    {
        var source = Source[..Source.IndexOf("      specification", StringComparison.Ordinal)]
            .Replace("from OrderPlaced", "from OrderPlaced\n          from OtherOrderPlaced", StringComparison.Ordinal)
            + $"\n      event OtherOrderPlaced from \"storefront\"\n        orderId {type}\n";
        var result = Bind(source);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(value => value.Message)));
        result.Value!.Model.SemanticVersion.Major.ShouldEqual(version);
    }

    [Fact]
    void should_execute_the_capture_with_its_typed_identity()
    {
        var source = Source.Replace("given OrderReceived", "when append OrderPlaced", StringComparison.Ordinal);
        var result = Bind(source);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(value => value.Message)));
        var model = result.Value!.Model;
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        var specification = model.Application.Modules.Single().Features.Single().Slices[0].Specifications.Single();
        var run = new SemanticSpecificationRunner().Run(plan, specification.Id);
        Assert.True(run.Passed, string.Join('\n', run.Failures));
    }
    [Fact]
    void should_leave_conflicting_reaction_producers_at_text()
    {
        var source = Source[..Source.IndexOf("      specification", StringComparison.Ordinal)] + """

              event OtherOrderReceived
                orderId OrderId
            slice Automation OtherProducer
              reaction ProduceOrder
                when OtherOrderReceived
                  produces OrderReceived
                    for "legacy"
                    orderId = orderId
        """;
        var result = Bind(source);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(value => value.Message)));
        var model = result.Value!.Model;
        model.SemanticVersion.Major.ShouldBeLessThan(32u);
        model.Application.Modules.Single().Features.Single().Slices.Single(slice => !slice.Captures.IsEmpty)
            .Captures.Single().Appends.Single().EventSourceType.ShouldEqual(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text));
    }
}
