using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace WebQLTB.Models
{
    //[PrimaryKey(nameof(MaQuyen), nameof(MaNV))]
    public class PhanQuyen
    {
        [Key]
        public byte MaQuyen {  get; set; }
        public byte MaNV { get; set; }
    }
}
