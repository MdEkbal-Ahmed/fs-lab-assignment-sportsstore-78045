using System.Text;
using Microsoft.AspNetCore.Hosting;
using Stripe;

namespace SportsStore;

/// <summary>
/// Handles Stripe webhook events. Logs card decline / payment failure to Serilog (Console, File, Seq).
/// </summary>
public static class StripeWebhookHandler
{
    public const string WebhookPath = "/webhooks/stripe";

    public static async Task<IResult> HandleAsync(
        HttpRequest request,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken = default)
    {
        var logger = loggerFactory.CreateLogger("StripeWebhook");

        var signature = request.Headers["Stripe-Signature"].FirstOrDefault();
        var webhookSecret = configuration["Stripe:WebhookSecret"]?.Trim();
        var skipVerification = environment.IsDevelopment() && configuration.GetValue<bool>("Stripe:SkipWebhookSignatureVerification");

        if (string.IsNullOrWhiteSpace(signature) && !skipVerification)
        {
            logger.LogWarning("Stripe webhook received without Stripe-Signature header.");
            return Results.BadRequest();
        }

        request.EnableBuffering();
        request.Body.Position = 0;

        using var ms = new MemoryStream();
        await request.Body.CopyToAsync(ms, cancellationToken);
        byte[] bytes = ms.ToArray();
        string body = Encoding.UTF8.GetString(bytes);

        if (string.IsNullOrEmpty(body))
        {
            logger.LogWarning("Stripe webhook received with empty body. BodyLength: 0");
            return Results.BadRequest();
        }

        body = body.Replace("\r\n", "\n").Replace("\r", "\n");

        Event? stripeEvent;
        if (skipVerification)
        {
            logger.LogWarning("Stripe webhook signature verification SKIPPED (Development only). Set Stripe:SkipWebhookSignatureVerification=false and use correct whsec_ secret for real verification.");
            stripeEvent = EventUtility.ParseEvent(body, throwOnApiVersionMismatch: false);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(webhookSecret))
            {
                logger.LogWarning("Stripe webhook received but Stripe:WebhookSecret is not configured.");
                return Results.BadRequest();
            }
            try
            {
                stripeEvent = EventUtility.ConstructEvent(body, signature!, webhookSecret, tolerance: 600);
            }
            catch (StripeException ex)
            {
                logger.LogWarning(ex, "Stripe webhook signature verification failed. BodyLength: {BodyLength}. Tip: When using Stripe CLI, copy the whsec_... secret from the CLI output and run: dotnet user-secrets set \"Stripe:WebhookSecret\" \"whsec_...\" --project SportsStore.csproj", body.Length);
                return Results.BadRequest();
            }
        }

        // Log payment success and decline/failure to Seq (and Console/File)
        if (stripeEvent.Type == "checkout.session.completed")
        {
            logger.LogInformation(
                "Stripe payment success. EventId: {StripeEventId}, EventType: {StripeEventType}, Message: Checkout completed, payment succeeded.",
                stripeEvent.Id,
                stripeEvent.Type);
        }
        else if (stripeEvent.Type == "payment_intent.payment_failed" || stripeEvent.Type == "charge.failed")
        {
            var message = stripeEvent.Type == "charge.failed"
                ? "Stripe card declined or charge failed."
                : "Stripe payment intent failed (e.g. card declined).";

            logger.LogWarning(
                "Stripe payment declined or failed. EventId: {StripeEventId}, EventType: {StripeEventType}, Message: {Message}",
                stripeEvent.Id,
                stripeEvent.Type,
                message);
        }

        return Results.Ok();
    }
}
