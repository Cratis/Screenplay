// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_projection_variants;

public class global_handlers_check_each_known_shape : given.a_compiler
{
    const string Source =
        """
        module Work
          feature Tracking
            slice StateChange Changes
              event IssueCreated
                title String
              event IssueStarted
                title String
              event TitleChanged
                title String
            slice StateView Items
              readmodel BacklogItem
                title String?
              readmodel DevelopmentItem
                buildStatus String?
              projection WorkItem
                from TitleChanged
                  title = title
                variant BacklogItem
                  enters on IssueCreated
                variant DevelopmentItem
                  enters on IssueStarted
        """;

    CompilationResult<Syntax.ApplicationSyntax> _result;
    CompilationResult<Syntax.ApplicationSyntax> _valid;
    CompilationResult<Syntax.ApplicationSyntax> _unknown;
    CompilationResult<Syntax.ApplicationSyntax> _every;
    CompilationResult<Syntax.ApplicationSyntax> _join;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _valid = _compiler.Compile(Source.Replace("buildStatus String?", "title String?", StringComparison.Ordinal));
        _unknown = _compiler.Compile(Source.Replace("readmodel DevelopmentItem", "readmodel OtherItem", StringComparison.Ordinal));
        _every = _compiler.Compile(Source.Replace("from TitleChanged\n          title = title", "every\n          title = title", StringComparison.Ordinal));
        _join = _compiler.Compile(Source.Replace("from TitleChanged\n          title = title", "join item on title\n          with TitleChanged\n            title = title", StringComparison.Ordinal));
    }

    [Fact] void should_reject_a_shared_member_missing_from_a_known_variant() =>
        _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.GlobalHandlerPropertyNotOnVariant).ShouldBeTrue();
    [Fact] void should_accept_a_shared_member_on_all_known_variants() =>
        _valid.Diagnostics.All(diagnostic => diagnostic.Code != DiagnosticCodes.GlobalHandlerPropertyNotOnVariant).ShouldBeTrue();
    [Fact] void should_leave_an_unknown_variant_shape_undecided() =>
        string.Join("; ", _unknown.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.GlobalHandlerPropertyNotOnVariant).Select(diagnostic => diagnostic.Message)).ShouldEqual(string.Empty);
    [Fact] void should_check_every_mappings() =>
        _every.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.GlobalHandlerPropertyNotOnVariant).ShouldBeTrue();
    [Fact] void should_check_join_mappings() =>
        _join.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.GlobalHandlerPropertyNotOnVariant).ShouldBeTrue();
}
