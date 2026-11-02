using System.ComponentModel.DataAnnotations;

namespace WebQLTB.Models;

public class GhiNhanThanhToanViewModel : IValidatableObject
{
    [Required]
    [StringLength(11, MinimumLength = 11)]
    public string MaDonHang { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(PAYMENT|REFUND)$")]
    public string LoaiGiaoDich { get; set; } = "PAYMENT";

    [Range(typeof(decimal), "1", "999999999999.99", ErrorMessage = "Số tiền phải lớn hơn 0.")]
    public decimal SoTien { get; set; }

    [Required]
    [RegularExpression("^(CASH|BANK_TRANSFER)$", ErrorMessage = "Hãy chọn phương thức thanh toán hợp lệ.")]
    public string PhuongThuc { get; set; } = "CASH";

    public string? MaThanhToanGoc { get; set; }

    [StringLength(500)]
    public string? DuongDanChungTu { get; set; }

    [StringLength(500)]
    public string? GhiChu { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (decimal.Truncate(SoTien) != SoTien)
        {
            yield return new ValidationResult(
                "Số tiền phải là số nguyên đồng Việt Nam.",
                new[] { nameof(SoTien) });
        }
    }
}
