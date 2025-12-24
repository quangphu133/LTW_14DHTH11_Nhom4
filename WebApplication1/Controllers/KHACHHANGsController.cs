using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebApplication1.Filters;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    // VIEW MODELS
    public class SpResult
    {
        public int Code { get; set; }
        public string ThongBao { get; set; }
        public decimal? SoTienGiam { get; set; }
    }

    public class LichSuChiTietViewModel
    {
        public int MaHD { get; set; }
        public DateTime NgayLap { get; set; }
        public string TenSach { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGiaBan { get; set; }
        public decimal ThanhTien { get; set; }
    }

    public class KhachHangTimKiemViewModel : KHACHHANG
    {
        public string HangThanhVien { get; set; }
    }

    public class KHACHHANGsController : BaseAdminController
    {
        private QLNHASACHEntities db = new QLNHASACHEntities();

        // Trang chủ: Danh sách khách hàng
        public ActionResult Index()
        {
            return View(db.KHACHHANGs.ToList());
        }

        // Tìm kiếm
        public ActionResult TimKiem(string tuKhoa)
        {
            if (string.IsNullOrEmpty(tuKhoa)) return RedirectToAction("Index");
            string sqlQuery = "SELECT * FROM fn_TimKiemKhachHang(@p0)";

            var ketQua = db.Database.SqlQuery<KhachHangTimKiemViewModel>(sqlQuery, tuKhoa).ToList();

            ViewBag.TuKhoa = tuKhoa;
            return View(ketQua);
        }

        // Chi tiết khách hàng
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            KHACHHANG kh = db.KHACHHANGs.Find(id);
            if (kh == null) return HttpNotFound();
            return View(kh);
        }

        // Xem lịch sử mua hàng
        public ActionResult LichSuChiTiet(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var kh = db.KHACHHANGs.Find(id);
            if (kh == null) return HttpNotFound();
            var list = db.Database.SqlQuery<LichSuChiTietViewModel>("EXEC sp_XemChiTietLichSuMuaHang @p0", id).ToList();

            ViewBag.KhachHang = kh;
            return View(list);
        }

        // Thêm mới khách hàng
        public ActionResult Create() { return View(); }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "HoTen,SDT,DiaChi,Email")] KHACHHANG kh)
        {
            if (ModelState.IsValid)
            {
                var res = db.Database.SqlQuery<SpResult>("EXEC sp_ThemKhachHangMoi @p0, @p1, @p2, @p3",
                    kh.HoTen, kh.SDT, kh.DiaChi, kh.Email).FirstOrDefault();

                if (res != null && res.Code == 1) return RedirectToAction("Index");
                else ModelState.AddModelError("SDT", res?.ThongBao);
            }
            return View(kh);
        }

        // Sửa thông tin
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            KHACHHANG kh = db.KHACHHANGs.Find(id);
            if (kh == null) return HttpNotFound();
            return View(kh);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaKH,HoTen,SDT,DiaChi,Email")] KHACHHANG kh)
        {
            if (ModelState.IsValid)
            {
                var res = db.Database.SqlQuery<SpResult>("EXEC sp_CapNhatThongTinKhach @p0, @p1, @p2, @p3",
                    kh.MaKH, kh.HoTen, kh.DiaChi, kh.Email).FirstOrDefault();

                if (res != null && res.Code == 1) return RedirectToAction("Index");
                else ModelState.AddModelError("", "Lỗi cập nhật: " + res?.ThongBao);
            }
            return View(kh);
        }

        // Đổi điểm lấy giảm giá
        public ActionResult DoiDiem(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            KHACHHANG kh = db.KHACHHANGs.Find(id);
            if (kh == null) return HttpNotFound();
            return View(kh);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult DoiDiem(int MaKH, int DiemMuonDoi)
        {
            var kh = db.KHACHHANGs.Find(MaKH);
            if (DiemMuonDoi <= 0) { ModelState.AddModelError("", "Số điểm phải > 0"); return View(kh); }

            var res = db.Database.SqlQuery<SpResult>("EXEC sp_DoiDiemGiamGia @p0, @p1", MaKH, DiemMuonDoi).FirstOrDefault();

            if (res != null)
            {
                if (res.Code == 1)
                {
                    ViewBag.Message = $"Đổi thành công! Giảm: {res.SoTienGiam:N0} VNĐ";
                    ViewBag.MessageType = "success";
                    // Load lại dữ liệu mới nhất để cập nhật điểm trên giao diện
                    kh = db.KHACHHANGs.Find(MaKH);
                }
                else
                {
                    ViewBag.Message = res.ThongBao;
                    ViewBag.MessageType = "danger";
                }
            }
            return View(kh);
        }

        // Tích điểm thủ công
        public ActionResult TichDiem(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            KHACHHANG kh = db.KHACHHANGs.Find(id);
            if (kh == null) return HttpNotFound();
            return View(kh);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult TichDiem(int MaKH, int DiemMuonThem)
        {
            var kh = db.KHACHHANGs.Find(MaKH);
            if (DiemMuonThem <= 0) { ModelState.AddModelError("", "Số điểm phải > 0"); return View(kh); }

            var res = db.Database.SqlQuery<SpResult>("EXEC sp_TichDiem @p0, @p1", MaKH, DiemMuonThem).FirstOrDefault();

            if (res != null && res.Code == 1)
            {
                ViewBag.Message = $"Đã cộng thưởng thêm {DiemMuonThem} điểm!";
                ViewBag.MessageType = "success";
                kh = db.KHACHHANGs.Find(MaKH);
            }
            return View(kh);
        }

        // Xóa khách hàng
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            KHACHHANG kh = db.KHACHHANGs.Find(id);
            if (kh == null) return HttpNotFound();
            return View(kh);
        }
        // POST: KHACHHANGs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            KHACHHANG kh = db.KHACHHANGs.Find(id);
            try
            {
                // Thử xóa khách hàng
                db.KHACHHANGs.Remove(kh);
                db.SaveChanges(); // Nếu khách có hóa đơn, lỗi sẽ xảy ra ở đây
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                // Bắt lỗi và hiện thông báo thay vì sập web
                ModelState.AddModelError("", "Không thể xóa khách hàng này vì họ đang có Hóa Đơn mua hàng trong hệ thống.");

                // Trả về lại giao diện xóa để người dùng thấy thông báo
                return View("Delete", kh);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}