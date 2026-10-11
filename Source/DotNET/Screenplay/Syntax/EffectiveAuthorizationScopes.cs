// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

internal static class EffectiveAuthorizationScopes
{
    internal static IEnumerable<AuthorizeSyntax?> For(ModuleSyntax module, IEnumerable<string> featurePath, AuthorizeSyntax? own)
    {
        var scopes = new List<AuthorizeSyntax?> { module.Authorize };
        var features = module.Features;
        foreach (var name in featurePath)
        {
            var feature = features.Single(value => value.Name == name);
            scopes.Add(feature.Authorize);
            features = feature.Features;
        }

        scopes.Add(own);

        return scopes;
    }
}
