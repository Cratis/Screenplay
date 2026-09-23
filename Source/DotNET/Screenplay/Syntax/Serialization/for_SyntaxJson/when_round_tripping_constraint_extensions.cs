// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_round_tripping_constraint_extensions : Specification
{
    const string Source =
        """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              event ProjectRegistered
                code String
                year Int
              event ProjectImported
                code String
                year Int
              event ProjectReleased
              constraint UniqueProject
                unique code, year on ProjectRegistered
                unique code, year on ProjectImported
                released by ProjectReleased
                ignore casing
                message "Duplicate project"
        """;

    ApplicationSyntax _original;
    ApplicationSyntax _restored;

    void Establish() => _original = new ScreenplayCompiler().Parse(Source).Value!;

    void Because() => _restored = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(_original));

    [Fact] void should_preserve_every_extension() => SyntaxJson.StructurallyEqual(_original, _restored).ShouldBeTrue();
    [Fact] void should_preserve_additional_targets() => _restored.Modules.Single().Features.Single().Slices.Single().Constraints.Single().AdditionalRules.Count().ShouldEqual(1);
    [Fact] void should_preserve_releases() => _restored.Modules.Single().Features.Single().Slices.Single().Constraints.Single().ReleasedBy.ShouldContain("ProjectReleased");
}
