// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFolderMerge;

public class when_merging_authorization_lines_with_comments : Specification
{
    static readonly string[] _moduleComments =
    [
        "// First gate.", "// First gate trailing.", "// First second line.", "// First second trailing.", "// First third line.", "// First third trailing.",
        "// Second gate.", "// Second gate trailing.", "// Second second line.", "// Second second trailing.", "// Second third line.", "// Second third trailing."
    ];

    static readonly string[] _featureComments =
    [
        "// First feature gate.", "// First feature second line.", "// First feature second trailing.",
        "// Second feature gate.", "// Second feature second line.", "// Second feature second trailing."
    ];

    CompilationResult<ApplicationSyntax> _result;
    CompilationResult<ApplicationSyntax> _roundTripped;
    ModuleSyntax _module;
    string _printed;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        _result = PlayFolderMerge.Merge([
            compiler.Parse("""
                policy SignedIn
                  require authenticated
                policy Allowed
                  require authenticated
                policy Audited
                  require authenticated
                module Example
                  // First gate.
                  authorize SignedIn // First gate trailing.
                  // First second line.
                  authorize Allowed // First second trailing.
                  // First third line.
                  authorize Audited // First third trailing.
                  feature View
                    // First feature gate.
                    authorize SignedIn
                    // First feature second line.
                    authorize Allowed // First feature second trailing.
                    slice StateView First
                """,
                "First.play"),
            compiler.Parse("""
                module Example
                  // Second gate.
                  authorize SignedIn // Second gate trailing.
                  // Second second line.
                  authorize Allowed // Second second trailing.
                  // Second third line.
                  authorize Audited // Second third trailing.
                  feature View
                    // Second feature gate.
                    authorize Audited
                    // Second feature second line.
                    authorize SignedIn // Second feature second trailing.
                    slice StateView Second
                """,
                "Second.play")
        ]);
        _module = _result.Value!.Modules.Single();
        _printed = new ScreenplayPrinter().Print(_result.Value);
        _roundTripped = compiler.Parse(_printed);
    }

    [Fact] void should_keep_every_module_comment_once_in_file_then_source_order() => _module.Authorize!.SourceComments.Select(comment => comment.Text).ShouldEqual(_moduleComments);
    [Fact] void should_keep_every_feature_comment_once_in_file_then_source_order() => _module.Features.Single().Authorize!.SourceComments.Select(comment => comment.Text).ShouldEqual(_featureComments);
    [Fact] void should_leave_no_comment_on_module_requirements() => Requirements(_module.Authorize!.Requirement).SelectMany(requirement => requirement.SourceComments).ShouldBeEmpty();
    [Fact] void should_leave_no_comment_on_feature_requirements() => Requirements(_module.Features.Single().Authorize!.Requirement).SelectMany(requirement => requirement.SourceComments).ShouldBeEmpty();
    [Fact] void should_print_module_comments_immediately_above_the_gate() => _printed.Contains(string.Concat(_moduleComments.Select(comment => $"  {comment}\n")) + "  authorize SignedIn and Allowed and Audited\n", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_print_feature_comments_immediately_above_the_gate() => _printed.Contains(string.Concat(_featureComments.Select(comment => $"    {comment}\n")) + "    authorize SignedIn and Allowed and (Audited and SignedIn)\n", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_reparse_successfully() => _roundTripped.Success.ShouldBeTrue();
    [Fact] void should_keep_module_comments_after_reparsing() => _roundTripped.Value!.Modules.Single().Authorize!.SourceComments.Select(comment => comment.Text).ShouldEqual(_moduleComments);
    [Fact] void should_keep_feature_comments_after_reparsing() => _roundTripped.Value!.Modules.Single().Features.Single().Authorize!.SourceComments.Select(comment => comment.Text).ShouldEqual(_featureComments);
    [Fact] void should_warn_only_about_the_duplicate_module_gate() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual([DiagnosticCodes.DuplicateAuthorizationAcrossFiles]);

    static IEnumerable<PolicyRequirementSyntax> Requirements(PolicyRequirementSyntax requirement) => requirement is LogicalPolicyRequirementSyntax logical
        ? [logical, .. Requirements(logical.Left), .. Requirements(logical.Right)]
        : [requirement];
}
