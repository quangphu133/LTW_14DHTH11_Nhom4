using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    public class Giohang
    {
        QLNHASACHEntities db = new QLNHASACHEntities();

        public int iMaSach { get; set; }
        public string sTenSach { get; set; }
        public string sAnhBia { get; set; } // <--- Thêm dòng này
        public double dDonGia { get; set; }
        public int iSoLuong { get; set; }
        public double dThanhTien
        {
            get { return iSoLuong * dDonGia; }
        }

        public Giohang(int MaSach)
        {
            iMaSach = MaSach;
            SACH sach = db.SACHes.Single(n => n.MaSach == iMaSach);
            sTenSach = sach.TenSach;
            sAnhBia = sach.AnhBia; // <--- Thêm dòng này (Lấy ảnh từ CSDL)
            dDonGia = double.Parse(sach.GiaBan.ToString());
            iSoLuong = 1;
        }
    }
}