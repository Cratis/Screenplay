// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Xunit;

namespace Cratis.Screenplay.Semantics.Serialization.for_EsmJsonSchema;

public class when_describing_the_vocabulary : Specification
{
    List<string> _missing = [];

    void Because()
    {
        foreach (var shape in Enum.GetValues<EsmBaselineShape>().Where(shape => shape != EsmBaselineShape.Atom))
        {
            var described = EsmSchemaMembers.Shapes.Contains(shape) ? EsmSchemaMembers.For(shape).Select(member => member.Member).ToHashSet(StringComparer.Ordinal) : [];
            foreach (var major in EsmBaselineShapes.Majors)
            {
                _missing.AddRange(EsmBaselineShapes.Fields(major, shape).Keys.Where(member => !described.Contains(member)).Select(member => $"v{major} {shape}.{member}"));
            }
        }
    }

    [Fact] void should_give_every_member_of_every_version_a_value_kind() => Assert.True(_missing.Count == 0, string.Join(", ", _missing.Distinct().Take(20)));
    [Fact] void should_only_describe_vocabulary_shapes() => EsmSchemaMembers.Shapes.ShouldNotContain(EsmBaselineShape.Atom);
}
#endif
