// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_a_capture : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(
        """
        module Billing
          feature Legacy
            slice Translate LegacySync
              capture LegacyInvoiceCapture
                source api
                  api LegacyInvoicingApi
                key id
                map
                  status = status translate
                    "betalt" => paid
                  split contactName by ","
                    lastName
                    firstName
                  summary = `${status} invoice`
                append LegacyStatusChanged
                  tag legacy
                  when status
                    status    = $.status
                    changedAt = $context.occurred
                append LegacyPaid
                  when status from "sent" to "paid"
                    source = "legacy"
                append LegacyPaid
                  when status and contactName
                    source = "both"
                append LegacyPaid
                  when `status == "paid" && overdue == true`
                    source = "flagged"
                children lineItems identified by lineNumber
                  append LegacyLineRemoved
                    when removed
                      lineNumber = $.lineNumber
                nested billingContact
                  append LegacyPaid
                    when email
                      source = $.email
              event LegacyStatusChanged
                status String
                changedAt DateTime
              event LegacyPaid
                source String
              event LegacyLineRemoved
                lineNumber Int
        """);

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_select_esm_v6() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V6);
    [Fact] void should_keep_the_key() => Capture.Key.ShouldEqual("id");
    [Fact] void should_keep_the_map_operations_in_order() => Capture.Map.Select(_ => _.Kind).ShouldContainOnly(SemanticCaptureMapKind.Value, SemanticCaptureMapKind.Split, SemanticCaptureMapKind.Template);
    [Fact] void should_keep_the_translations() => Capture.Map[0].Translations.Single().ShouldEqual(new SemanticCaptureTranslation("betalt", "paid"));
    [Fact] void should_split_into_its_targets() => Capture.Map[1].Targets.ShouldContainOnly("lastName", "firstName");
    [Fact] void should_bind_the_template_parts() => Capture.Map[2].Template.Select(_ => _.Field ?? _.Text).ShouldContainOnly("status", " invoice");
    [Fact] void should_bind_every_condition() => Capture.Appends.Select(_ => _.When!.Kind).ShouldContainOnly(SemanticCaptureConditionKind.AnyChanged, SemanticCaptureConditionKind.Transition, SemanticCaptureConditionKind.AllChanged, SemanticCaptureConditionKind.Expression);
    [Fact] void should_set_a_property_from_a_field() => Capture.Appends[0].Mappings[0].Field.ShouldEqual("status");
    [Fact] void should_set_a_property_from_the_occurrence() => Capture.Appends[0].Mappings[1].Value.ShouldBeOfExactType<SemanticEventContextExpression>();
    [Fact] void should_keep_the_tags() => Capture.Appends[0].Tags.ShouldContainOnly("legacy");
    [Fact] void should_append_to_a_text_event_source_when_no_command_types_it() => Capture.Appends[0].EventSourceType.ShouldEqual(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text));
    [Fact] void should_bind_the_children() => Capture.Children.Single().Appends.Single().When!.Kind.ShouldEqual(SemanticCaptureConditionKind.Removed);
    [Fact] void should_bind_the_nested_record() => Capture.Nested.Single().Field.ShouldEqual("billingContact");

    SemanticCapture Capture => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Captures.Single();
}
