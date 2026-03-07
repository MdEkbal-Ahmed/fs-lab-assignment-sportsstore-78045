using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportsStore.Infrastructure;
using SportsStore.Models;

namespace SportsStore.Pages {

    public class CartModel : PageModel {
        private readonly IStoreRepository repository;
        private readonly ILogger<CartModel> logger;

        public CartModel(IStoreRepository repo, Cart cartService, ILogger<CartModel> logger) {
            repository = repo;
            Cart = cartService;
            this.logger = logger;
        }

        public Cart Cart { get; set; }
        public string ReturnUrl { get; set; } = "/";

        public void OnGet(string returnUrl) {
            ReturnUrl = returnUrl ?? "/";
        }

        public IActionResult OnPost(long productId, string returnUrl) {
            Product? product = repository.Products
                .FirstOrDefault(p => p.ProductID == productId);
            if (product != null) {
                Cart.AddItem(product, 1);
                logger.LogInformation(
                    "Cart item added. CartId: {CartId}, ProductId: {ProductId}, ProductName: {ProductName}, Username: {Username}",
                    Cart.CartId,
                    product.ProductID,
                    product.Name,
                    User?.Identity?.Name ?? "Anonymous");
            }
            return RedirectToPage(new { returnUrl = returnUrl });
        }

        public IActionResult OnPostRemove(long productId, string returnUrl) {
            var line = Cart.Lines.First(cl => cl.Product.ProductID == productId);
            Cart.RemoveLine(line.Product);
            logger.LogInformation(
                "Cart item removed. CartId: {CartId}, ProductId: {ProductId}, ProductName: {ProductName}, Username: {Username}",
                Cart.CartId,
                line.Product.ProductID,
                line.Product.Name,
                User?.Identity?.Name ?? "Anonymous");
            return RedirectToPage(new { returnUrl = returnUrl });
        }
    }
}
