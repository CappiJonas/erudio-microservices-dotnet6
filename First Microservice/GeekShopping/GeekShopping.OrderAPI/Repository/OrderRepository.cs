using GeekShopping.OrderAPI.Model;
using GeekShopping.OrderAPI.Model.Context;
using Microsoft.EntityFrameworkCore;

namespace GeekShopping.OrderAPI.Repository
{
    public class OrderRepository : IOrderRepository
    {
        private readonly DbContextOptions<SqlServerContext> _context;

        public OrderRepository(DbContextOptions<SqlServerContext> context)
        {
            _context = context;
        }

        public async Task<bool> AddOrder(OrderHeader orderHeader)
        {
            if (orderHeader == null)
                return false;

            await using var _db = new SqlServerContext(_context);
            _db.OrderHeaders.Add(orderHeader);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task UpdateOrderPaymentStatus(long orderHeaderId, bool status)
        {
            await using var _db = new SqlServerContext(_context);
            var header = await _db.OrderHeaders.FirstOrDefaultAsync(h => h.Id == orderHeaderId);

            if (header != null)
            {
                header.PaymentStatus = status;
                await _db.SaveChangesAsync();
            }
        }
    }
}
