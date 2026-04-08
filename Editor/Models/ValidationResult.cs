using System;
using System.Collections.Generic;

namespace SkyWalker.Craft.Editor.Models
{
    [Serializable]
    public class ValidationResult
    {
        public bool valid;
        public List<ValidationError> errors;
        public List<string> warnings;

        public ValidationResult()
        {
            errors = new List<ValidationError>();
            warnings = new List<string>();
        }

        public static ValidationResult Ok()
        {
            return new ValidationResult { valid = true };
        }

        public static ValidationResult Fail(string message, string operationType = null, int operationIndex = -1)
        {
            var result = new ValidationResult { valid = false };
            result.errors.Add(new ValidationError
            {
                message = message,
                operationType = operationType,
                operationIndex = operationIndex
            });
            return result;
        }

        public void AddError(string message, string operationType = null, int operationIndex = -1)
        {
            valid = false;
            errors.Add(new ValidationError
            {
                message = message,
                operationType = operationType,
                operationIndex = operationIndex
            });
        }

        public void AddWarning(string message)
        {
            warnings.Add(message);
        }

        public void Merge(ValidationResult other)
        {
            if (!other.valid)
                valid = false;
            errors.AddRange(other.errors);
            warnings.AddRange(other.warnings);
        }
    }

    [Serializable]
    public class ValidationError
    {
        public string message;
        public string operationType;
        public int operationIndex;
    }
}
