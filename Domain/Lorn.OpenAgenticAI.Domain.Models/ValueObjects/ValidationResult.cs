using System;
using System.Collections.Generic;
using System.Linq;
using Lorn.OpenAgenticAI.Domain.Models.Common;

namespace Lorn.OpenAgenticAI.Domain.Models.ValueObjects;

/// <summary>
/// 验证结果值对象
/// </summary>
public class ValidationResult : ValueObject
{
    public bool IsValid => !Errors.Any();
    public List<ValidationError> Errors { get; private set; } = [];
    public string Summary => string.Join("; ", Errors.Select(e => e.ErrorMessage));

    public ValidationResult()
    {
        Errors = [];
    }

    public ValidationResult(List<ValidationError> errors)
    {
        Errors = errors ?? [];
    }

    /// <summary>
    /// 添加验证错误
    /// </summary>
    public void AddError(ValidationError error)
    {
        if (error != null)
        {
            Errors.Add(error);
        }
    }

    /// <summary>
    /// 添加验证错误
    /// </summary>
    public void AddError(string propertyName, string errorMessage)
    {
        Errors.Add(new ValidationError(propertyName, errorMessage));
    }

    /// <summary>
    /// 合并其他验证结果
    /// </summary>
    public void Merge(ValidationResult? other)
    {
        if (other?.Errors != null)
        {
            Errors.AddRange(other.Errors);
        }
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return IsValid;
        foreach (var error in Errors.OrderBy(e => e.PropertyName))
        {
            yield return error.PropertyName;
            yield return error.ErrorMessage;
        }
    }

    public override string ToString()
    {
        return IsValid ? "Valid" : $"Invalid: {Summary}";
    }
}

/// <summary>
/// 验证错误
/// </summary>
public class ValidationError
{
    public string PropertyName { get; private set; } = string.Empty;
    public string ErrorMessage { get; private set; } = string.Empty;
    public string ErrorCode { get; private set; } = string.Empty;
    public object? AttemptedValue { get; private set; }

    public ValidationError(string propertyName, string errorMessage, string? errorCode = null, object? attemptedValue = null)
    {
        PropertyName = propertyName ?? string.Empty;
        ErrorMessage = errorMessage ?? string.Empty;
        ErrorCode = errorCode ?? string.Empty;
        AttemptedValue = attemptedValue;
    }

    public override string ToString()
    {
        return $"{PropertyName}: {ErrorMessage}";
    }
}