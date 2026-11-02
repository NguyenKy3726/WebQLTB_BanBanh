using System.ComponentModel.DataAnnotations;

namespace WebQLTB.Models;

public class DonHangChuyenTrangThaiViewModel : IValidatableObject
{
    [Required]
    public string MaDonHang { get; set; } = string.Empty;

    [Required]
    public string TrangThaiHienTai { get; set; } = string.Empty;

    [Required]
    public string HanhDong { get; set; } = string.Empty;

    public DateTime? NgayGiaoDuKien { get; set; }
    [Range(typeof(decimal), "0", "9999999999.99", ErrorMessage = "Phí giao hàng vượt giới hạn cho phép.")]
    public decimal? PhiGiaoHang { get; set; }

    [Range(typeof(decimal), "0", "9999999999.99", ErrorMessage = "Tiền giảm giá vượt giới hạn cho phép.")]
    public decimal? TienGiamGia { get; set; }

    [Range(typeof(decimal), "0", "9999999999.99", ErrorMessage = "Tiền cọc vượt giới hạn cho phép.")]
    public decimal? TienCocYeuCau { get; set; }

    [StringLength(500)]
    public string? LyDoHuy { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var amounts = new (string Name, decimal? Value)[]
        {
            (nameof(PhiGiaoHang), PhiGiaoHang),
            (nameof(TienGiamGia), TienGiamGia),
            (nameof(TienCocYeuCau), TienCocYeuCau)
        };

        foreach (var amount in amounts)
        {
            if (amount.Value.HasValue && decimal.Truncate(amount.Value.Value) != amount.Value.Value)
            {
                yield return new ValidationResult(
                    "Số tiền phải là số nguyên đồng Việt Nam.",
                    new[] { amount.Name });
            }
        }
    }
}
