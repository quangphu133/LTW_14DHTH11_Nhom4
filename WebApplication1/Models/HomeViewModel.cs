using System.Collections.Generic;

namespace WebApplication1.Models
{
    public class HomeViewModel
    {
        // Danh sách thể loại cho Menu
        public List<THELOAI> DSDanhMuc { get; set; }

        // Danh sách sách bán chạy
        public List<SACH> SachBanChay { get; set; }

        // Danh sách sách mới
        public List<SACH> SachMoi { get; set; }
        public List<THELOAI> DsTheLoai { get; set; }
    }
}