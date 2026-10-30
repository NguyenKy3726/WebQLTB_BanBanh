namespace WebQLTB.Models;

public class HomeViewModel
{
    public List<ProductCardViewModel> GoiY { get; set; } = new();
    public List<HomeCategoryViewModel> DanhMuc { get; set; } = new();
}

public class HomeCategoryViewModel
{
    public string Ten { get; set; } = "";
    public int SoMau { get; set; }
}
