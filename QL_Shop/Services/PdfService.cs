using QL_Shop.Models;
using System.Text;
using System.IO;

namespace QL_Shop.Services
{
    public class PdfService
    {
        public PdfService()
        {
            // No dependencies needed for temporary implementation
        }

        public byte[] GenerateInvoicePdf(Order order)
        {
            var htmlContent = GenerateInvoiceHtml(order);
            
            // Temporary implementation - return HTML content as bytes
            return Encoding.UTF8.GetBytes(htmlContent);
        }

        public byte[] GenerateReportPdf(DateTime startDate, DateTime endDate, IEnumerable<Order> orders, decimal totalRevenue)
        {
            var htmlContent = GenerateReportHtml(startDate, endDate, orders, totalRevenue);
            
            // Temporary implementation - return HTML content as bytes
            return Encoding.UTF8.GetBytes(htmlContent);
        }

        private string GenerateInvoiceHtml(Order order)
        {
            var sb = new StringBuilder();
            
            sb.Append(@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
                <title>Hóa đơn</title>
                <style>
                    body { font-family: Arial, sans-serif; margin: 0; padding: 20px; }
                    .invoice-header { text-align: center; margin-bottom: 30px; }
                    .invoice-header h1 { color: #333; margin-bottom: 5px; }
                    .invoice-info { margin-bottom: 20px; }
                    .invoice-info div { margin-bottom: 5px; }
                    table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }
                    th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
                    th { background-color: #f2f2f2; }
                    .total-row { font-weight: bold; }
                    .footer { margin-top: 30px; text-align: center; font-size: 12px; color: #777; }
                </style>
            </head>
            <body>
                <div class='invoice-header'>
                    <h1>HÓA ĐƠN BÁN HÀNG</h1>
                    <p>SHOP GIÀY QL</p>
                </div>
                
                <div class='invoice-info'>
                    <div><strong>Mã đơn hàng:</strong> " + order.OrderNumber + @"</div>
                    <div><strong>Ngày đặt hàng:</strong> " + order.OrderDate.ToString("dd/MM/yyyy HH:mm") + @"</div>
                    <div><strong>Tên khách hàng:</strong> " + order.CustomerName + @"</div>
                    <div><strong>Địa chỉ:</strong> " + order.Address + @"</div>
                    <div><strong>Số điện thoại:</strong> " + order.Phone + @"</div>
                    <div><strong>Email:</strong> " + order.Email + @"</div>
                </div>
                
                <table>
                    <thead>
                        <tr>
                            <th>STT</th>
                            <th>Sản phẩm</th>
                            <th>Đơn giá</th>
                            <th>Số lượng</th>
                            <th>Thành tiền</th>
                        </tr>
                    </thead>
                    <tbody>");

            int index = 1;
            foreach (var item in order.OrderDetails)
            {
                sb.Append(@"
                        <tr>
                            <td>" + index + @"</td>
                            <td>" + item.Product.Name + @"</td>
                            <td>" + item.UnitPrice.ToString("#,##0") + @" VNĐ</td>
                            <td>" + item.Quantity + @"</td>
                            <td>" + item.Subtotal.ToString("#,##0") + @" VNĐ</td>
                        </tr>");
                index++;
            }

            sb.Append(@"
                        <tr class='total-row'>
                            <td colspan='4' style='text-align: right;'>Tổng cộng:</td>
                            <td>" + order.TotalAmount.ToString("#,##0") + @" VNĐ</td>
                        </tr>
                    </tbody>
                </table>
                
                <div class='footer'>
                    <p>Cảm ơn quý khách đã mua hàng tại Shop Giày QL!</p>
                </div>
            </body>
            </html>");

            return sb.ToString();
        }

        private string GenerateReportHtml(DateTime startDate, DateTime endDate, IEnumerable<Order> orders, decimal totalRevenue)
        {
            var sb = new StringBuilder();
            
            sb.Append(@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
                <title>Báo cáo doanh thu</title>
                <style>
                    body { font-family: Arial, sans-serif; margin: 0; padding: 20px; }
                    .report-header { text-align: center; margin-bottom: 30px; }
                    .report-header h1 { color: #333; margin-bottom: 5px; }
                    .report-info { margin-bottom: 20px; }
                    .report-info div { margin-bottom: 5px; }
                    table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }
                    th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
                    th { background-color: #f2f2f2; }
                    .total-row { font-weight: bold; }
                    .footer { margin-top: 30px; text-align: center; font-size: 12px; color: #777; }
                </style>
            </head>
            <body>
                <div class='report-header'>
                    <h1>BÁO CÁO DOANH THU</h1>
                    <p>SHOP GIÀY QL</p>
                </div>
                
                <div class='report-info'>
                    <div><strong>Từ ngày:</strong> " + startDate.ToString("dd/MM/yyyy") + @"</div>
                    <div><strong>Đến ngày:</strong> " + endDate.ToString("dd/MM/yyyy") + @"</div>
                    <div><strong>Ngày xuất báo cáo:</strong> " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + @"</div>
                </div>
                
                <table>
                    <thead>
                        <tr>
                            <th>STT</th>
                            <th>Mã đơn hàng</th>
                            <th>Ngày đặt hàng</th>
                            <th>Khách hàng</th>
                            <th>Trạng thái</th>
                            <th>Tổng tiền</th>
                        </tr>
                    </thead>
                    <tbody>");

            int index = 1;
            foreach (var order in orders)
            {
                sb.Append(@"
                        <tr>
                            <td>" + index + @"</td>
                            <td>" + order.OrderNumber + @"</td>
                            <td>" + order.OrderDate.ToString("dd/MM/yyyy HH:mm") + @"</td>
                            <td>" + order.CustomerName + @"</td>
                            <td>" + order.Status + @"</td>
                            <td>" + order.TotalAmount.ToString("#,##0") + @" VNĐ</td>
                        </tr>");
                index++;
            }

            sb.Append(@"
                        <tr class='total-row'>
                            <td colspan='5' style='text-align: right;'>Tổng doanh thu:</td>
                            <td>" + totalRevenue.ToString("#,##0") + @" VNĐ</td>
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
}
