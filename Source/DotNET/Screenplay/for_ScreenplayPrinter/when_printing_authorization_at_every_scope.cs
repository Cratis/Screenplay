// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_authorization_at_every_scope : given.a_printer
{
    const string Source =
        """
        policy Portal
          require authenticated

        policy Staff
          require authenticated

        policy Finance
          require authenticated

        policy Manager
          require authenticated

        module BackOffice
          // module gate
          authorize Portal
          feature Orders
            authorize Staff or Finance
            feature Returns
              // nested gate
              authorize Manager
              slice StateChange ReturnOrder
                command RequestReturn
                  authorize Finance
                query FindReturn => Return
                  authorize Staff
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_preserve_the_scoped_policy_trees() => SyntaxJson.StructurallyEqual(_roundtrip.Original!.Value!, _roundtrip.Reparsed.Value!).ShouldBeTrue();
    [Fact] void should_keep_the_module_comment() => _roundtrip.Printed.ShouldContain("// module gate");
    [Fact] void should_keep_the_nested_comment() => _roundtrip.Printed.ShouldContain("// nested gate");
    [Fact] void should_keep_the_declaration_order() => (_roundtrip.Printed.IndexOf("authorize Portal", StringComparison.Ordinal) < _roundtrip.Printed.IndexOf("feature Orders", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_print_stably() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_preserve_the_nested_policy() => _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Features.Single().Authorize!.References().Single().Name.ShouldEqual("Manager");
    [Fact] void should_walk_module_feature_and_nested_feature_gates()
    {
        var walker = new AuthorizationWalker();
        walker.VisitApplication(_roundtrip.Reparsed.Value!);
        walker.Names.ShouldContainOnly("Portal", "Staff", "Finance", "Manager", "Finance", "Staff");
    }

    sealed class AuthorizationWalker : ScreenplaySyntaxWalker
    {
        public List<string> Names { get; } = [];

        public override void VisitAuthorize(AuthorizeSyntax syntax)
        {
            Names.AddRange(syntax.References().Select(_ => _.Name));
            base.VisitAuthorize(syntax);
        }
    }
}
