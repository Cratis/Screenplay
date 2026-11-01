// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

// Chronicle FindParentEvent checks the child's earlier creation event before treating a shared event as the parent
// (KeyResolvers.cs:390-418). TryResolveViaCreationEventSource finds another child-owned event whose source is the child key
// (KeyResolvers.cs:527-635), then resolves its parent hierarchy to update the child where it was created (:556,567-583).
// Screenplay ParentIdentity instead always uses the current parent from key (SemanticScopedProjection.Deferred.cs:74-81).
public class with_a_shared_event_updating_an_existing_grandchild : given.a_self_contained_projection_plan
{
    const string Order = "00000000-0000-0000-0000-000000000001";
    const string LineA = "00000000-0000-0000-0000-0000000000a1";
    const string LineB = "00000000-0000-0000-0000-0000000000b1";
    const string Part = "00000000-0000-0000-0000-0000000000f1";
    const string Source =
        """
        concept Id : Uuid
        type Part
          partId Id
          label String?
        type Line
          lineId Id
          label String?
          parts Part[]
        module Orders
          feature Ordering
            slice StateView Lookup
              event OrderAdded
                orderId Id
              event LineAdded
                orderId Id
                lineId Id
              event PartAdded
                lineId Id
                partId Id
                label String
              event LineRenamed
                orderId Id
                lineId Id
                partId Id
                label String
              readmodel OrderView
                orderId Id
                lines Line[]
              query OrderById => OrderView?
                by orderId Id
              projection Orders => OrderView
                from OrderAdded key orderId
                children lines identified by lineId
                  from LineAdded key lineId
                    parent orderId
                  from LineRenamed key lineId
                    parent orderId
                    label = label
                  children parts identified by partId
                    from PartAdded key partId
                      parent lineId
                      label = label
                    from LineRenamed key partId
                      parent lineId
                      label = label
        """;

    void Establish() => Plan(Source);

    void Because() => Project(
        Fact("OrderAdded", Order, ("orderId", Text(Order))),
        Fact("LineAdded", LineA, ("orderId", Text(Order)), ("lineId", Text(LineA))),
        Fact("LineAdded", LineB, ("orderId", Text(Order)), ("lineId", Text(LineB))),
        Fact("PartAdded", Part, ("lineId", Text(LineA)), ("partId", Text(Part)), ("label", Text("old"))),
        Fact("LineRenamed", Part, ("orderId", Text(Order)), ("lineId", Text(LineB)), ("partId", Text(Part)), ("label", Text("new"))));

    [Fact] void should_project() => _failure.ShouldBeNull();
    [Fact] void should_rename_the_line() => ((SemanticTextValue)Member(Line(LineB), "Line", "label")).Value.ShouldEqual("new");
    [Fact] void should_update_the_part_where_it_was_created() => ((SemanticTextValue)Member(Parts(LineA).Single(part => SemanticValueRules.AreEqual(Member(part, "Part", "partId"), Text(Part))), "Part", "label")).Value.ShouldEqual("new");
    [Fact] void should_not_add_the_part_under_the_renamed_line() => Parts(LineB).Length.ShouldEqual(0);

    SemanticValue Line(string id) => ((SemanticArrayValue)Value("OrderView", Order, "lines")).Values.Single(line => SemanticValueRules.AreEqual(Member(line, "Line", "lineId"), Text(id)));

    SemanticValue[] Parts(string line) => [.. ((SemanticArrayValue)Member(Line(line), "Line", "parts")).Values];
}
