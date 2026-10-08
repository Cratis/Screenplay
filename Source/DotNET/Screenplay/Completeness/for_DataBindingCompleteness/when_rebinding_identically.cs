// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_DataBindingCompleteness;

public class when_rebinding_identically : given.a_screen
{
    void Establish() => Compile("data R[] via query Q\nsection Detail\n  data R[] via query M.F.View.Q");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.DataBindings]));

    [Fact] void should_accept_the_resolved_rebinding() => Findings.ShouldBeEmpty();
}
