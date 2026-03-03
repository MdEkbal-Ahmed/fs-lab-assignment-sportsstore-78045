using Microsoft.AspNetCore.Mvc;
using SportsStore.Models;

namespace SportsStore.Controllers {

    public class OrderController : Controller {
        private readonly IOrderRepository _repository;
        private readonly Cart _cart;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderRepository repoService, Cart cartService, ILogger<OrderController> logger) {
            _repository = repoService;
            _cart = cartService;
            _logger = logger;
        }

        public ViewResult Checkout() {
            _logger.LogInformation("Checkout page requested. CartItemCount: {CartItemCount}", _cart.Lines.Count);
            return View(new Order());
        }

        [HttpPost]
        public IActionResult Checkout(Order order) {
            if (_cart.Lines.Count == 0) {
                _logger.LogWarning("Checkout attempted with empty cart");
                ModelState.AddModelError("", "Sorry, your cart is empty!");
            }
            if (ModelState.IsValid) {
                decimal total = _cart.ComputeTotalValue();
                _logger.LogInformation(
                    "Checkout submitted. CustomerName: {CustomerName}, ItemCount: {ItemCount}, TotalAmount: {TotalAmount}",
                    order.Name, _cart.Lines.Count, total);

                order.Lines = _cart.Lines.ToArray();
                try {
                    _repository.SaveOrder(order);
                    _logger.LogInformation("Order created successfully. OrderId: {OrderId}, CustomerName: {CustomerName}",
                        order.OrderID, order.Name);
                    _cart.Clear();
                    return RedirectToPage("/Completed", new { orderId = order.OrderID });
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Error saving order. CustomerName: {CustomerName}", order.Name);
                    ModelState.AddModelError("", "An error occurred while saving your order. Please try again.");
                    return View(order);
                }
            }
            return View(order);
        }
    }
}
