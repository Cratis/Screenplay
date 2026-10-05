// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_authoring_an_absence_key;

public class and_the_read_model_has_no_keyed_query : given.a_workspace_with_an_absent_read_model_key
{
    const string WithoutAssertion =
        """
        module Lending
          feature Loans
            slice StateView BooksOnLoan
              readmodel OnLoanBook
                bookId Uuid
              query ListBooksOnLoan => OnLoanBook[]
        """;

    const string WithAssertion =
        """
        module Lending
          feature Loans
            slice StateView BooksOnLoan
              readmodel OnLoanBook
                bookId Uuid
              query ListBooksOnLoan => OnLoanBook[]
              specification ReturnedBookLeavesLoan
                then no readmodel OnLoanBook for "0b8f3c5e-1d2a-4f6b-9c7d-3e4a5b6c7d8e"
        """;

    WorkspaceAuthoringResult _safe = null!;

    void Establish() => CreateWith(WithoutAssertion);

    void Because() => _safe = Workspace.ProposeAuthoring(ReplaceDocument(WorkspaceAuthoringReferencePolicy.Safe, WithAssertion));

    [Fact] void should_refuse_the_new_assertion() => _safe.Accepted.ShouldBeFalse();
    [Fact] void should_say_which_read_model_cannot_be_identified() => _safe.Conflicts.Single().Message.ShouldContain("read model 'OnLoanBook' has no keyed query");
    [Fact] void should_say_how_to_identify_it() => _safe.Conflicts.Single().Message.ShouldContain("a query that returns 'OnLoanBook' with 'by <property> <Type>'");
    [Fact] void should_name_the_draft_reference_policy() => _safe.Conflicts.Single().Message.ShouldContain("Draft reference policy");
}
