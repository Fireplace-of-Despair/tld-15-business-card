// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using System.Threading.Tasks;
using Serilog;
using StainlessCore.Exceptions;

namespace StainlessCore;

/// <summary>
/// Runs a feature and turns an exception into an incident code. The caller reads the outcome instead of
/// catching an exception.
/// </summary>
public static class Execute
{
    /// <summary> Execution result </summary>
    /// <typeparam name="T">Type</typeparam>
    public sealed record Result<T>
    {
        /// <summary> The data returned from the feature execution </summary>
        public T? Data { get; init; }

        /// <summary>
        /// The incident code that indicates the type of error that occurred during the feature execution.
        /// </summary>
        public IncidentCode? IncidentCode { get; init; }

        /// <summary> Create a Result instance </summary>
        /// <param name="data">Execution result </param>
        /// <param name="incident">Execution incident </param>
        private Result(T? data = default, IncidentCode? incident = null)
        {
            Data = data;
            IncidentCode = incident;
        }

        /// <summary> Creates a successful result with the given data </summary>
        internal static Result<T> Success(T data) => new(data);

        /// <summary> Creates a failed result with the given incident code </summary>
        internal static Result<T> Failure(IncidentCode incident) => new(incident: incident);
    }

    /// <summary> Run a feature and catch every exception that it throws. </summary>
    /// <typeparam name="T">Type of the data that the feature returns.</typeparam>
    /// <param name="func">The feature to run.</param>
    /// <returns>A successful result, or a failed result that carries an incident code.</returns>
    /// <remarks>
    /// An <see cref="IncidentException"/> keeps its own code. Every other exception becomes
    /// <see cref="Exceptions.IncidentCode.General"/>. Serilog logs the real exception.
    /// </remarks>
    public static async Task<Result<T>> Run<T>(Func<Task<T>> func)
    {
        try
        {
            var result = await func().ConfigureAwait(false);
            return Result<T>.Success(result);
        }
        catch (IncidentException ex)
        {
            Log.Error(ex, "Incident {IncidentCode}", ex.Code);
            return Result<T>.Failure(ex.Code);
        }
        catch (Exception ee)
        {
            Log.Error(ee, "Exception");
            return Result<T>.Failure(IncidentCode.General);
        }
    }
}
