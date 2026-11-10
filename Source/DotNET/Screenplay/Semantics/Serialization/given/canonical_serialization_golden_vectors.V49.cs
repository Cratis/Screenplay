// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

/// <summary>
/// Supplies the ESM v49 golden: every v48 construct plus a policy that compares recorded state with the caller's identifier.
/// </summary>
public static partial class canonical_serialization_golden_vectors
{
    /// <summary>
    /// The source of the v49 golden: the v48 golden plus a policy that compares a recorded author with the caller's identifier.
    /// </summary>
    public const string CallerIdentitySource = AgentExposureSource + """


        policy IsAuthor
          reads Article as article by slug
          require authenticated and article.author == $identity.id

        module Journal
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

    /// <summary>
    /// Gets the canonical bytes of the v49 golden.
    /// </summary>
    public static byte[] EsmV49Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v49.json");

    /// <summary>
    /// Creates the v49 golden model.
    /// </summary>
    /// <returns>The model.</returns>
    public static ExecutableSemanticModel CreateSemanticModelV49()
    {
        var result = property_order_binding.AdmitScreens(CallerIdentitySource);
        if (!result.Success) throw new InvalidSemanticContract(string.Join('\n', result.Refusals));
        return result.Compilation!.Model;
    }
}
#endif
