using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SportsStore.Controllers;
using SportsStore.Models;
using SportsStore.Services;
using Xunit;

namespace SportsStore.Tests {

    public class OrderControllerTests {

        private static OrderController CreateController(
            IOrderRepository orderRepo,
            IStoreRepository storeRepo,
            Cart cart,
            IStripePaymentService stripePayment,
            ILogger<OrderController> logger) {
            return new OrderController(orderRepo, storeRepo, cart, stripePayment, logger);
        }

        [Fact]
        public void Cannot_Checkout_Empty_Cart() {
            var mockOrder = new Mock<IOrderRepository>();
            var mockStore = new Mock<IStoreRepository>();
            var cart = new Cart();
            var mockStripe = new Mock<IStripePaymentService>();
            var mockLogger = new Mock<ILogger<OrderController>>();
            var target = CreateController(mockOrder.Object, mockStore.Object, cart, mockStripe.Object, mockLogger.Object);

            var result = target.Checkout(new Order(), CancellationToken.None).GetAwaiter().GetResult() as ViewResult;

            mockOrder.Verify(m => m.SaveOrder(It.IsAny<Order>()), Times.Never);
            mockStripe.Verify(m => m.CreateCheckoutSessionAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<CancellationToken>()), Times.Never);
            Assert.True(string.IsNullOrEmpty(result?.ViewName));
            Assert.False(result?.ViewData.ModelState.IsValid);
        }

        [Fact]
        public void Cannot_Checkout_Invalid_ShippingDetails() {
            var mockOrder = new Mock<IOrderRepository>();
            var mockStore = new Mock<IStoreRepository>();
            var cart = new Cart();
            cart.AddItem(new Product(), 1);
            var mockStripe = new Mock<IStripePaymentService>();
            var mockLogger = new Mock<ILogger<OrderController>>();
            var target = CreateController(mockOrder.Object, mockStore.Object, cart, mockStripe.Object, mockLogger.Object);
            target.ModelState.AddModelError("error", "error");

            var result = target.Checkout(new Order(), CancellationToken.None).GetAwaiter().GetResult() as ViewResult;

            mockOrder.Verify(m => m.SaveOrder(It.IsAny<Order>()), Times.Never);
            mockStripe.Verify(m => m.CreateCheckoutSessionAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<CancellationToken>()), Times.Never);
            Assert.True(string.IsNullOrEmpty(result?.ViewName));
            Assert.False(result?.ViewData.ModelState.IsValid);
        }

        [Fact]
        public async Task Can_Checkout_Redirects_To_Stripe() {
            var mockOrder = new Mock<IOrderRepository>();
            var mockStore = new Mock<IStoreRepository>();
            var cart = new Cart();
            cart.AddItem(new Product { ProductID = 1, Name = "P1", Price = 10m }, 1);
            var mockStripe = new Mock<IStripePaymentService>();
            mockStripe.Setup(m => m.CreateCheckoutSessionAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("https://checkout.stripe.com/c/pay/test");
            var mockLogger = new Mock<ILogger<OrderController>>();
            var target = CreateController(mockOrder.Object, mockStore.Object, cart, mockStripe.Object, mockLogger.Object);

            var order = new Order {
                Name = "Test",
                Line1 = "L1",
                City = "City",
                State = "State",
                Country = "Country",
            };
            var result = await target.Checkout(order, CancellationToken.None);

            var redirect = result as RedirectResult;
            Assert.NotNull(redirect);
            Assert.StartsWith("https://checkout.stripe.com", redirect.Url);
            mockStripe.Verify(m => m.CreateCheckoutSessionAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<CancellationToken>()), Times.Once);
            mockOrder.Verify(m => m.SaveOrder(It.IsAny<Order>()), Times.Never);
        }
    }
}
