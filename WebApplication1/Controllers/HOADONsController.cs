using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using WebApplication1.Filters;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class HOADONsController : BaseAdminController
    {
        private QLNHASACHEntities db = new QLNHASACHEntities();

        // ================== DANH SÁCH HÓA ĐƠN ==================
        public ActionResult Index()
        {
            var hoadons = db.HOADONs
                .Include(h => h.KHACHHANG)
                .Include(h => h.NHANVIEN)
                .OrderByDescending(h => h.NgayLap);
            return View(hoadons.ToList());
        }

        // ================== XEM CHI TIẾT HÓA ĐƠN ==================
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var hoadon = db.HOADONs
                .Include(h => h.KHACHHANG)
                .Include(h => h.NHANVIEN)
                .Include(h => h.CHITIETHOADONs.Select(c => c.SACH))
                .FirstOrDefault(h => h.MaHD == id);
            if (hoadon == null) return HttpNotFound();
            return View(hoadon);
        }

        // ================== CÁC HÀM XỬ LÝ GIỎ HÀNG ==================
        private List<CartItem> LayGioHang()
        {
            var gioHang = Session["GioHang"] as List<CartItem>;
            if (gioHang == null)
            {
                gioHang = new List<CartItem>();
                Session["GioHang"] = gioHang;
            }
            return gioHang;
        }

        public ActionResult ThemGioHang(int id)
        {
            var sach = db.SACHes.Find(id);
            if (sach == null) return HttpNotFound();

            if (sach.SoLuongTon <= 0)
            {
                TempData["Error"] = $"Sách '{sach.TenSach}' đã hết hàng!";
                return RedirectToAction("Index", "SACHes");
            }

            var gioHang = LayGioHang();
            var item = gioHang.FirstOrDefault(p => p.MaSach == id);

            if (item == null)
            {
                gioHang.Add(new CartItem
                {
                    MaSach = id,
                    TenSach = sach.TenSach,
                    SoLuong = 1,
                    DonGia = sach.GiaBan ?? 0
                });
            }
            else
            {
                if (item.SoLuong >= sach.SoLuongTon)
                {
                    TempData["Error"] = $"Chỉ còn {sach.SoLuongTon} cuốn '{sach.TenSach}' trong kho!";
                    return RedirectToAction("GioHang");
                }
                item.SoLuong++;
            }

            Session["GioHang"] = gioHang;
            TempData["Success"] = "Đã thêm vào giỏ hàng!";
            return RedirectToAction("GioHang");
        }

        public ActionResult GioHang()
        {
            var gioHang = LayGioHang();
            if (gioHang.Count == 0) ViewBag.Empty = "Giỏ hàng trống";
            return View(gioHang);
        }

        public ActionResult TangSoLuong(int id) { var g = LayGioHang(); var i = g.FirstOrDefault(x => x.MaSach == id); if (i != null) i.SoLuong++; return RedirectToAction("GioHang"); }

        public ActionResult GiamSoLuong(int id)
        {
            var g = LayGioHang();
            var i = g.FirstOrDefault(x => x.MaSach == id);
            if (i != null)
            {
                if (i.SoLuong > 1) i.SoLuong--;
                else g.Remove(i);
            }
            return RedirectToAction("GioHang");
        }

        public ActionResult XoaGioHang(int id)
        {
            var g = LayGioHang();
            var i = g.FirstOrDefault(x => x.MaSach == id);
            if (i != null) g.Remove(i);
            return RedirectToAction("GioHang");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CapNhatGioHang(List<CartItem> gioHangMoi)
        {
            var gioHang = LayGioHang();
            foreach (var item in gioHangMoi)
            {
                var sp = gioHang.FirstOrDefault(p => p.MaSach == item.MaSach);
                if (sp != null && item.SoLuong > 0)
                {
                    var sach = db.SACHes.Find(item.MaSach);
                    if (sach != null && item.SoLuong > sach.SoLuongTon)
                    {
                        TempData["Error"] = $"Không đủ hàng: {sach.TenSach} (tồn: {sach.SoLuongTon})";
                        return RedirectToAction("GioHang");
                    }
                    sp.SoLuong = item.SoLuong;
                }
            }
            gioHang.RemoveAll(x => x.SoLuong <= 0);
            return RedirectToAction("GioHang");
        }

        // ================== LẬP HÓA ĐƠN (GET) ==================
        public ActionResult Create()
        {
            if (Session["MaNV"] == null) return RedirectToAction("Login", "Users");

            var gioHang = LayGioHang();
            if (gioHang == null || gioHang.Count == 0)
            {
                TempData["Error"] = "Giỏ hàng trống! Vui lòng thêm sản phẩm trước.";
                return RedirectToAction("GioHang");
            }

            ViewBag.MaKH = new SelectList(db.KHACHHANGs, "MaKH", "HoTen");
            ViewBag.GioHang = gioHang;
            ViewBag.TongTien = gioHang.Sum(x => x.ThanhTien);

            return View();
        }

        // ================== LẬP HÓA ĐƠN (POST) ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaKH")] HOADON hoadon)
        {
            if (Session["MaNV"] == null) return RedirectToAction("Login", "Users");

            var gioHang = LayGioHang();
            if (gioHang == null || gioHang.Count == 0) ModelState.AddModelError("", "Giỏ hàng trống!");

            if (ModelState.IsValid)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // 1. Tạo Hóa đơn
                        hoadon.MaNV = (int)Session["MaNV"];
                        hoadon.NgayLap = DateTime.Now;
                        hoadon.TongTien = gioHang.Sum(x => x.ThanhTien);
                        hoadon.TrangThai = "Đã thanh toán";

                        db.HOADONs.Add(hoadon);
                        db.SaveChanges(); // Lấy MaHD

                        // 2. Lưu chi tiết
                        foreach (var item in gioHang)
                        {
                            var sach = db.SACHes.Find(item.MaSach);

                            // Kiểm tra tồn kho
                            if (sach == null || sach.SoLuongTon < item.SoLuong)
                                throw new Exception($"Sách '{item.TenSach}' không đủ hàng (Còn: {sach.SoLuongTon})!");

                            var ct = new CHITIETHOADON
                            {
                                MaHD = hoadon.MaHD,
                                MaSach = item.MaSach,
                                SoLuong = item.SoLuong,
                                DonGiaBan = item.DonGia
                            };

                            db.CHITIETHOADONs.Add(ct);
                        }

                        db.SaveChanges();
                        transaction.Commit();

                        Session.Remove("GioHang");
                        TempData["Success"] = $"Lập hóa đơn #{hoadon.MaHD} thành công! Kho đã được cập nhật.";
                        return RedirectToAction("Index");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        TempData["Error"] = "Lỗi thanh toán: " + ex.Message;
                    }
                }
            }

            ViewBag.MaKH = new SelectList(db.KHACHHANGs, "MaKH", "HoTen", hoadon.MaKH);
            ViewBag.GioHang = gioHang;
            ViewBag.TongTien = gioHang.Sum(x => x.ThanhTien);
            return View(hoadon);
        }

        // ================== XÓA HÓA ĐƠN ==================
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var hoadon = db.HOADONs
                .Include(h => h.KHACHHANG)
                .Include(h => h.NHANVIEN)
                .FirstOrDefault(h => h.MaHD == id);
            if (hoadon == null) return HttpNotFound();
            return View(hoadon);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var hoadon = db.HOADONs.Include(h => h.CHITIETHOADONs).FirstOrDefault(h => h.MaHD == id);
                    if (hoadon == null) return RedirectToAction("Index");

                    // Hoàn trả tồn kho 
                    foreach (var ct in hoadon.CHITIETHOADONs.ToList())
                    {
                        var sach = db.SACHes.Find(ct.MaSach);
                        if (sach != null) sach.SoLuongTon += ct.SoLuong;
                        db.CHITIETHOADONs.Remove(ct);
                    }

                    db.HOADONs.Remove(hoadon);
                    db.SaveChanges();
                    transaction.Commit();

                    TempData["Success"] = "Đã xóa hóa đơn và hoàn trả kho thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["Error"] = "Lỗi khi xóa: " + ex.Message;
                    return RedirectToAction("Index");
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}