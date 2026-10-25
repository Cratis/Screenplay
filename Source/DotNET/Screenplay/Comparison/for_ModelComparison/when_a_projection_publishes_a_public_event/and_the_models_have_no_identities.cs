// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_a_projection_publishes_a_public_event;

public class and_the_models_have_no_identities : given.two_models
{
    const string Outbound = """
        module Integration
          feature Translation
            slice StateChange PackOrder
              command PackOrder
                orderId Uuid identifier
                produces OrderPacked
                  for orderId
              event OrderPacked
                orderId Uuid
            slice Translate PublishOrderShipped
              direction outbound
              public event OrderShipped
                status String
              projection OrderShippedPublisher => OrderShipped
                from OrderPacked
                  status = "packed"
        """;

    void Establish()
    {
        _before = Create(Outbound, application: "Contracts");
        _after = Create(Outbound, application: "Contracts");
    }

    void Because() => _result = ModelComparison.Compare(ComparedModel.WithoutIdentities(_before), ComparedModel.WithoutIdentities(_after));

    [Fact] void should_not_report_the_event_the_projection_targets_as_an_uncomparable_view() =>
        _result.Sections.SelectMany(section => section.Gaps).Any(gap => gap.Kind == ComparisonGapKind.NotComparableIndexed).ShouldBeFalse();
}
