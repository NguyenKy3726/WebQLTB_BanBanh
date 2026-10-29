using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using WebQLTB.Models;

namespace WebQLTB.Data
{
    public class AppDbContext :DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }
        // 1. Thao tác trên bảng NhanVien
        public DbSet<NhanVien> NhanViens { get; set; }

        public List<NhanVien> NhanVien_DS()
        {
            return NhanViens
                .FromSqlRaw("EXEC dbo.NhanVien_DS")
                .ToList();
        }

        public NhanVien? NhanVien_CT(byte MaNV)
        {
            var p = new[]
            {
                new Microsoft.Data.SqlClient.SqlParameter("MaNV", MaNV)
            };
            return NhanViens.FromSqlRaw("EXEC NhanVien_CT @MaNV", p).ToList().FirstOrDefault();
        }

        public int NhanVien_Them(NhanVien nv, string? matKhauMoi)
        {
            using var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.NhanVien_Them";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@HoNV", nv.HoNV));
            cmd.Parameters.Add(new SqlParameter("@TenNV", nv.TenNV));
            cmd.Parameters.Add(new SqlParameter("@DienThoai", nv.DienThoai));
            cmd.Parameters.Add(new SqlParameter("@Email", nv.Email));
            cmd.Parameters.Add(new SqlParameter("@TenDN", nv.TenDN));
            cmd.Parameters.Add(new SqlParameter("@matKhauMoi", SqlDbType.NVarChar, 50)
            {
                Value = (object?)matKhauMoi ?? DBNull.Value
            });
            cmd.Parameters.Add(new SqlParameter(
                "@AnhNV", (object?)nv.AnhNV ?? DBNull.Value));

            Database.OpenConnection();
            try
            {
                return cmd.ExecuteNonQuery();
            }
            finally
            {
                Database.CloseConnection();
            }
        }

        public int NhanVien_Sua(NhanVien nv, string? matKhauMoi)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.NhanVien_Sua";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@MaNV", nv.MaNV));
            cmd.Parameters.Add(new SqlParameter("@HoNV", nv.HoNV));
            cmd.Parameters.Add(new SqlParameter("@TenNV", nv.TenNV));
            cmd.Parameters.Add(new SqlParameter("@DienThoai", nv.DienThoai));
            cmd.Parameters.Add(new SqlParameter("@Email", nv.Email));
            cmd.Parameters.Add(new SqlParameter("@TenDN", nv.TenDN));
            cmd.Parameters.Add(new SqlParameter("@AnhNV", (object?)nv.AnhNV ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@matKhauMoi", SqlDbType.NVarChar, 50)
            {
                Value = (object?)matKhauMoi ?? DBNull.Value
            });

            Database.OpenConnection();
            try { return cmd.ExecuteNonQuery(); }
            finally { Database.CloseConnection(); }
        }

        public int NhanVien_Xoa(byte MaNV)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.NhanVien_Xoa";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("@MaNV", MaNV));

            Database.OpenConnection();
            try { return cmd.ExecuteNonQuery(); }
            finally { Database.CloseConnection(); }
        }

        public List<string> NhanVien_QuyenDanhSach(byte maNV)
        {
            using var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.NhanVien_QuyenDanhSach";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("@MaNV", maNV));
            Database.OpenConnection();
            try
            {
                using var reader = cmd.ExecuteReader();
                var result = new List<string>();
                while (reader.Read()) result.Add(reader.GetString(0));
                return result;
            }
            finally { Database.CloseConnection(); }
        }

        public bool NhanVien_CoQuyen(byte maNV, string tenQuyen)
        {
            using var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.NhanVien_CoQuyen";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("@MaNV", maNV));
            cmd.Parameters.Add(new SqlParameter("@TenQuyen", tenQuyen));
            Database.OpenConnection();
            try { return Convert.ToInt32(cmd.ExecuteScalar()) == 1; }
            finally { Database.CloseConnection(); }
        }

        public byte NhanVien_DangNhap(string TenDN, string MatKhau) //Trả về Mã nhân viên nếu đăng nhập thành công
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "NhanVien_DangNhap";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("@TenDN", TenDN));
            cmd.Parameters.Add(new SqlParameter("@MatKhau", MatKhau));
            Database.OpenConnection();
            var reader = cmd.ExecuteReader();
            byte maNV = 0;
            if (reader.Read())//Nếu có dữ liệu để đọc
            {
                maNV = reader.GetByte(reader.GetOrdinal("MaNV"));
            }
            Database.CloseConnection();
            return maNV;
        }
        // 2. Thao tác trên bảng Quyen
        public DbSet<Quyen> Quyens { get; set; }

        public List<Quyen> Quyen_DS()
        {
            return Quyens
                .FromSqlRaw("EXEC dbo.Quyen_DS")
                .ToList();
        }

        public Quyen? Quyen_CT(byte MaQuyen)
        {
            var p = new[] { new SqlParameter("MaQuyen", MaQuyen) };
            return Quyens.FromSqlRaw("EXEC Quyen_CT @MaQuyen", p).ToList().FirstOrDefault();
        }

        public int Quyen_Them(Quyen q)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.Quyen_Them";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@TenQuyen", q.TenQuyen));
            cmd.Parameters.Add(new SqlParameter("@MoTa", (object?)q.MoTa ?? DBNull.Value));

            Database.OpenConnection();
            try
            {
                return cmd.ExecuteNonQuery();
            }
            finally
            {
                Database.CloseConnection();
            }
        }

        public int Quyen_Sua(Quyen q)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.Quyen_Sua";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@MaQuyen", q.MaQuyen));
            cmd.Parameters.Add(new SqlParameter("@TenQuyen", q.TenQuyen));
            cmd.Parameters.Add(new SqlParameter("@MoTa", (object?)q.MoTa ?? DBNull.Value));

            Database.OpenConnection();
            try { return cmd.ExecuteNonQuery(); }
            finally { Database.CloseConnection(); }
        }

        public int Quyen_Xoa(byte MaQuyen)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.Quyen_Xoa";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("@MaQuyen", MaQuyen));

            Database.OpenConnection();
            try { return cmd.ExecuteNonQuery(); }
            finally { Database.CloseConnection(); }
        }
        // 3. Thao tác trên bảng LoaiBanh
        public DbSet<LoaiBanh> LoaiBanhs { get; set; }

        public List<LoaiBanh> LoaiBanh_DS()
        {
            return LoaiBanhs
                .FromSqlRaw("EXEC dbo.LoaiBanh_DS")
                .ToList();
        }

        public LoaiBanh? LoaiBanh_CT(byte MaLoai)
        {
            var p = new[] { new SqlParameter("MaLoai", MaLoai) };
            return LoaiBanhs.FromSqlRaw("EXEC LoaiBanh_CT @MaLoai", p).ToList().FirstOrDefault();
        }

        public int LoaiBanh_Them(LoaiBanh lb)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.LoaiBanh_Them";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@TenLoaiBanh", lb.TenLoaiBanh));
            cmd.Parameters.Add(new SqlParameter("@MoTa", (object?)lb.MoTa ?? DBNull.Value));

            Database.OpenConnection();
            try
            {
                return cmd.ExecuteNonQuery();
            }
            finally
            {
                Database.CloseConnection();
            }
        }

        public int LoaiBanh_Sua(LoaiBanh lb)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.LoaiBanh_Sua";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@MaLoaiBanh", lb.MaLoaiBanh));
            cmd.Parameters.Add(new SqlParameter("@TenLoaiBanh", lb.TenLoaiBanh));
            cmd.Parameters.Add(new SqlParameter("@MoTa", (object?)lb.MoTa ?? DBNull.Value));

            Database.OpenConnection();
            try { return cmd.ExecuteNonQuery(); }
            finally { Database.CloseConnection(); }
        }

        public int LoaiBanh_Xoa(byte MaLoaiBanh)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.LoaiBanh_Xoa";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("@MaLoaiBanh", MaLoaiBanh));

            Database.OpenConnection();
            try { return cmd.ExecuteNonQuery(); }
            finally { Database.CloseConnection(); }
        }

        // 6. Thao tác trên bảng KhachHang
        public DbSet<KhachHang> KhachHangs { get; set; }

        public List<KhachHang> KhachHang_DS()
        {
            return KhachHangs
                .FromSqlRaw("EXEC dbo.KhachHang_DS")
                .ToList();
        }

        public KhachHang? KhachHang_CT(short MaKH)
        {
            var p = new[] { new SqlParameter("MaKH", MaKH) };
            return KhachHangs.FromSqlRaw("EXEC KhachHang_CT @MaKH", p).ToList().FirstOrDefault();
        }

        public int KhachHang_Them(KhachHang kh, string? matKhauMoi)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.KhachHang_Them";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@HoKH", kh.HoKH));
            cmd.Parameters.Add(new SqlParameter("@TenKH", kh.TenKH));
            cmd.Parameters.Add(new SqlParameter("@TenDN", kh.TenDN));
            cmd.Parameters.Add(new SqlParameter("@DienThoai", (object?)kh.DienThoai ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@Email", (object?)kh.Email ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@DiaChiThuongDung",
                (object?)kh.DiaChiThuongDung ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@matKhauMoi", SqlDbType.NVarChar, 50)
            {
                Value = (object?)matKhauMoi ?? DBNull.Value
            });
            Database.OpenConnection();
            try
            {
                return cmd.ExecuteNonQuery();
            }
            finally
            {
                Database.CloseConnection();
            }
        }

        public int KhachHang_Sua(KhachHang kh, string? matKhauMoi)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.KhachHang_Sua";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@MaKH", kh.MaKH));
            cmd.Parameters.Add(new SqlParameter("@HoKH", kh.HoKH));
            cmd.Parameters.Add(new SqlParameter("@TenKH", kh.TenKH));
            cmd.Parameters.Add(new SqlParameter("@TenDN", kh.TenDN));
            cmd.Parameters.Add(new SqlParameter("@DienThoai", (object?)kh.DienThoai ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@Email", (object?)kh.Email ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@DiaChiThuongDung",
                (object?)kh.DiaChiThuongDung ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@matKhauMoi", SqlDbType.NVarChar, 50)
            {
                Value = (object?)matKhauMoi ?? DBNull.Value
            });

            Database.OpenConnection();
            try { return cmd.ExecuteNonQuery(); }
            finally { Database.CloseConnection(); }
        }

        public int KhachHang_Xoa(short MaKH)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.KhachHang_Xoa";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("@MaKH", MaKH));

            Database.OpenConnection();
            try { return cmd.ExecuteNonQuery(); }
            finally { Database.CloseConnection(); }
        }

        public short KhachHang_DangNhap(string TenDN, string MatKhau) //Trả về Mã nhân viên nếu đăng nhập thành công
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "KhachHang_DangNhap";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("@TenDN", TenDN));
            cmd.Parameters.Add(new SqlParameter("@MatKhau", MatKhau));
            Database.OpenConnection();
            var reader = cmd.ExecuteReader();
            short maKH = 0;
            if (reader.Read())
            {
                maKH = reader.GetInt16(reader.GetOrdinal("MaKH"));
            }
            Database.CloseConnection();
            return maKH;
        }
        // 7. Thao tác trên bảng PhanQuyen
        public DbSet<PhanQuyen> PhanQuyens { get; set; }

        public List<PhanQuyen> PhanQuyen_DS()
        {
            return PhanQuyens
                .FromSqlRaw("EXEC dbo.PhanQuyen_DS")
                .ToList();
        }

        public PhanQuyen? PhanQuyen_CT(byte maquyen, byte manv)
        {
            var p = new[]
            {
                new Microsoft.Data.SqlClient.SqlParameter("MaQuyen", maquyen),
                new Microsoft.Data.SqlClient.SqlParameter("MaNV", manv)
            };
            return PhanQuyens.FromSqlRaw("EXEC PhanQuyen_CT @MaQuyen, @MaNV", p).ToList().FirstOrDefault();
        }

        public int PhanQuyen_Them(PhanQuyen pq)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.PhanQuyen_Them";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@MaQuyen", pq.MaQuyen));
            cmd.Parameters.Add(new SqlParameter("@MaNV", pq.MaNV));

            Database.OpenConnection();
            try
            {
                return cmd.ExecuteNonQuery();
            }
            finally
            {
                Database.CloseConnection();
            }
        }

        public int PhanQuyen_Xoa(byte MaQuyen, byte MaNV)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "PhanQuyen_Xoa";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("@MaQuyen", MaQuyen));
            cmd.Parameters.Add(new SqlParameter("@MaNV", MaNV));
            Database.OpenConnection();
            try
            {
                return cmd.ExecuteNonQuery();
            }
            finally
            {
                Database.CloseConnection();
            }
        }
        // 9. Thao tác trên bảng SanPham
        public DbSet<SanPham> SanPhams { get; set; }

        public List<SanPham> SanPham_DS()
        {
            return SanPhams
                .FromSqlRaw("EXEC dbo.SanPham_DS")
                .ToList();
        }

        public SanPham? SanPham_CT(string MaSP)
        {
            var p = new[] { new SqlParameter("MaSP", MaSP) };
            return SanPhams.FromSqlRaw("EXEC SanPham_CT @MaSP", p).ToList().FirstOrDefault();
        }

        public int SanPham_Them(SanPham sp)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.SanPham_Them";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@MaLoaiBanh", sp.MaLoaiBanh));
            cmd.Parameters.Add(new SqlParameter("@TenSP", sp.TenSP));
            cmd.Parameters.Add(new SqlParameter("@MaNVTao", sp.MaNVTao));
            cmd.Parameters.Add(new SqlParameter("@MoTaNgan", (object?)sp.MoTaNgan ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@MoTaChiTiet",
                (object?)sp.MoTaChiTiet ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@TrangThaiHienThi",
                (object?)sp.TrangThaiHienThi ?? "DRAFT"));

            Database.OpenConnection();
            try
            {
                return cmd.ExecuteNonQuery();
            }
            finally
            {
                Database.CloseConnection();
            }
        }

        public int SanPham_Sua(SanPham sp)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.SanPham_Sua";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@MaSP", sp.MaSP));
            cmd.Parameters.Add(new SqlParameter("@MaLoaiBanh", sp.MaLoaiBanh));
            cmd.Parameters.Add(new SqlParameter("@TenSP", sp.TenSP));
            cmd.Parameters.Add(new SqlParameter("@TrangThaiHienThi", sp.TrangThaiHienThi));
            cmd.Parameters.Add(new SqlParameter("@MaNVCapNhat", sp.MaNVCapNhat));
            cmd.Parameters.Add(new SqlParameter("@MoTaNgan", (object?)sp.MoTaNgan ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@MoTaChiTiet", SqlDbType.NVarChar, -1)
            {
                Value = (object?)sp.MoTaChiTiet ?? DBNull.Value
            });

            Database.OpenConnection();
            try { return cmd.ExecuteNonQuery(); }
            finally { Database.CloseConnection(); }
        }

        public int SanPham_Xoa(string MaSP)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.SanPham_Xoa";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("@MaSP", MaSP));

            Database.OpenConnection();
            try { return cmd.ExecuteNonQuery(); }
            finally { Database.CloseConnection(); }
        }

        // 10. Thao tác trên bảng HinhAnhBanh
        public DbSet<HinhAnhBanh> HinhAnhBanhs { get; set; }

        public List<HinhAnhBanh> HinhAnhBanh_DS()
        {
            return HinhAnhBanhs
                .FromSqlRaw("EXEC dbo.HinhAnhBanh_DS")
                .ToList();
        }

        public HinhAnhBanh? HinhAnhBanh_CT(short MaHinh)
        {
            var p = new[] { new SqlParameter("MaHinh", MaHinh) };
            return HinhAnhBanhs.FromSqlRaw("EXEC HinhAnhBanh_CT @MaHinh", p).ToList().FirstOrDefault();
        }

        public int HinhAnhBanh_Them(HinhAnhBanh ha)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.HinhAnhBanh_Them";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@MaSP", ha.MaSP));
            cmd.Parameters.Add(new SqlParameter("@DuongDanAnh", ha.DuongDanAnh));
            cmd.Parameters.Add(new SqlParameter("@LaAnhDaiDien", ha.LaAnhDaiDien));
            cmd.Parameters.Add(new SqlParameter("@ThuTuHienThi", ha.ThuTuHienThi));

            Database.OpenConnection();
            try
            {
                return cmd.ExecuteNonQuery();
            }
            finally
            {
                Database.CloseConnection();
            }
        }

        public int HinhAnhBanh_Sua(HinhAnhBanh ha)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.HinhAnhBanh_Sua";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@MaAnh", ha.MaAnh));
            cmd.Parameters.Add(new SqlParameter("@MaSP", ha.MaSP));
            cmd.Parameters.Add(new SqlParameter("@DuongDanAnh", ha.DuongDanAnh));
            cmd.Parameters.Add(new SqlParameter("@LaAnhDaiDien", ha.LaAnhDaiDien));
            cmd.Parameters.Add(new SqlParameter("@ThuTuHienThi", ha.ThuTuHienThi));

            Database.OpenConnection();
            try { return cmd.ExecuteNonQuery(); }
            finally { Database.CloseConnection(); }
        }

        public int HinhAnhBanh_Xoa(short MaAnh)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.HinhAnhBanh_Xoa";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("@MaAnh", MaAnh));

            Database.OpenConnection();
            try { return cmd.ExecuteNonQuery(); }
            finally { Database.CloseConnection(); }
        }
        // 11. Thao tác trên bảng BienTheBanh
        public DbSet<BienTheBanh> BienTheBanhs { get; set; }

        public List<BienTheBanh> BienTheBanh_DS()
        {
            return BienTheBanhs
                .FromSqlRaw("EXEC dbo.BienTheBanh_DS")
                .ToList();
        }

        public BienTheBanh? BienTheBanh_CT(short MaBienThe)
        {
            var p = new[] { new SqlParameter("MaBienThe", MaBienThe) };
            return BienTheBanhs.FromSqlRaw("EXEC BienTheBanh_CT @MaBienThe", p).ToList().FirstOrDefault();
        }

        public int BienTheBanh_Them(BienTheBanh bt)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.BienTheBanh_Them";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@MaSP", bt.MaSP));
            cmd.Parameters.Add(new SqlParameter("@TenBienThe", bt.TenBienThe));
            cmd.Parameters.Add(new SqlParameter("@GiaBan", bt.GiaBan));
            cmd.Parameters.Add(new SqlParameter("@SoLuongThucTe", bt.SoLuongThucTe));
            cmd.Parameters.Add(new SqlParameter("@SoNgayChuanBiDuKien",
                bt.SoNgayChuanBiDuKien));
            cmd.Parameters.Add(new SqlParameter("@TrangThaiBan", bt.TrangThaiBan));

            Database.OpenConnection();
            try
            {
                return cmd.ExecuteNonQuery();
            }
            finally
            {
                Database.CloseConnection();
            }
        }

        public int BienTheBanh_Sua(BienTheBanh bt)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.BienTheBanh_Sua";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@MaBienThe", bt.MaBienThe));
            cmd.Parameters.Add(new SqlParameter("@MaSP", bt.MaSP));
            cmd.Parameters.Add(new SqlParameter("@TenBienThe", bt.TenBienThe));
            cmd.Parameters.Add(new SqlParameter("@GiaBan", bt.GiaBan));
            cmd.Parameters.Add(new SqlParameter("@SoLuongThucTe", bt.SoLuongThucTe));
            cmd.Parameters.Add(new SqlParameter("@SoNgayChuanBiDuKien", bt.SoNgayChuanBiDuKien));
            cmd.Parameters.Add(new SqlParameter("@TrangThaiBan", bt.TrangThaiBan));

            Database.OpenConnection();
            try { return cmd.ExecuteNonQuery(); }
            finally { Database.CloseConnection(); }
        }

        public int BienTheBanh_Xoa(short MaBienThe)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.BienTheBanh_Xoa";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("@MaBienThe", MaBienThe));

            Database.OpenConnection();
            try { return cmd.ExecuteNonQuery(); }
            finally { Database.CloseConnection(); }
        }
        // 14. Thao tác trên bảng DonHang
        public DbSet<DonHang> DonHangs { get; set; }

        public List<DonHang> DonHang_DS()
        {
            return DonHangs
                .FromSqlRaw("EXEC dbo.DonHang_DS")
                .ToList();
        }

        public DonHang? DonHang_CT(string MaDonHang)
        {
            var p = new[] { new SqlParameter("MaDonHang", MaDonHang) };
            return DonHangs.FromSqlRaw("EXEC DonHang_CT @MaDonHang", p).ToList().FirstOrDefault();
        }

        public string DatHangWeb(short maKH, string ten, string dienThoai, string diaChi, string gioHangJson, string? ghiChu)
        {
            using var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.DonHang_WebDat";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("@MaKH", maKH));
            cmd.Parameters.Add(new SqlParameter("@TenNguoiNhan", ten));
            cmd.Parameters.Add(new SqlParameter("@DienThoaiNguoiNhan", dienThoai));
            cmd.Parameters.Add(new SqlParameter("@DiaChiGiaoHang", diaChi));
            cmd.Parameters.Add(new SqlParameter("@GioHangJson", gioHangJson));
            cmd.Parameters.Add(new SqlParameter("@GhiChu", (object?)ghiChu ?? DBNull.Value));
            Database.OpenConnection();
            try { return Convert.ToString(cmd.ExecuteScalar())!; }
            finally { Database.CloseConnection(); }
        }

        public int DonHang_Them(DonHang dh)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.DonHang_Them";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@TenNguoiNhan", dh.TenNguoiNhan));
            cmd.Parameters.Add(new SqlParameter("@DienThoaiNguoiNhan", dh.DienThoaiNguoiNhan));
            cmd.Parameters.Add(new SqlParameter("@DiaChiGiaoHang", dh.DiaChiGiaoHang));
            cmd.Parameters.Add(new SqlParameter("@MaKH", (object?)dh.MaKH ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@MaNVXuLy", (object?)dh.MaNVXuLy ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@PhiGiaoHang", dh.PhiGiaoHang));
            cmd.Parameters.Add(new SqlParameter("@NgayGiaoDuKien",
                (object?)dh.NgayGiaoDuKien ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@GhiChu", (object?)dh.GhiChu ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@NgayDat", (object?)dh.NgayDat ?? DBNull.Value));

            Database.OpenConnection();
            try
            {
                return cmd.ExecuteNonQuery();
            }
            finally
            {
                Database.CloseConnection();
            }
        }

        public int DonHang_Sua(DonHang dh)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.DonHang_Sua";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@MaDonHang", dh.MaDonHang));
            cmd.Parameters.Add(new SqlParameter("@TrangThaiDonHang", dh.TrangThaiDonHang));
            cmd.Parameters.Add(new SqlParameter("@TenNguoiNhan", dh.TenNguoiNhan));
            cmd.Parameters.Add(new SqlParameter("@DienThoaiNguoiNhan", dh.DienThoaiNguoiNhan));
            cmd.Parameters.Add(new SqlParameter("@DiaChiGiaoHang", dh.DiaChiGiaoHang));
            cmd.Parameters.Add(new SqlParameter("@PhiGiaoHang", dh.PhiGiaoHang));
            cmd.Parameters.Add(new SqlParameter("@TienGiamGia", dh.TienGiamGia));
            cmd.Parameters.Add(new SqlParameter("@MaKH", (object?)dh.MaKH ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@MaNVXuLy", (object?)dh.MaNVXuLy ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@MaNVXacNhan", (object?)dh.MaNVXacNhan ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@MaNVHoanTat", (object?)dh.MaNVHoanTat ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@NgayGiaoDuKien",
                (object?)dh.NgayGiaoDuKien ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@TienCocYeuCau",
                (object?)dh.TienCocYeuCau ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@GhiChu", (object?)dh.GhiChu ?? DBNull.Value));

            Database.OpenConnection();
            try { return cmd.ExecuteNonQuery(); }
            finally { Database.CloseConnection(); }
        }

        public int DonHang_Xoa(string MaDonHang)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.DonHang_Xoa";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("@MaDonHang", MaDonHang));

            Database.OpenConnection();
            try { return cmd.ExecuteNonQuery(); }
            finally { Database.CloseConnection(); }
        }
        // 17. Thao tác trên bảng ChiTietDonHang
        public DbSet<ChiTietDonHang> ChiTietDonHangs { get; set; }

        public List<ChiTietDonHang> ChiTietDonHang_DS()
        {
            return ChiTietDonHangs
                .FromSqlRaw("EXEC dbo.ChiTietDonHang_DS")
                .ToList();
        }

        public ChiTietDonHang? ChiTietDonHang_CT(string MaDonHang, short STT)
        {
            var p = new[]
            {
                new SqlParameter("MaDonHang", MaDonHang),
                new SqlParameter("STT", STT)
            };
            return ChiTietDonHangs.FromSqlRaw("EXEC ChiTietDonHang_CT @MaDonHang, @STT", p).ToList().FirstOrDefault();
        }

        public int ChiTietDonHang_Them(ChiTietDonHang ct)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.ChiTietDonHang_Them";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@MaDonHang", ct.MaDonHang));
            cmd.Parameters.Add(new SqlParameter("@MaBienThe", ct.MaBienThe));
            cmd.Parameters.Add(new SqlParameter("@SoLuong", ct.SoLuong));
            cmd.Parameters.Add(new SqlParameter("@DonGiaTaiThoiDiemDat",
                (object?)ct.DonGiaTaiThoiDiemDat ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@NgayDuKienCoHang",
                (object?)ct.NgayDuKienCoHang ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@GhiChu", (object?)ct.GhiChu ?? DBNull.Value));

            Database.OpenConnection();
            try
            {
                return cmd.ExecuteNonQuery();
            }
            finally
            {
                Database.CloseConnection();
            }
        }

        public int ChiTietDonHang_Sua(ChiTietDonHang ct)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.ChiTietDonHang_Sua";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@MaDonHang", ct.MaDonHang));
            cmd.Parameters.Add(new SqlParameter("@STT", ct.STT));
            cmd.Parameters.Add(new SqlParameter("@MaBienThe", ct.MaBienThe));
            cmd.Parameters.Add(new SqlParameter("@SoLuong", ct.SoLuong));
            cmd.Parameters.Add(new SqlParameter("@DonGiaTaiThoiDiemDat", ct.DonGiaTaiThoiDiemDat));
            cmd.Parameters.Add(new SqlParameter("@SoLuongCoSanDaGiu", ct.SoLuongCoSanDaGiu));
            cmd.Parameters.Add(new SqlParameter("@SoLuongCanLam", ct.SoLuongCanLam));
            cmd.Parameters.Add(new SqlParameter("@SoLuongDaLam", ct.SoLuongDaLam));
            cmd.Parameters.Add(new SqlParameter("@NgayDuKienCoHang",
                (object?)ct.NgayDuKienCoHang ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@GhiChu", (object?)ct.GhiChu ?? DBNull.Value));

            Database.OpenConnection();
            try { return cmd.ExecuteNonQuery(); }
            finally { Database.CloseConnection(); }
        }

        public int ChiTietDonHang_Xoa(string MaDonHang, short STT)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.ChiTietDonHang_Xoa";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("@MaDonHang", MaDonHang));
            cmd.Parameters.Add(new SqlParameter("@STT", STT));

            Database.OpenConnection();
            try { return cmd.ExecuteNonQuery(); }
            finally { Database.CloseConnection(); }
        }
        // 18. Thao tác trên bảng ThanhToan
        public DbSet<ThanhToan> ThanhToans { get; set; }

        public List<ThanhToan> ThanhToan_DS()
        {
            return ThanhToans
                .FromSqlRaw("EXEC dbo.ThanhToan_DS")
                .ToList();
        }

        public ThanhToan? ThanhToan_CT(string MaThanhToan)
        {
            var p = new[] { new SqlParameter("MaThanhToan", MaThanhToan) };
            return ThanhToans.FromSqlRaw("EXEC ThanhToan_CT @MaThanhToan", p).ToList().FirstOrDefault();
        }

        public int ThanhToan_Them(ThanhToan tt)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.ThanhToan_Them";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@MaDonHang", tt.MaDonHang));
            cmd.Parameters.Add(new SqlParameter("@LoaiGiaoDich", tt.LoaiGiaoDich));
            cmd.Parameters.Add(new SqlParameter("@SoTien", tt.SoTien));
            cmd.Parameters.Add(new SqlParameter("@PhuongThuc", tt.PhuongThuc));
            cmd.Parameters.Add(new SqlParameter("@MaThanhToanGoc",
                (object?)tt.MaThanhToanGoc ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@DuongDanChungTu",
                (object?)tt.DuongDanChungTu ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@GhiChu", (object?)tt.GhiChu ?? DBNull.Value));

            Database.OpenConnection();
            try
            {
                return cmd.ExecuteNonQuery();
            }
            finally
            {
                Database.CloseConnection();
            }
        }

        public int ThanhToan_Sua(ThanhToan tt)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.ThanhToan_Sua";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@MaThanhToan", tt.MaThanhToan));
            cmd.Parameters.Add(new SqlParameter("@SoTien", tt.SoTien));
            cmd.Parameters.Add(new SqlParameter("@PhuongThuc", tt.PhuongThuc));
            cmd.Parameters.Add(new SqlParameter("@TrangThaiThanhToan", tt.TrangThaiThanhToan));
            cmd.Parameters.Add(new SqlParameter("@MaNVXacNhan",
                (object?)tt.MaNVXacNhan ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@DuongDanChungTu",
                (object?)tt.DuongDanChungTu ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@GhiChu", (object?)tt.GhiChu ?? DBNull.Value));

            Database.OpenConnection();
            try { return cmd.ExecuteNonQuery(); }
            finally { Database.CloseConnection(); }
        }

        public int ThanhToan_Xoa(string MaThanhToan)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.ThanhToan_Xoa";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("@MaThanhToan", MaThanhToan));

            Database.OpenConnection();
            try { return cmd.ExecuteNonQuery(); }
            finally { Database.CloseConnection(); }
        }
        // Gộp các cấu hình trong một phương thức OnModelCreating
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // PhanQuyen: khóa gồm MaQuyen và MaNV
            modelBuilder.Entity<PhanQuyen>()
                .HasKey(c => new { c.MaQuyen, c.MaNV });


            // ChiTietDonHang: khóa gồm MaDonHang và STT
            modelBuilder.Entity<ChiTietDonHang>()
                .HasKey(c => new { c.MaDonHang, c.STT });
        }
    }
}