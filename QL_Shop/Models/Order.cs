using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace QL_Shop.Models
{
    public enum OrderStatus
    {
        [Display(Name = "Chờ xác nhận")]
        Pending,
        
        [Display(Name = "Đã xác nhận")]
        Confirmed,
        
        [Display(Name = "Đang giao hàng")]
        Shipping,
        
        [Display(Name = "Đã giao hàng")]
        Delivered,
        
        [Display(Name = "Đã hủy")]
        Cancelled
    }

    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Mã đơn hàng")]
        public string OrderNumber { get; set; }

        [Required]
        [Display(Name = "Ngày đặt hàng")]
        public DateTime OrderDate { get; set; }

        [Required]
        [Display(Name = "Trạng thái")]
        public OrderStatus Status { get; set; }

        [Required]
        [Display(Name = "Tổng tiền")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [Display(Name = "Họ tên khách hàng")]
        public string CustomerName { get; set; }

        [Required]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; }

        [Required]
        [Display(Name = "Số điện thoại")]
        [Phone]
        public string Phone { get; set; }

        [Required]
        [Display(Name = "Email")]
        [EmailAddress]
        public string Email { get; set; }

        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }

        [Display(Name = "Người dùng")]
        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public IdentityUser? User { get; set; }

        public ICollection<OrderDetail>? OrderDetails { get; set; }
    }
}
