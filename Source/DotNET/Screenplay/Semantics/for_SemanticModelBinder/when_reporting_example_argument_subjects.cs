// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_reporting_example_argument_subjects : given.a_semantic_binder
{
    const string Declarations = "module M\n  feature F\n    slice StateChange S\n      command C\n        amount Int\n        produces E\n          amount = amount\n      event E\n        amount Int\n";

    [Fact]
    void should_name_a_colliding_example()
    {
        var diagnostic = Bind("example E : E\n" + Declarations).Diagnostics.Single(item => item.Code == DiagnosticCodes.SpecificationExampleNameCollision);
        diagnostic.Arguments["example"].ShouldEqual(DiagnosticArgument.Text("E"));
    }

    [Fact]
    void should_name_actual_and_required_kinds()
    {
        var diagnostic = Bind("example Fact : E\n" + Declarations + "      specification WrongKind\n        when Fact\n        then E amount = 1").Diagnostics.Single(item => item.Code == DiagnosticCodes.SpecificationExampleKindMismatch);
        diagnostic.Arguments.ShouldEqual(DiagnosticArguments.Empty.With("example", DiagnosticArgument.Text("Fact")).With("kind", DiagnosticArgument.Keyword("event")).With("expectedKind", DiagnosticArgument.Keyword("command")));
    }

    [Fact]
    void should_name_the_example_type_and_invalid_property()
    {
        var diagnostic = Bind("example Fact : E\n  removed = 1\n" + Declarations).Diagnostics.Single(item => item.Code == DiagnosticCodes.InvalidSpecificationExampleValue);
        diagnostic.Arguments.ShouldEqual(DiagnosticArguments.Empty.With("example", DiagnosticArgument.Text("Fact")).With("type", DiagnosticArgument.Text("E")).With("property", DiagnosticArgument.Text("removed")));
    }
}
