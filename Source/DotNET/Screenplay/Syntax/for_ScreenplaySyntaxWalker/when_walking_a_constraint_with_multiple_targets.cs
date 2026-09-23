// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.for_ScreenplaySyntaxWalker;

public class when_walking_a_constraint_with_multiple_targets : Specification
{
    const string Source =
        """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              event ProjectRegistered
                code String
              event ProjectImported
                code String
              constraint UniqueProject
                unique code on ProjectRegistered
                unique code on ProjectImported
        """;

    walker _walker;

    void Establish() => _walker = new();

    void Because() => _walker.VisitConstraint(new ScreenplayCompiler().Parse(Source).Value!.Modules.Single().Features.Single().Slices.Single().Constraints.Single());

    [Fact] void should_visit_both_unique_rules() => _walker.Events.ShouldContainOnly(["ProjectRegistered", "ProjectImported"]);

    class walker : ScreenplaySyntaxWalker
    {
        public List<string> Events { get; } = [];

        public override void VisitUniquePropertyConstraint(UniquePropertyConstraintSyntax syntax)
        {
            Events.Add(syntax.Event);
            base.VisitUniquePropertyConstraint(syntax);
        }
    }
}
