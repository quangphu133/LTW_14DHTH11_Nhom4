using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using WebApplication1.Filters;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class BaoCaoController : BaseAdminController
    {
        private QLNHASACHEntities db = new QLNHASACHEntities();

        // TRANG CHỦ BÁO CÁO
        public ActionResult index()
        {
            var dsNV = db.NHANVIENs
                  .Select(n => new { n.MaNV, n.HoTen })
                  .ToList();

            // Lưu danh sách vào ViewBag để dropdown dùng
            ViewBag.DSNhanVien = new SelectList(dsNV, "MaNV", "HoTen");
            return View();
        }

        // DOANH THU THEO NHÂN VIÊN
        public ActionResult DoanhThuNhanVien()
        {
            // Sử dụng class tự sinh: sp_DoanhThuTheoNhanVien_Result
            var doanhthu = db.Database.SqlQuery<sp_DoanhThuTheoNhanVien_Result>(
                "EXEC sp_DoanhThuTheoNhanVien"
            ).ToList();

            return View(doanhthu);
        }

        // THỐNG KÊ SỐ SÁCH ĐÃ NHẬP
        public ActionResult SoSachNhap()
        {
            // Sử dụng class tự sinh: sp_ThongKeSoSachNhap_Result
            var soSachNhap = db.Database.SqlQuery<sp_ThongKeSoSachNhap_Result>(
                "EXEC sp_ThongKeSoSachNhap"
            ).ToList();

            return View(soSachNhap);
        }

        // THỐNG KÊ SỐ HÓA ĐƠN
        public ActionResult SoHoaDon()
        {
            // Sử dụng class tự sinh: sp_ThongKe_SoLuongHoaDon_Result
            var soHoaDon = db.Database.SqlQuery<sp_ThongKe_SoLuongHoaDon_Result>(
                "EXEC sp_ThongKe_SoLuongHoaDon"
            ).ToList();

            return View(soHoaDon);
        }

        // GET: BaoCao/SoHoaDonNhanVien/5
        public ActionResult SoHoaDonNhanVien(int maNV)
        {
            var param = new SqlParameter("@MaNV", maNV);
            var soHD = db.Database.SqlQuery<int>(
                "SELECT dbo.fn_SoHoaDon(@MaNV)", param).FirstOrDefault();


            return View(soHD);
        }

        // DOANH THU THEO NGÀY
        public ActionResult DoanhThuTheoNgay()
        {
            ViewBag.TuNgay = DateTime.Now.ToString("yyyy-MM-dd");
            ViewBag.DenNgay = DateTime.Now.ToString("yyyy-MM-dd");

            // Trả về list rỗng ban đầu
            return View(new List<sp_DoanhThuTheoNgay_Result>());
        }

        [HttpPost]
        public ActionResult DoanhThuTheoNgay(DateTime? tuNgay, DateTime? denNgay)
        {
            var start = tuNgay ?? DateTime.Now;
            var end = denNgay ?? DateTime.Now;

            var p1 = new SqlParameter("@TuNgay", start);
            var p2 = new SqlParameter("@DenNgay", end);

            // Sử dụng class tự sinh: sp_DoanhThuTheoNgay_Result
            var data = db.Database.SqlQuery<sp_DoanhThuTheoNgay_Result>(
                "EXEC sp_DoanhThuTheoNgay @TuNgay, @DenNgay", p1, p2
            ).ToList();

            ViewBag.TuNgay = start.ToString("yyyy-MM-dd");
            ViewBag.DenNgay = end.ToString("yyyy-MM-dd");

            // Tính tổng doanh thu
            ViewBag.TongDoanhThu = data.Sum(x => x.DoanhThu.GetValueOrDefault(0));

            return View(data);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}