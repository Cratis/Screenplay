// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Languages;

// Compiler wrappers must keep discovery and parsing on the same caller-owned registry.
internal interface ILanguageRegistryOwner
{
    IScreenplayLanguageRegistry Languages { get; }
}
