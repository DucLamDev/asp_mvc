using System.ComponentModel.DataAnnotations;

namespace QL_Shop.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        [Display(Name = "Tên danh mục")]
        [StringLength(50)]
        public string Name { get; set; }

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        public ICollection<Product>? Products { get; set; }
    }
}
