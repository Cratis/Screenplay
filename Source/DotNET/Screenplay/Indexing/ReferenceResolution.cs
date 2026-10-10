// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Indexing;

sealed record ReferenceResolution(AuthoredReference Reference, AuthoredDeclaration[] Candidates);
