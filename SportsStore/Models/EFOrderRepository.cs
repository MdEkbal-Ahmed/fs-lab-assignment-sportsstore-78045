using Microsoft.EntityFrameworkCore;

namespace SportsStore.Models {

    public class EFOrderRepository : IOrderRepository {
        private readonly StoreDbContext _context;
        private readonly ILogger<EFOrderRepository> _logger;

        public EFOrderRepository(StoreDbContext ctx, ILogger<EFOrderRepository> logger) {
            _context = ctx;
            _logger = logger;
        }

        public IQueryable<Order> Orders => _context.Orders
                            .Include(o => o.Lines)
                            .ThenInclude(l => l.Product);

        public void SaveOrder(Order order) {
            _context.AttachRange(order.Lines.Select(l => l.Product));
            if (order.OrderID == 0) {
                _context.Orders.Add(order);
            }
            _context.SaveChanges();
            _logger.LogInformation("Order saved. OrderId: {OrderId}, LineCount: {LineCount}",
                order.OrderID, order.Lines.Count);
        }
    }
}
