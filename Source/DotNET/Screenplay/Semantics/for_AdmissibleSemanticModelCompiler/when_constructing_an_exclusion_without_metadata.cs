// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_constructing_an_exclusion_without_metadata : Specification
{
    SemanticExclusion _exclusion;

    void Because() => _exclusion = new(SemanticExclusionKind.Slice, "M/F/S", null, SourceLocation.Start, []);

    [Fact] void should_default_to_root() => _exclusion.Cause.ShouldEqual(SemanticExclusionCause.Root);
    [Fact] void should_default_to_empty_dependencies() => _exclusion.DependsOn.ShouldBeEmpty();
}
