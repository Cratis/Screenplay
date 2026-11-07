// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

/// <summary>
/// A picker - 'choose from query &lt;Query&gt; show &lt;property&gt;' - shows a property of the rows its query returns.
/// Renaming that property would leave the picker stale, so the rename is refused.
/// </summary>
public class with_a_picker_showing_it : given.a_property_chain_workspace
{
    const string PickerSource = """
        module Shop
          form RegisterCustomerForm for RegisterCustomer
            field segment
              choose from query AllSegments show title

          feature Customers
            slice StateChange RegisterCustomer
              command RegisterCustomer
                customerId Uuid identifier
                segment String
                produces CustomerRegistered
                  segment = segment
              event CustomerRegistered
                segment String
              screen RegisterCustomer
                title "Register a customer"

            slice StateView Segments
              readmodel Segment
                title String
              query AllSegments => Segment[]
        """;

    void Establish() => Workspace = Create(PickerSource);

    void Because() => Result = Workspace.ProposeProperty(Request("Segment", name: "title"));

    [Fact] void should_refuse_the_rename() => Result.Accepted.ShouldBeFalse();
    [Fact] void should_say_the_span_is_unsupported() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.UnsupportedPropertySpan);
}
