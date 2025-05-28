using Microsoft.AspNetCore.Authorization;
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
    [Authorize(Roles = "Admin")]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PdfService _pdfService;

        public ReportController(ApplicationDbContext context, PdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        // GET: Report
        public IActionResult Index()
        {
            return View();
        }

        // GET: Report/Sales
        public async Task<IActionResult> Sales(DateTime? startDate, DateTime? endDate)
        {
            // Default to current month if dates not provided
            if (!startDate.HasValue)
                startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            
            if (!endDate.HasValue)
                endDate = DateTime.Now.Date.AddDays(1).AddSeconds(-1);

            ViewData["StartDate"] = startDate.Value.ToString("yyyy-MM-dd");
            ViewData["EndDate"] = endDate.Value.ToString("yyyy-MM-dd");

            var orders = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate && o.Status != OrderStatus.Cancelled)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewData["TotalRevenue"] = orders.Sum(o => o.TotalAmount);
            ViewData["OrderCount"] = orders.Count;
            ViewData["AverageOrderValue"] = orders.Count > 0 ? orders.Average(o => o.TotalAmount) : 0;

            return View(orders);
        }

        // GET: Report/Products
        public async Task<IActionResult> Products(DateTime? startDate, DateTime? endDate)
        {
            // Default to current month if dates not provided
            if (!startDate.HasValue)
                startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            
            if (!endDate.HasValue)
                endDate = DateTime.Now.Date.AddDays(1).AddSeconds(-1);

            ViewData["StartDate"] = startDate.Value.ToString("yyyy-MM-dd");
            ViewData["EndDate"] = endDate.Value.ToString("yyyy-MM-dd");

            var productSales = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Product)
                .Where(od => od.Order.OrderDate >= startDate && od.Order.OrderDate <= endDate && od.Order.Status != OrderStatus.Cancelled)
                .GroupBy(od => new { od.ProductId, od.Product.Name })
                .Select(g => new ProductSalesViewModel
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Name,
                    QuantitySold = g.Sum(od => od.Quantity),
                    Revenue = g.Sum(od => od.Quantity * od.UnitPrice)
                })
                .OrderByDescending(ps => ps.Revenue)
                .ToListAsync();

            return View(productSales);
        }

        // GET: Report/Categories
        public async Task<IActionResult> Categories(DateTime? startDate, DateTime? endDate)
        {
            // Default to current month if dates not provided
            if (!startDate.HasValue)
                startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            
            if (!endDate.HasValue)
                endDate = DateTime.Now.Date.AddDays(1).AddSeconds(-1);

            ViewData["StartDate"] = startDate.Value.ToString("yyyy-MM-dd");
            ViewData["EndDate"] = endDate.Value.ToString("yyyy-MM-dd");

            var categorySales = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Product)
                .ThenInclude(p => p.Category)
                .Where(od => od.Order.OrderDate >= startDate && od.Order.OrderDate <= endDate && od.Order.Status != OrderStatus.Cancelled)
                .GroupBy(od => new { od.Product.CategoryId, od.Product.Category.Name })
                .Select(g => new CategorySalesViewModel
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.Name,
                    QuantitySold = g.Sum(od => od.Quantity),
                    Revenue = g.Sum(od => od.Quantity * od.UnitPrice)
                })
                .OrderByDescending(cs => cs.Revenue)
                .ToListAsync();

            return View(categorySales);
        }

        // GET: Report/Inventory
        public async Task<IActionResult> Inventory()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .OrderBy(p => p.Category.Name)
                .ThenBy(p => p.Name)
                .ToListAsync();

            return View(products);
        }

        // GET: Report/DownloadSalesReport
        public async Task<IActionResult> DownloadSalesReport(DateTime startDate, DateTime endDate)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate && o.Status != OrderStatus.Cancelled)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            decimal totalRevenue = orders.Sum(o => o.TotalAmount);

            var pdfBytes = _pdfService.GenerateReportPdf(startDate, endDate, orders, totalRevenue);
            return File(pdfBytes, "application/pdf", $"BaoCaoDoanhThu-{startDate:yyyyMMdd}-{endDate:yyyyMMdd}.pdf");
        }

        // GET: Report/DownloadInventoryReport
        public async Task<IActionResult> DownloadInventoryReport()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .OrderBy(p => p.Category.Name)
                .ThenBy(p => p.Name)
                .ToListAsync();

            // Generate HTML for inventory report
            var htmlContent = GenerateInventoryReportHtml(products);
            
            var globalSettings = new DinkToPdf.GlobalSettings
            {
                ColorMode = DinkToPdf.ColorMode.Color,
                Orientation = DinkToPdf.Orientation.Portrait,
                PaperSize = DinkToPdf.PaperKind.A4,
                Margins = new DinkToPdf.MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 },
                DocumentTitle = "Báo cáo tồn kho"
            };

            var objectSettings = new DinkToPdf.ObjectSettings
            {
                PagesCount = true,
                HtmlContent = htmlContent,
                WebSettings = { DefaultEncoding = "utf-8" }
            };

            var document = new DinkToPdf.HtmlToPdfDocument()
            {
                GlobalSettings = globalSettings,
                Objects = { objectSettings }
            };

            var converter = new DinkToPdf.SynchronizedConverter(new DinkToPdf.PdfTools());
            var pdfBytes = converter.Convert(document);

            return File(pdfBytes, "application/pdf", $"BaoCaoTonKho-{DateTime.Now:yyyyMMdd}.pdf");
        }

        private string GenerateInventoryReportHtml(IEnumerable<Product> products)
        {
            var sb = new System.Text.StringBuilder();
            
            sb.Append(@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
                <title>Báo cáo tồn kho</title>
                <style>
                    body { font-family: Arial, sans-serif; margin: 0; padding: 20px; }
                    .report-header { text-align: center; margin-bottom: 30px; }
                    .report-header h1 { color: #333; margin-bottom: 5px; }
                    .report-info { margin-bottom: 20px; }
                    .report-info div { margin-bottom: 5px; }
                    table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }
                    th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
                    th { background-color: #f2f2f2; }
                    .category-header { background-color: #e6e6e6; font-weight: bold; }
                    .footer { margin-top: 30px; text-align: center; font-size: 12px; color: #777; }
                </style>
            </head>
            <body>
                <div class='report-header'>
                    <h1>BÁO CÁO TỒN KHO</h1>
                    <p>SHOP GIÀY QL</p>
                </div>
                
                <div class='report-info'>
                    <div><strong>Ngày xuất báo cáo:</strong> " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + @"</div>
                </div>
                
                <table>
                    <thead>
                        <tr>
                            <th>STT</th>
                            <th>Mã SP</th>
                            <th>Tên sản phẩm</th>
                            <th>Danh mục</th>
                            <th>Kích cỡ</th>
                            <th>Màu sắc</th>
                            <th>Giá</th>
                            <th>Tồn kho</th>
                            <th>Giá trị tồn kho</th>
                        </tr>
                    </thead>
                    <tbody>");

            int index = 1;
            string currentCategory = "";
            decimal totalInventoryValue = 0;

            foreach (var product in products)
            {
                decimal inventoryValue = product.Price * product.Stock;
                totalInventoryValue += inventoryValue;

                // Add category header if category changes
                if (product.Category.Name != currentCategory)
                {
                    currentCategory = product.Category.Name;
                    sb.Append(@"
                        <tr class='category-header'>
                            <td colspan='9'>" + currentCategory + @"</td>
                        </tr>");
                }

                sb.Append(@"
                        <tr>
                            <td>" + index + @"</td>
                            <td>" + product.Id + @"</td>
                            <td>" + product.Name + @"</td>
                            <td>" + product.Category.Name + @"</td>
                            <td>" + product.Size + @"</td>
                            <td>" + product.Color + @"</td>
                            <td>" + product.Price.ToString("#,##0") + @" VNĐ</td>
                            <td>" + product.Stock + @"</td>
                            <td>" + inventoryValue.ToString("#,##0") + @" VNĐ</td>
                        </tr>");
                index++;
            }

            sb.Append(@"
                        <tr>
                            <td colspan='8' style='text-align: right; font-weight: bold;'>Tổng giá trị tồn kho:</td>
                            <td style='font-weight: bold;'>" + totalInventoryValue.ToString("#,##0") + @" VNĐ</td>
                        </tr>
                    </tbody>
                </table>
                
                <div class='footer'>
                    <p>Báo cáo được tạo tự động từ hệ thống Shop Giày QL</p>
                </div>
            </body>
            </html>");

            return sb.ToString();
        }
    }

    public class ProductSalesViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class CategorySalesViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }
}
