using MultiAgentKataApp.Models;

namespace MultiAgentKataApp.Services
{
    public interface IOrderService
    {
        Order GetOrder(int orderId);
        Task<List<Order>> GetAllOrdersAsync();
        Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
        MonthlyReport GenerateMonthlyReport(int month);
        Task<InventoryUpdateResult> UpdateInventoryAsync(InventoryUpdateRequest request);
    }
}
