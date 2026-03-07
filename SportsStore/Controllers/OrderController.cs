using Microsoft.AspNetCore.Mvc;
using SportsStore.Models;
using SportsStore.Services;
using SportsStore.Infrastructure;

namespace SportsStore.Controllers {

    public class OrderController : Controller {
        private readonly IOrderRepository _repository;
        private readonly IStoreRepository _storeRepository;
        private readonly Cart _cart;
        private readonly IStripePaymentService _stripePayment;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            IOrderRepository repoService,
            IStoreRepository storeRepository,
            Cart cartService,
            IStripePaymentService stripePayment,
            ILogger<OrderController> logger) {
            _repository = repoService;
            _storeRepository = storeRepository;
            _cart = cartService;
            _stripePayment = stripePayment;
            _logger = logger;
        }

        public ViewResult Checkout() {
            _logger.LogInformation(
                "Checkout page requested. CartId: {CartId}, CartItemCount: {CartItemCount}, Username: {Username}",
                _cart.CartId,
                _cart.Lines.Count,
                User?.Identity?.Name ?? "Anonymous");
            return View(new Order());
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(Order order, CancellationToken cancellationToken) {
            if (_cart.Lines.Count == 0) {
                _logger.LogWarning(
                    "Checkout attempted with empty cart. CartId: {CartId}, Username: {Username}",
                    _cart.CartId,
                    User?.Identity?.Name ?? "Anonymous");
                ModelState.AddModelError("", "Sorry, your cart is empty!");
                return View(order);
            }

            if (!ModelState.IsValid) {
                return View(order);
            }

            decimal total = _cart.ComputeTotalValue();
            _logger.LogInformation(
                "Checkout submitted (proceeding to payment). CartId: {CartId}, CustomerName: {CustomerName}, ItemCount: {ItemCount}, TotalAmount: {TotalAmount}, Username: {Username}",
                _cart.CartId,
                order.Name,
                _cart.Lines.Count,
                total,
                User?.Identity?.Name ?? "Anonymous");

            var pending = new PendingOrderData {
                Name = order.Name,
                Line1 = order.Line1,
                Line2 = order.Line2,
                Line3 = order.Line3,
                City = order.City,
                State = order.State,
                Zip = order.Zip,
                Country = order.Country,
                GiftWrap = order.GiftWrap,
                Lines = _cart.Lines.Select(l => new PendingOrderLine {
                    ProductID = l.Product.ProductID ?? 0,
                    Quantity = l.Quantity,
                }).ToList(),
            };

            long amountCents = (long)(total * 100);
            if (amountCents < 50) {
                ModelState.AddModelError("", "Minimum order amount for payment is $0.50.");
                return View(order);
            }

            var scheme = Request.Scheme;
            var host = Request.Host.Value;
            var successUrl = $"{scheme}://{host}/Order/Success?session_id={{CHECKOUT_SESSION_ID}}";
            var cancelUrl = $"{scheme}://{host}/Order/Cancel";

            try {
                var checkoutUrl = await _stripePayment.CreateCheckoutSessionAsync(
                    amountTotalCents: amountCents,
                    orderDescription: $"Sports Store order – {order.Name}",
                    successUrl,
                    cancelUrl,
                    metadata: new Dictionary<string, string> {
                        ["CartId"] = _cart.CartId ?? "",
                        ["CustomerName"] = order.Name ?? "",
                    },
                    cancellationToken);

                HttpContext.Session.SetJson("PendingOrder", pending);
                return Redirect(checkoutUrl);
            }
            catch (InvalidOperationException ex) {
                _logger.LogWarning(ex, "Stripe not configured or error creating session. CustomerName: {CustomerName}", order.Name);
                ModelState.AddModelError("", "Payment is not configured. Please set Stripe keys (see README).");
                return View(order);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Stripe checkout session failed. CustomerName: {CustomerName}, CartId: {CartId}", order.Name, _cart.CartId);
                ModelState.AddModelError("", "Unable to start payment. Please try again.");
                return View(order);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Success(string? session_id, CancellationToken cancellationToken) {
            if (string.IsNullOrWhiteSpace(session_id)) {
                _logger.LogWarning("Order Success called without session_id");
                HttpContext.Session.Remove("PendingOrder");
                return View("PaymentError");
            }

            StripePaymentResult paymentResult;
            try {
                paymentResult = await _stripePayment.GetSessionPaymentStatusAsync(session_id, cancellationToken);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to retrieve Stripe session. SessionId: {SessionId}", session_id);
                HttpContext.Session.Remove("PendingOrder");
                return View("PaymentError");
            }

            if (!paymentResult.IsPaid) {
                _logger.LogWarning("Order Success called but payment not completed. SessionId: {SessionId}", session_id);
                HttpContext.Session.Remove("PendingOrder");
                return View("PaymentError");
            }

            var pending = HttpContext.Session.GetJson<PendingOrderData>("PendingOrder");
            if (pending == null || pending.Lines.Count == 0) {
                _logger.LogWarning("PendingOrder missing from session after successful payment. SessionId: {SessionId}", session_id);
                return View("PaymentError");
            }

            var order = new Order {
                Name = pending.Name,
                Line1 = pending.Line1,
                Line2 = pending.Line2,
                Line3 = pending.Line3,
                City = pending.City,
                State = pending.State,
                Zip = pending.Zip,
                Country = pending.Country,
                GiftWrap = pending.GiftWrap,
                StripeSessionId = paymentResult.SessionId,
                StripePaymentIntentId = paymentResult.PaymentIntentId,
                Lines = new List<CartLine>(),
            };

            foreach (var line in pending.Lines) {
                var product = _storeRepository.Products.FirstOrDefault(p => p.ProductID == line.ProductID);
                if (product == null) {
                    _logger.LogWarning("Product not found for PendingOrder line. ProductId: {ProductId}", line.ProductID);
                    continue;
                }
                order.Lines.Add(new CartLine { Product = product, Quantity = line.Quantity });
            }

            if (order.Lines.Count == 0) {
                _logger.LogError("No valid order lines after rebuilding from PendingOrder. SessionId: {SessionId}", session_id);
                HttpContext.Session.Remove("PendingOrder");
                return View("PaymentError");
            }

            try {
                _repository.SaveOrder(order);
                _logger.LogInformation(
                    "Order created after successful payment. OrderId: {OrderId}, CustomerName: {CustomerName}, StripeSessionId: {StripeSessionId}, Username: {Username}",
                    order.OrderID,
                    order.Name,
                    paymentResult.SessionId,
                    User?.Identity?.Name ?? "Anonymous");
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error saving order after payment. SessionId: {SessionId}, CustomerName: {CustomerName}", session_id, order.Name);
                ModelState.AddModelError("", "Payment succeeded but we could not save your order. Please contact support with session: " + session_id);
                return View("PaymentError");
            }

            HttpContext.Session.Remove("PendingOrder");
            _cart.Clear();
            return RedirectToPage("/Completed", new { orderId = order.OrderID });
        }

        [HttpGet]
        public IActionResult Cancel() {
            _logger.LogInformation(
                "Payment cancelled by user. CartId: {CartId}, Username: {Username}",
                _cart.CartId,
                User?.Identity?.Name ?? "Anonymous");
            HttpContext.Session.Remove("PendingOrder");
            return View("PaymentCancelled");
        }
    }
}
