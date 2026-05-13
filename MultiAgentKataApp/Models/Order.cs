namespace MultiAgentKataApp.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class Order
    {
        public int Id { get; set; }
        public Customer? Customer { get; set; }  // Nullable — source of NullReferenceException bug
        public List<OrderItem> Items { get; set; } = new();
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = "Pending";
    }

    public class OrderItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class PaymentRequest
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public string CardToken { get; set; } = string.Empty;
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class InventoryUpdateRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string Operation { get; set; } = "ADD"; // ADD or SUBTRACT
    }

    public class InventoryUpdateResult
    {
        public int ProductId { get; set; }
        public int NewQuantity { get; set; }
        public bool Success { get; set; }
    }

    public class MonthlyReport
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<string> TopProducts { get; set; } = new();
    }
}
