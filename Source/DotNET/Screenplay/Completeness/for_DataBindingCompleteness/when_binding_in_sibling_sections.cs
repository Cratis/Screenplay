// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_DataBindingCompleteness;

public class when_binding_in_sibling_sections : given.a_screen
{
    void Establish() => Compile("section First\n  data R[] via query Q\nsection Second\n  data R via query One");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.DataBindings]));

    [Fact] void should_keep_sibling_bindings_independent() => Findings.ShouldBeEmpty();
}
