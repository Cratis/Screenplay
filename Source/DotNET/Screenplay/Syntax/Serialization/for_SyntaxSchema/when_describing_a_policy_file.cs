// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxSchema;

public class when_describing_a_policy_file : Specification
{
    [Fact] void should_discover_the_additive_file_member() => SyntaxSchema.For("PolicySyntax").GetProperty("properties").TryGetProperty("file", out _).ShouldBeTrue();
}
