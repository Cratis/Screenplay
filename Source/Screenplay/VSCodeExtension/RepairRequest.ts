// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface RepairRequest {
    id: number;
    method: string;
    params: unknown;
    resolve: (value: unknown) => void;
    reject: (reason: unknown) => void;
    timer?: ReturnType<typeof setTimeout>;
    abort?: () => void;
    signal?: AbortSignal;
    cancelled: boolean;
    beforeSend?: () => void;
    discarded?: (value: unknown) => void;
}
