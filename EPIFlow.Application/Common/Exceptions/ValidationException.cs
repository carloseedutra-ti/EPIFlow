using System;
using System.Collections.Generic;

namespace EPIFlow.Application.Common.Exceptions;

public class ValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(string message, IReadOnlyDictionary<string, string[]> errors) : base(message)
    {
        Errors = errors;
    }
}
