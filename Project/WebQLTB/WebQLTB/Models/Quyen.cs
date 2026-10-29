using System.ComponentModel.DataAnnotations;

namespace WebQLTB.Models
{
    public class Quyen
    {
        [Key]
        public byte MaQuyen { get; set; }

        public string TenQuyen { get; set; } = null!;
        public string? MoTa { get; set; }
    }
}
