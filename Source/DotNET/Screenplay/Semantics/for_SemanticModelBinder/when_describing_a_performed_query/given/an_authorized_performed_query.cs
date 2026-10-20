// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_describing_a_performed_query.given;

public class an_authorized_performed_query : for_SemanticModelBinder.given.a_semantic_binder
{
    protected const string Source =
        """
        policy Buyers
          require claim "role" matches "buyer"
        policy Anyone
          require anonymous
        module Purchasing
          authorize Buyers
          feature Overview
            slice StateView Figures
              readmodel Figures
                figureId String
                total Int
              query OpenFigures => Figures
                by figureId String
                performer
                  file Queries/OpenFigures.cs
              query PublicFigures => Figures[]
                authorize Anyone
                performer
                  file Queries/PublicFigures.cs
              query FigureList => Figures[]
                performer
                  file Queries/FigureList.cs
            slice StateView Notices
              readmodel Notice
                noticeId String
                text String
              query OpenNotices => Notice[]
                performer
                  file Queries/OpenNotices.cs
        module Open
          feature Notices
            slice StateView BannerView
              readmodel Banner
                bannerId String
                text String
              query CurrentBanner => Banner[]
                performer
                  file Queries/CurrentBanner.cs
        """;

    protected CompilationResult<SemanticCompilation> _result;

    protected SemanticPerformedQueryShape ShapeOf(string query) =>
        _result.ImplementationRequirements.Single(requirement =>
            requirement.Role == SemanticImplementationRole.QueryPerformer && requirement.Owner.Name == query).QueryShape!;

    void Establish() => _result = Bind(Source);
}
