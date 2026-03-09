using Stripe;
using Stripe.Checkout;

namespace SportsStore.Services {

    /// <summary>
    /// Stripe payment implementation using official Stripe .NET SDK. Uses test keys only when configured via User Secrets or environment.
    /// </summary>
    public class StripePaymentService : IStripePaymentService {
        private readonly IConfiguration _configuration;
        private readonly ILogger<StripePaymentService> _logger;

        public StripePaymentService(IConfiguration configuration, ILogger<StripePaymentService> logger) {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> CreateCheckoutSessionAsync(
            long amountTotalCents,
            string orderDescription,
            string successUrl,
            string cancelUrl,
            IReadOnlyDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default) {

            var secretKey = _configuration["Stripe:SecretKey"];
            if (string.IsNullOrWhiteSpace(secretKey)) {
                _logger.LogError("Stripe:SecretKey is not configured. Use User Secrets or environment variables.");
                throw new InvalidOperationException(
                    "Stripe is not configured. Set Stripe:SecretKey (e.g. via 'dotnet user-secrets set Stripe:SecretKey sk_test_...').");
            }

            StripeConfiguration.ApiKey = secretKey;

            var options = new SessionCreateOptions {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions> {
                    new SessionLineItemOptions {
                        PriceData = new SessionLineItemPriceDataOptions {
                            Currency = "usd",
                            UnitAmount = amountTotalCents,
                            ProductData = new SessionLineItemPriceDataProductDataOptions {
                                Name = orderDescription,
                                Description = "Sports Store order",
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
            };

            if (metadata != null && metadata.Count > 0) {
                options.Metadata = new Dictionary<string, string>(metadata);
            }

            var service = new SessionService();
            var session = await service.CreateAsync(options, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Stripe Checkout Session created. SessionId: {SessionId}, AmountCents: {AmountCents}",
                session.Id,
                amountTotalCents);

            return session.Url ?? throw new InvalidOperationException("Stripe did not return a checkout URL.");
        }

        public async Task<StripePaymentResult> GetSessionPaymentStatusAsync(string sessionId, CancellationToken cancellationToken = default) {
            var secretKey = _configuration["Stripe:SecretKey"];
            if (string.IsNullOrWhiteSpace(secretKey)) {
                _logger.LogError("Stripe:SecretKey is not configured.");
                return new StripePaymentResult { IsPaid = false };
            }

            StripeConfiguration.ApiKey = secretKey;

            var service = new SessionService();
            var session = await service.GetAsync(sessionId, cancellationToken: cancellationToken);

            var result = new StripePaymentResult {
                SessionId = session.Id,
                PaymentIntentId = session.PaymentIntentId,
                IsPaid = session.PaymentStatus == "paid",
            };

            _logger.LogInformation(
                "Stripe session retrieved. SessionId: {SessionId}, PaymentStatus: {PaymentStatus}, IsPaid: {IsPaid}",
                session.Id,
                session.PaymentStatus,
                result.IsPaid);

            return result;
        }
    }
}
