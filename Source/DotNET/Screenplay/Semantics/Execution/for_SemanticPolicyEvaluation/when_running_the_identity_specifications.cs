// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticPolicyEvaluation;

// Only the author of an article may edit it: the policy compares the recorded author with the caller's identifier ($identity.id).
public class when_running_the_identity_specifications : Specification
{
    const string Source =
        """
        policy IsAuthor
          reads Article as article by slug
          require authenticated and article.author == $identity.id
        module Blog
          feature Articles
            slice StateChange EditArticle
              event ArticleEdited
                title String
              command EditArticle
                slug String identifier
                title String
                authorize IsAuthor
                produces ArticleEdited
                  for slug
                  title = title
              specification TheAuthorMayEdit
                given readmodel Article
                  slug = "intro"
                  author = "alice"
                given caller
                  authenticated
                  id "alice"
                when EditArticle
                  slug = "intro"
                  title = "Hello"
                then ArticleEdited
                  title = "Hello"
              specification SomeoneElseIsDenied
                given readmodel Article
                  slug = "intro"
                  author = "alice"
                given caller
                  authenticated
                  id "bob"
                when EditArticle
                  slug = "intro"
                  title = "Hello"
                then denied
              specification ACallerWithNoIdentifierIsDenied
                given readmodel Article
                  slug = "intro"
                  author = "alice"
                given caller
                  authenticated
                when EditArticle
                  slug = "intro"
                  title = "Hello"
                then denied
              specification AnArticleWithNoRecordedAuthorIsDenied
                given caller
                  authenticated
                  id "alice"
                when EditArticle
                  slug = "intro"
                  title = "Hello"
                then denied
            slice StateView Articles
              readmodel Article
                slug String key
                author String
              event ArticleWritten
                author String
              projection BuildArticle => Article
                from ArticleWritten
                  slug = $eventSourceId
                  author = author
        """;

    SemanticCompilation _compilation;
    SemanticSpecificationRun[] _runs;
    string[] _errors;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Blog"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("blog"), "blog", "blog.play", Source);
        var result = new SemanticModelCompiler().Compile("Blog", SemanticDocumentSet.Create([document], catalog));
        _errors = [.. result.Diagnostics.Where(diagnostic => diagnostic.Severity == Diagnostics.DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Message)];
        _compilation = result.Value;
    }

    void Because()
    {
        if (_compilation is null) return;
        var plan = SemanticExecutionPlan.Compile(_compilation.Model).Plan!;
        var runner = new SemanticSpecificationRunner();
        _runs = [.. plan.Specifications.Values.Select(specification => runner.Run(plan, specification.Id))];
    }

    [Fact] void should_compile() => string.Join(" | ", _errors).ShouldEqual(string.Empty);
    [Fact] void should_select_v49() => _compilation.Model.SemanticVersion.ShouldEqual(SemanticVersion.V49);
    [Fact] void should_run_every_specification() => _runs.Length.ShouldEqual(4);
    [Fact] void should_pass_every_specification() => _runs.SelectMany(run => run.Failures).ShouldBeEmpty();
}
