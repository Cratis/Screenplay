// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.given;

public class a_projection_effect_workspace : a_multi_source_property_workspace
{
    protected const string AggregateSource = """
        module Projects
          feature Registration
            slice StateChange Facts
              event A
                name String
            slice StateView Lookup
              readmodel View
                viewId String
                count Int
                name String
              projection View => View
                from A
                  increment count
              query Views => View[]
        """;

    protected const string JoinSource = """
        module Projects
          feature Registration
            slice StateChange Facts
              event A
                name String
            slice StateView Lookup
              readmodel View
                viewId String
                value String
                name String
              projection View => View
                join item on viewId
                  with A
                    value = name
              query Views => View[]
        """;

    protected const string SharedSource = """
        module Projects
          feature Registration
            slice StateChange Facts
              event A
                name String
              event B
                other String
            slice StateView Lookup
              readmodel View
                viewId String
                name String
              projection View => View
                from A, B
              query Views => View[]
        """;

    protected const string VariantSource = """
        module Projects
          feature Registration
            slice StateChange Facts
              event Changed
                name String
              event First
              event Second
            slice StateView Lookup
              readmodel One
                itemId String
                old String optional
                label String optional
              readmodel Two
                itemId String
                old String optional
                label String optional
              projection Items
                from Changed
                  old = name
                variant One
                  enters on First
                variant Two
                  enters on Second
              query Ones => One[]
              query Twos => Two[]
        """;

    protected const string ExampleSource = """
        module Projects
          feature Registration
            slice StateChange Facts
              event Audited
                name String
                other String
              example Expected : Audited
                name = "Ada"
                other = "kept"
              specification Audit
                when append Audited
                  name = "Ada"
                  other = "kept"
                then Expected
        """;
}
