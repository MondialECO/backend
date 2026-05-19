using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebApp.Models;

namespace WebApp.Filters
{
    /// <summary>
    /// Runs any registered FluentValidation validator for each action
    /// argument before the action executes. On failure it short-circuits
    /// with a 400 using the shared ApiResponse envelope, so validation
    /// errors have the same shape as every other API response.
    /// </summary>
    public class ValidationFilter : IAsyncActionFilter
    {
        private readonly IServiceProvider _services;

        public ValidationFilter(IServiceProvider services) => _services = services;

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var errors = new Dictionary<string, string[]>();

            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument is null) continue;

                var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
                if (_services.GetService(validatorType) is not IValidator validator) continue;

                var validationContext = new ValidationContext<object>(argument);
                var result = await validator.ValidateAsync(validationContext);
                if (result.IsValid) continue;

                foreach (var failure in result.Errors)
                {
                    var key = failure.PropertyName;
                    var message = failure.ErrorMessage;
                    errors[key] = errors.TryGetValue(key, out var existing)
                        ? existing.Append(message).ToArray()
                        : new[] { message };
                }
            }

            if (errors.Count > 0)
            {
                var payload = ApiResponse.Error(
                    "Validation failed.",
                    context.HttpContext.TraceIdentifier,
                    errors);
                context.Result = new BadRequestObjectResult(payload);
                return;
            }

            await next();
        }
    }
}
