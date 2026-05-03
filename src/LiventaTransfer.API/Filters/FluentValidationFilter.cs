using System.Text.RegularExpressions;
using FluentValidation;
using LiventaTransfer.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LiventaTransfer.API.Filters;

public sealed class FluentValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public FluentValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            var argumentType = argument.GetType();
            var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);
            var validator = _serviceProvider.GetService(validatorType) as IValidator;

            if (validator is null) continue;

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext);

            if (!result.IsValid)
            {
                var errors = result.Errors.Select(FormatError).ToList();
                var payload = ApiResult<object>.Fail("Doğrulama hatası.", errors, statusCode: 400);
                context.Result = new BadRequestObjectResult(payload);
                return;
            }
        }

        await next();
    }

    private static readonly Regex StopsPathRegex = new(@"^Stops\[(\d+)\]", RegexOptions.Compiled);

    private static string FormatError(FluentValidation.Results.ValidationFailure e)
    {
        if (string.IsNullOrEmpty(e.PropertyName))
            return e.ErrorMessage;

        var match = StopsPathRegex.Match(e.PropertyName);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var idx))
            return $"Durak {idx + 1}: {e.ErrorMessage}";

        return e.ErrorMessage;
    }
}
