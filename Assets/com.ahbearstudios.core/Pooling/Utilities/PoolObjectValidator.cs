using System;
using System.Collections.Generic;
using AhBearStudios.Pooling.Services;
using UnityEngine;

namespace AhBearStudios.Pooling.Core.Pooling.Core

//namespace AhBearStudios.Pooling.Utilities
{
    /// <summary>
    /// Provides validation functionality for pooled objects
    /// </summary>
    /// <typeparam name="T">Type of objects to validate</typeparam>
    public class PoolObjectValidator<T> where T : class
    {
        private readonly List<Func<T, bool>> _validationRules = new List<Func<T, bool>>();
        private readonly List<string> _validationMessages = new List<string>();
        private readonly bool _logValidationFailures;
        private readonly string _validatorName;
        
        /// <summary>
        /// Creates a new validator with optional logging of validation failures
        /// </summary>
        /// <param name="logValidationFailures">Whether to log validation failures to the console</param>
        /// <param name="validatorName">Optional name for the validator, used for diagnostics</param>
        public PoolObjectValidator(bool logValidationFailures = true, string validatorName = null)
        {
            _logValidationFailures = logValidationFailures;
            _validatorName = validatorName ?? $"{typeof(T).Name}Validator";
        }
        
        /// <summary>
        /// Adds a validation rule with an optional description
        /// </summary>
        /// <param name="validationRule">Function that returns true if the object is valid</param>
        /// <param name="description">Description of the validation rule</param>
        /// <returns>This validator for method chaining</returns>
        public PoolObjectValidator<T> AddRule(Func<T, bool> validationRule, string description = null)
        {
            if (validationRule == null)
                throw new ArgumentNullException(nameof(validationRule));
                
            _validationRules.Add(validationRule);
            _validationMessages.Add(description ?? $"Validation rule #{_validationRules.Count}");
            
            return this;
        }
        
        /// <summary>
        /// Adds a validation rule that checks if a specific component reference is not null
        /// </summary>
        /// <typeparam name="TComponent">Type of component to check</typeparam>
        /// <param name="componentGetter">Function to get the component from the object</param>
        /// <param name="componentName">Name of the component for error messages</param>
        /// <returns>This validator for method chaining</returns>
        public PoolObjectValidator<T> RequireComponent<TComponent>(Func<T, TComponent> componentGetter, string componentName = null) 
            where TComponent : class
        {
            if (componentGetter == null)
                throw new ArgumentNullException(nameof(componentGetter));
                
            string name = componentName ?? typeof(TComponent).Name;
            return AddRule(
                obj => componentGetter(obj) != null,
                $"Required component {name} is missing");
        }
        
        /// <summary>
        /// Validates an object against all added rules
        /// </summary>
        /// <param name="obj">The object to validate</param>
        /// <returns>True if the object passes all validation rules, false otherwise</returns>
        public bool Validate(T obj)
        {
            if (obj == null)
            {
                if (_logValidationFailures)
                {
                    string message = $"Validation failed: Object is null";
                    Debug.LogWarning(message);
                    PoolingServices.Logger?.LogWarningInstance(message);
                }
                return false;
            }
            
            var profiler = PoolingServices.Profiler;
            if (profiler != null) profiler.BeginSample("Validate", _validatorName);
            
            bool isValid = true;
            
            try
            {
                for (int i = 0; i < _validationRules.Count; i++)
                {
                    try
                    {
                        bool ruleResult = _validationRules[i](obj);
                        if (!ruleResult)
                        {
                            isValid = false;
                            
                            if (_logValidationFailures)
                            {
                                string message = $"Validation failed for {obj}: {_validationMessages[i]}";
                                Debug.LogWarning(message);
                                PoolingServices.Logger?.LogWarningInstance(message);
                            }
                            
                            // If we're logging all failures, continue checking other rules
                            // Otherwise, return false immediately
                            if (!_logValidationFailures)
                            {
                                return false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        isValid = false;
                        
                        if (_logValidationFailures)
                        {
                            string message = $"Exception during validation of {obj}: {_validationMessages[i]}";
                            Debug.LogException(ex);
                            PoolingServices.Logger?.LogWarningInstance(message);
                        }
                        
                        if (!_logValidationFailures)
                        {
                            return false;
                        }
                    }
                }
                
                return isValid;
            }
            finally
            {
                if (profiler != null) profiler.EndSample("Validate", _validatorName, 0, 0);
            }
        }
        
        /// <summary>
        /// Returns a function that performs validation using this validator
        /// </summary>
        /// <returns>A function that takes an object and returns whether it's valid</returns>
        public Func<T, bool> AsValidationFunction()
        {
            return Validate;
        }
        
        /// <summary>
        /// Creates a validator with a single rule
        /// </summary>
        /// <param name="validationRule">The validation rule</param>
        /// <param name="description">Description of the rule</param>
        /// <param name="logFailures">Whether to log validation failures</param>
        /// <returns>A new validator with the specified rule</returns>
        public static PoolObjectValidator<T> Create(Func<T, bool> validationRule, string description = null, bool logFailures = true)
        {
            return new PoolObjectValidator<T>(logFailures).AddRule(validationRule, description);
        }
        
        /// <summary>
        /// Creates a validator that performs multiple checks on an object's state
        /// </summary>
        /// <param name="validationRules">Dictionary mapping rule descriptions to validation functions</param>
        /// <param name="logFailures">Whether to log validation failures</param>
        /// <returns>A new validator with the specified rules</returns>
        public static PoolObjectValidator<T> CreateMultiRule(Dictionary<string, Func<T, bool>> validationRules, bool logFailures = true)
        {
            if (validationRules == null)
                throw new ArgumentNullException(nameof(validationRules));
                
            var validator = new PoolObjectValidator<T>(logFailures);
            
            foreach (var rule in validationRules)
            {
                validator.AddRule(rule.Value, rule.Key);
            }
            
            return validator;
        }
    }
}