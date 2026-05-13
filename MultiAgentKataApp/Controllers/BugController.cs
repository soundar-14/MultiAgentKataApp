using Microsoft.AspNetCore.Mvc;
using MultiAgentKataApp.Models;
using MultiAgentKataApp.Services;

namespace MultiAgentKataApp.Controllers
{
    /// <summary>
    /// BugController — Demonstrates 5 real-world bug scenarios for multi-agent analysis.
    ///
    /// Bug Scenarios:
    ///   GET  /api/bug/order/2       → BUG 1: NullReferenceException (Customer is null)
    ///   GET  /api/bug/orders        → BUG 2: DB connection pool exhaustion (no pagination/timeout)
    ///   POST /api/bug/payment       → BUG 3: Missing configuration (PaymentGateway:Url not set)
    ///   GET  /api/bug/report/13     → BUG 4: Invalid input — ArgumentOutOfRangeException
    ///   POST /api/bug/inventory/update → BUG 5: Race condition (no concurrency control)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class BugController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<BugController> _logger;

        public BugController(IOrderService orderService, ILogger<BugController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        /// <summary>
        /// BUG 1: NullReferenceException — Try orderId=2 (Customer is null in DB)
        /// Stack trace: BugController.GetOrder → OrderService.GetOrder → Customer.Name
        /// Root Cause: Missing null check on Customer object
        /// Fix: Add null conditional operator (order?.Customer?.Name ?? "Unknown")
        /// </summary>
        [HttpGet("order/{orderId}")]
        public IActionResult GetOrder(int orderId)
        {
            _logger.LogInformation("Fetching order {OrderId}", orderId);
            try
            {
                var order = _orderService.GetOrder(orderId);
                // BUG: No null check — NullReferenceException when order.Customer is null
                return Ok(new
                {
                    OrderId = order.Id,
                    Customer = order.Customer!.Name,  // throws NullReferenceException for orderId=2
                    Total = order.Total,
                    Status = order.Status
                });
            }
            catch (NullReferenceException ex)
            {
                _logger.LogError(ex, "NullReferenceException in GetOrder for orderId={OrderId}. Stack: {Stack}", orderId, ex.StackTrace);
                return StatusCode(500, new { error = "NullReferenceException", message = ex.Message, hint = "Customer object is null for this order" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching order {OrderId}", orderId);
                return StatusCode(500, new { error = ex.GetType().Name, message = ex.Message });
            }
        }

        /// <summary>
        /// BUG 2: DB connection pool exhaustion — intermittent 500 errors under load
        /// Error: "The timeout period elapsed prior to obtaining a connection from the pool"
        /// Root Cause: No connection pool configuration, no query timeout, no pagination
        /// Fix: Set MaxPoolSize in connection string, add CommandTimeout, add pagination
        /// </summary>
        [HttpGet("orders")]
        public async Task<IActionResult> GetAllOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            _logger.LogInformation("Fetching all orders — page={Page}, size={PageSize} (BUG: pagination not implemented in service)", page, pageSize);
            try
            {
                var orders = await _orderService.GetAllOrdersAsync();
                return Ok(new { Count = orders.Count, Data = orders });
            }
            catch (TimeoutException ex)
            {
                _logger.LogError("DB timeout: {Message}. Check connection pool settings in appsettings.json", ex.Message);
                return StatusCode(500, new { error = "DatabaseTimeout", message = "Connection pool may be exhausted. Check MaxPoolSize configuration." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching orders: {ErrorType}", ex.GetType().Name);
                return StatusCode(500, new { error = ex.GetType().Name, message = ex.Message });
            }
        }

        /// <summary>
        /// BUG 3: Missing configuration throws InvalidOperationException
        /// Error: "configuration key 'PaymentGateway:Url' is missing"
        /// Root Cause: PaymentGateway:Url not in appsettings.json
        /// Fix: Add PaymentGateway section to appsettings.json + add validation at startup
        /// </summary>
        [HttpPost("payment")]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
        {
            _logger.LogInformation("Processing payment for order {OrderId}, amount={Amount}", request.OrderId, request.Amount);
            try
            {
                var result = await _orderService.ProcessPaymentAsync(request);
                return Ok(new { Success = result.Success, TransactionId = result.TransactionId });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("configuration"))
            {
                _logger.LogError("Configuration missing: {Message}", ex.Message);
                return StatusCode(500, new { error = "ConfigurationError", message = ex.Message, hint = "Add PaymentGateway:Url to appsettings.json" });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("Payment gateway error: StatusCode={StatusCode}, Message={Message}", ex.StatusCode, ex.Message);
                return StatusCode(503, new { error = "PaymentGatewayUnavailable", message = ex.Message });
            }
        }

        /// <summary>
        /// BUG 4: ArgumentOutOfRangeException — Try month=13 or month=0
        /// Error: "Year, Month, and Day parameters describe an un-representable DateTime"
        /// Root Cause: No input validation before passing to DateTime constructor
        /// Fix: Validate month range (1-12) before processing
        /// </summary>
        [HttpGet("report/{month}")]
        public IActionResult GetMonthlyReport(int month)
        {
            _logger.LogInformation("Generating monthly report for month={Month}", month);
            try
            {
                var report = _orderService.GenerateMonthlyReport(month);
                return Ok(report);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                _logger.LogWarning("Invalid month {Month}: {Message}", month, ex.Message);
                return BadRequest(new { error = "InvalidParameter", message = $"Month must be 1-12. Received: {month}", stackTrace = ex.StackTrace });
            }
        }

        /// <summary>
        /// BUG 5: Race condition in inventory update — concurrent requests cause data inconsistency
        /// Error: Inventory goes negative or shows wrong quantity under concurrent load
        /// Root Cause: No optimistic/pessimistic locking, no row versioning
        /// Fix: Use Interlocked operations, DB row version, or distributed lock (Redis)
        /// </summary>
        [HttpPost("inventory/update")]
        public async Task<IActionResult> UpdateInventory([FromBody] InventoryUpdateRequest request)
        {
            _logger.LogInformation("Inventory update: Product={ProductId}, Op={Op}, Qty={Qty}",
                request.ProductId, request.Operation, request.Quantity);
            try
            {
                var result = await _orderService.UpdateInventoryAsync(request);
                return Ok(new { Success = result.Success, ProductId = result.ProductId, NewQuantity = result.NewQuantity });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = "ProductNotFound", message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Inventory update failed for product {ProductId}", request.ProductId);
                return StatusCode(500, new { error = ex.GetType().Name, message = ex.Message });
            }
        }
    }
}
