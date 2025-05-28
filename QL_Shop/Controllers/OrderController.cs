using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QL_Shop.Data;
using QL_Shop.Models;
using QL_Shop.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace QL_Shop.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly PdfService _pdfService;

        public OrderController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            PdfService pdfService)
        {
            _context = context;
            _userManager = userManager;
            _pdfService = pdfService;
        }

        // GET: Order
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        // GET: Order/MyOrders
        public async Task<IActionResult> MyOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var orders = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // GET: Order/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            // Check if the user is authorized to view this order
            if (!User.IsInRole("Admin") && order.UserId != _userManager.GetUserId(User))
            {
                return Forbid();
            }

            return View(order);
        }

        // GET: Order/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Order/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("CustomerName,Address,Phone,Email,Notes")] Order order)
        {
            if (ModelState.IsValid)
            {
                // Generate order number
                order.OrderNumber = "ORD" + DateTime.Now.ToString("yyyyMMddHHmmss");
                order.OrderDate = DateTime.Now;
                order.Status = OrderStatus.Pending;
                order.TotalAmount = 0; // Will be updated when adding order details

                _context.Add(order);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Edit), new { id = order.Id });
            }
            return View(order);
        }

        // GET: Order/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            ViewData["Products"] = await _context.Products.ToListAsync();
            return View(order);
        }

        // POST: Order/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,OrderNumber,OrderDate,Status,TotalAmount,CustomerName,Address,Phone,Email,Notes,UserId")] Order order)
        {
            if (id != order.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(order);
        }

        // POST: Order/AddOrderDetail
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddOrderDetail(int orderId, int productId, int quantity)
        {
            if (quantity <= 0)
            {
                TempData["Error"] = "Số lượng phải lớn hơn 0";
                return RedirectToAction(nameof(Edit), new { id = orderId });
            }

            var order = await _context.Orders.FindAsync(orderId);
            var product = await _context.Products.FindAsync(productId);

            if (order == null || product == null)
            {
                return NotFound();
            }

            if (product.Stock < quantity)
            {
                TempData["Error"] = "Số lượng sản phẩm trong kho không đủ";
                return RedirectToAction(nameof(Edit), new { id = orderId });
            }

            // Check if product already exists in order
            var existingDetail = await _context.OrderDetails
                .FirstOrDefaultAsync(od => od.OrderId == orderId && od.ProductId == productId);

            if (existingDetail != null)
            {
                // Update existing order detail
                existingDetail.Quantity += quantity;
                _context.Update(existingDetail);
            }
            else
            {
                // Create new order detail
                var orderDetail = new OrderDetail
                {
                    OrderId = orderId,
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPrice = product.Price
                };

                _context.Add(orderDetail);
            }

            // Update product stock
            product.Stock -= quantity;
            _context.Update(product);

            // Update order total
            order.TotalAmount += product.Price * quantity;
            _context.Update(order);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Edit), new { id = orderId });
        }

        // POST: Order/RemoveOrderDetail
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveOrderDetail(int orderDetailId)
        {
            var orderDetail = await _context.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                .FirstOrDefaultAsync(od => od.Id == orderDetailId);

            if (orderDetail == null)
            {
                return NotFound();
            }

            var orderId = orderDetail.OrderId;

            // Update product stock
            orderDetail.Product.Stock += orderDetail.Quantity;
            _context.Update(orderDetail.Product);

            // Update order total
            orderDetail.Order.TotalAmount -= orderDetail.UnitPrice * orderDetail.Quantity;
            _context.Update(orderDetail.Order);

            // Remove order detail
            _context.OrderDetails.Remove(orderDetail);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Edit), new { id = orderId });
        }

        // POST: Order/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = status;
            _context.Update(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Order/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Order/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            // Return products to stock
            foreach (var detail in order.OrderDetails)
            {
                detail.Product.Stock += detail.Quantity;
                _context.Update(detail.Product);
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Order/Invoice/5
        public async Task<IActionResult> Invoice(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            // Check if the user is authorized to view this order
            if (!User.IsInRole("Admin") && order.UserId != _userManager.GetUserId(User))
            {
                return Forbid();
            }

            return View(order);
        }

        // GET: Order/DownloadInvoice/5
        public async Task<IActionResult> DownloadInvoice(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            // Check if the user is authorized to view this order
            if (!User.IsInRole("Admin") && order.UserId != _userManager.GetUserId(User))
            {
                return Forbid();
            }

            var pdfBytes = _pdfService.GenerateInvoicePdf(order);
            return File(pdfBytes, "application/pdf", $"HoaDon-{order.OrderNumber}.pdf");
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }
}
