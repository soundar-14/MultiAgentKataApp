using MultiAgentKataApp.Models;

namespace MultiAgentKataApp.Services
{
    /// <summary>
    /// OrderService - Contains intentional bugs for multi-agent analysis demonstration.
    /// Each method demonstrates a specific category of bug that agents will detect and fix.
    /// </summary>
    public class OrderService : IOrderService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<OrderService> _logger;

        // Simulated in-memory store (represents DB)
        private static readonly List<Order> _orders = new()
        {
            new Order { Id = 1, Customer = new Customer { Id = 1, Name = "Alice", Email = "alice@example.com" }, Total = 150.00m, CreatedAt = DateTime.UtcNow.AddDays(-5), Status = "Completed" },
            new Order { Id = 2, Customer = null, Total = 75.50m, CreatedAt = DateTime.UtcNow.AddDays(-3), Status = "Pending" }, // BUG: Customer is null
            new Order { Id = 3, Customer = new Customer { Id = 3, Name = "Charlie", Email = "charlie@example.com" }, Total = 220.00m, CreatedAt = DateTime.UtcNow.AddDays(-1), Status = "Processing" },
        };

        private static readonly Dictionary<int, int> _inventory = new() { { 101, 50 }, { 102, 30 }, { 103, 0 } };

        public OrderService(IConfiguration configuration, ILogger<OrderService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // BUG SCENARIO 1: NullReferenceException
        // When orderId=2, Customer is null — accessing Customer.Name throws NullReferenceException
        // Stack trace: OrderService.GetOrder → BugController.GetOrder → Customer.Name
        public Order GetOrder(int orderId)
        {
            _logger.LogInformation("DB query: SELECT * FROM Orders WHERE Id = {OrderId}", orderId);
            var order = _orders.FirstOrDefault(o => o.Id == orderId);

            // BUG: Missing null check on 'order' — throws NullReferenceException if not found
            // ALSO BUG: Customer can be null — caller accesses Customer.Name directly
            return order!;
        }

        // BUG SCENARIO 2: Database connection pool exhaustion / timeout
        // Under high load, too many concurrent calls exhaust the DB connection pool
        // Results in: "The timeout period elapsed prior to obtaining a connection from the pool"
        public async Task<List<Order>> GetAllOrdersAsync()
        {
            _logger.LogInformation("DB query: SELECT * FROM Orders — no pagination, no connection timeout set");

            // BUG: Simulating delay without cancellation token or timeout
            // Real code would use SqlConnection without CommandTimeout or pool size config
            await Task.Delay(100); // Simulates DB call

            // BUG: Returns all records — no pagination (memory pressure under large datasets)
            return _orders;
        }

        // BUG SCENARIO 3: Missing configuration — throws InvalidOperationException
        // When PaymentGatewayUrl is not in appsettings, throws on .GetRequiredSection()
        public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
        {
            // BUG: Uses GetRequiredSection — throws InvalidOperationException if key missing
            var gatewayUrl = _configuration["PaymentGateway:Url"]
                ?? throw new InvalidOperationException("configuration key 'PaymentGateway:Url' is missing");

            _logger.LogInformation("Calling payment gateway at {GatewayUrl} for order {OrderId}", gatewayUrl, request.OrderId);

            // BUG: No retry logic, no circuit breaker — single point of failure
            // If gateway is down, throws HttpRequestException immediately with no fallback
            var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

            try
            {
                // Simulated external call (would fail in real env without gateway)
                await Task.Delay(50);
                return new PaymentResult { Success = true, TransactionId = Guid.NewGuid().ToString(), Message = "Payment processed" };
            }
            catch (TaskCanceledException)
            {
                _logger.LogError("Payment gateway timeout for order {OrderId}", request.OrderId);
                throw new HttpRequestException("Payment gateway request timed out", null, System.Net.HttpStatusCode.GatewayTimeout);
            }
        }

        // BUG SCENARIO 4: Missing input validation — ArgumentOutOfRangeException
        // Month values outside 1-12 cause DateTime constructor to throw
        public MonthlyReport GenerateMonthlyReport(int month)
        {
            // BUG: No validation — throws ArgumentOutOfRangeException for month < 1 or > 12
            var startDate = new DateTime(DateTime.UtcNow.Year, month, 1); // throws if month invalid
            var endDate = startDate.AddMonths(1).AddDays(-1);

            _logger.LogInformation("Generating report for {StartDate} to {EndDate}", startDate, endDate);

            var monthOrders = _orders.Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate).ToList();

            return new MonthlyReport
            {
                Month = month,
                Year = DateTime.UtcNow.Year,
                TotalOrders = monthOrders.Count,
                TotalRevenue = monthOrders.Sum(o => o.Total),
                TopProducts = new List<string> { "ProductA", "ProductB" }
            };
        }

        // BUG SCENARIO 5: Race condition — no concurrency control
        // Two simultaneous requests can both read quantity=5, both subtract 3, leaving 2 instead of -1
        // Should use optimistic concurrency (row version / ETag) or pessimistic locking
        public async Task<InventoryUpdateResult> UpdateInventoryAsync(InventoryUpdateRequest request)
        {
            _logger.LogInformation("Updating inventory: Product={ProductId}, Op={Op}, Qty={Qty}",
                request.ProductId, request.Operation, request.Quantity);

            if (!_inventory.TryGetValue(request.ProductId, out var currentQty))
                throw new KeyNotFoundException($"Product {request.ProductId} not found in inventory");

            // BUG: No locking — race condition when concurrent requests modify same product
            await Task.Delay(10); // Simulates DB read latency — race window

            var newQty = request.Operation == "ADD"
                ? currentQty + request.Quantity
                : currentQty - request.Quantity;

            // BUG: No negative inventory check
            _inventory[request.ProductId] = newQty;

            return new InventoryUpdateResult { ProductId = request.ProductId, NewQuantity = newQty, Success = true };
        }
    }
}
