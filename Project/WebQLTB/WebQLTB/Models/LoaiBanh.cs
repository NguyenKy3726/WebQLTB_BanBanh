using System.ComponentModel.DataAnnotations;

namespace WebQLTB.Models
{
    public class LoaiBanh
    {
        [Key]
        public byte MaLoaiBanh { get; set; }

        public string TenLoaiBanh { get; set; } = null!;
        public string? MoTa { get; set; }
    }
}
