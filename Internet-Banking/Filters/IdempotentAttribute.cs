using System.Security.Claims;
using System.Text.Json;
using Internet_Banking.Data;
using Internet_Banking.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Internet_Banking.Filters;

[AttributeUsage(AttributeTargets.Method)]
public class IdempotentAttribute : Attribute, IAsyncActionFilter
{
    private const string HeaderName = "Idempotency-Key";
    private const int MaxKeyLength = 100;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var key = context.HttpContext.Request.Headers[HeaderName].ToString().Trim();

        if(string.IsNullOrEmpty(key) || key.Length > MaxKeyLength)
        {
            context.Result = new BadRequestObjectResult(
                $"The {HeaderName} header is required and must be at most {MaxKeyLength} characters.");
            return;
        }

        var customerIdValue = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(customerIdValue, out var customerId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // The filter's bookkeeping runs in its own scope
        var scopeFactory = context.HttpContext.RequestServices.GetRequiredService<IServiceScopeFactory>();

        Guid recordId;

        using (var scope = scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<Internet_BankingDbContext>();

            var existing = await dbContext.IdempotencyRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.CustomerId == customerId && r.IdempotencyKey == key);

            if (existing is not null)
            {
                if (existing.StatusCode is null)
                {
                    context.Result = new ConflictObjectResult("A request with this Idempotency-Key is already being processed.");
                    return;
                }

                context.HttpContext.Response.Headers["Idempotency-Replayed"] = "true";
                context.Result = new ContentResult
                {
                    StatusCode = existing.StatusCode,
                    Content = existing.ResponseBody,
                    ContentType = "application/json"
                };
                return;
            }

            // record the key before running the endpoint
            var record = new IdempotencyRecord
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                IdempotencyKey = key
            };

            dbContext.IdempotencyRecords.Add(record);

            try
            {
                await dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Another request with the same key inserted first.
                context.Result = new ConflictObjectResult("A request with this Idempotency-Key is already being processed.");
                return;
            }

            recordId = record.Id;
        }

        // Run the real endpoint.
        var executed = await next();

        var statusCode = (executed.Result as IStatusCodeActionResult)?.StatusCode;
        var succeeded = executed.Exception is null && statusCode is >= 200 and < 300;

        if (succeeded)
        {            
            string? body = null;

            if (executed.Result is ObjectResult { Value: not null } objectResult)
            {
                var jsonOptions = context.HttpContext.RequestServices
                    .GetRequiredService<IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>>()
                    .Value.JsonSerializerOptions;

                body = JsonSerializer.Serialize(objectResult.Value, jsonOptions);
            }

            // The first scope has been disposed, so a new one is opened
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Internet_BankingDbContext>();

            await dbContext.IdempotencyRecords
                .Where(r => r.Id == recordId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(r => r.StatusCode, statusCode)
                    .SetProperty(r => r.ResponseBody, body));
        }
        else
        {
            // Delete the record so the client can retry with the same key instead of being stuck with a 409 if the request failed
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Internet_BankingDbContext>();

            await dbContext.IdempotencyRecords
                .Where(r => r.Id == recordId)
                .ExecuteDeleteAsync();
        }

    }
}
