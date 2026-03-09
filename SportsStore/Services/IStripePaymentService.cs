namespace SportsStore.Services {

    /// <summary>
    /// Abstraction for Stripe payment operations. Keeps payment logic separate from controllers.
    /// </summary>
    public interface IStripePaymentService {

        /// <summary>
        /// Creates a Stripe Checkout Session and returns the URL to redirect the customer to.
        /// </summary>
        /// <param name="amountTotalCents">Total amount to charge in cents (e.g. 1999 = $19.99)</param>
        /// <param name="orderDescription">Description or reference for the payment (e.g. "Sports Store order")</param>
        /// <param name="successUrl">URL to redirect to after successful payment (e.g. https://localhost:5001/Order/Success?session_id={CHECKOUT_SESSION_ID})</param>
        /// <param name="cancelUrl">URL to redirect to if the customer cancels</param>
        /// <param name="metadata">Optional key-value metadata to attach to the session</param>
        /// <returns>Checkout session URL to redirect the user to</returns>
        Task<string> CreateCheckoutSessionAsync(
            long amountTotalCents,
            string orderDescription,
            string successUrl,
            string cancelUrl,
            IReadOnlyDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a Checkout Session and returns whether payment was completed successfully.
        /// </summary>
        /// <param name="sessionId">Stripe Checkout Session ID (e.g. cs_test_...)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Payment result with SessionId, PaymentIntentId (if paid), and success flag</returns>
        Task<StripePaymentResult> GetSessionPaymentStatusAsync(string sessionId, CancellationToken cancellationToken = default);
    }

    public class StripePaymentResult {
        public bool IsPaid { get; set; }
        public string? SessionId { get; set; }
        public string? PaymentIntentId { get; set; }
    }
}
