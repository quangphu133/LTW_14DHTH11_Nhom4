using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class GioHangController : Controller
    {
        // Khởi tạo DbContext
        private QLNHASACHEntities db = new QLNHASACHEntities();

        // Lấy giỏ hàng từ Session
        public List<Giohang> Laygiohang()
        {
            List<Giohang> lstGiohang = Session["Giohang"] as List<Giohang>;
            if (lstGiohang == null)
            {
                lstGiohang = new List<Giohang>();
                Session["Giohang"] = lstGiohang;
            }
            return lstGiohang;
        }

        // Thêm hàng vào giỏ
        public ActionResult ThemGioHang(int iMaSach, string strURL)
        {
            // Kiểm tra sách có tồn tại trong DB không
            SACH sach = db.SACHes.SingleOrDefault(n => n.MaSach == iMaSach);
            if (sach == null)
            {
                Response.StatusCode = 404;
                return null;
            }

            List<Giohang> lstGiohang = Laygiohang();

            // Kiểm tra sách đã có trong giỏ chưa
            Giohang sanpham = lstGiohang.Find(n => n.iMaSach == iMaSach);
            if (sanpham == null)
            {
                sanpham = new Giohang(iMaSach);
                lstGiohang.Add(sanpham);
            }
            else
            {
                sanpham.iSoLuong++;
            }

            return Redirect(strURL);
        }

        // Trang Giỏ hàng
        public ActionResult GioHang()
        {
            List<Giohang> lstGiohang = Laygiohang();
            if (lstGiohang.Count == 0)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.TongSoLuong = TongSoLuong();
            ViewBag.TongTien = TongTien();
            return View(lstGiohang);
        }

        // Tính tổng số lượng
        private int TongSoLuong()
        {
            int iTongSoLuong = 0;
            List<Giohang> lstGiohang = Session["Giohang"] as List<Giohang>;
            if (lstGiohang != null)
            {
                iTongSoLuong = lstGiohang.Sum(n => n.iSoLuong);
            }
            return iTongSoLuong;
        }

        // Tính tổng tiền
        private double TongTien()
        {
            double dTongTien = 0;
            List<Giohang> lstGiohang = Session["Giohang"] as List<Giohang>;
            if (lstGiohang != null)
            {
                dTongTien = lstGiohang.Sum(n => n.dThanhTien);
            }
            return dTongTien;
        }

        // Cập nhật giỏ hàng
        public ActionResult CapNhatGioHang(int iMaSP, FormCollection f)
        {
            List<Giohang> lstGiohang = Laygiohang();
            Giohang sanpham = lstGiohang.SingleOrDefault(n => n.iMaSach == iMaSP);
            if (sanpham != null)
            {
                sanpham.iSoLuong = int.Parse(f["txtSoLuong"].ToString());
            }
            return RedirectToAction("GioHang");
        }

        // Xóa giỏ hàng
        public ActionResult XoaGioHang(int iMaSP)
        {
            List<Giohang> lstGiohang = Laygiohang();
            Giohang sanpham = lstGiohang.SingleOrDefault(n => n.iMaSach == iMaSP);
            if (sanpham != null)
            {
                lstGiohang.RemoveAll(n => n.iMaSach == iMaSP);
            }
            if (lstGiohang.Count == 0)
            {
                return RedirectToAction("Index", "Home");
            }
            return RedirectToAction("GioHang");
        }

        // Partial View hiển thị icon giỏ hàng (Mini Cart)
        public ActionResult GioHangPartial()
        {
            ViewBag.TongSoLuong = TongSoLuong();
            ViewBag.TongTien = TongTien();
            List<Giohang> lstGiohang = Laygiohang();
            return PartialView(lstGiohang);
        }

        // ==========================================
        // CÁC ACTION ĐẶT HÀNG (CHECKOUT)
        // ==========================================

        [HttpGet]
        public ActionResult DatHang()
        {
            // Kiểm tra đăng nhập
            if (Session["TaiKhoan"] == null || Session["TaiKhoan"].ToString() == "")
            {
                return RedirectToAction("Login", "Users");
            }

            if (Session["Giohang"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            List<Giohang> lstGiohang = Laygiohang();
            ViewBag.TongSoLuong = TongSoLuong();
            ViewBag.TongTien = TongTien();

            return View(lstGiohang);
        }

        [HttpPost]
        public ActionResult DatHang(FormCollection collection)
        {
            // Tạo đơn hàng (Header)
            DONDATHANG ddh = new DONDATHANG();
            KHACHHANG kh = (KHACHHANG)Session["TaiKhoan"];
            List<Giohang> gh = Laygiohang();

            ddh.MaKH = kh.MaKH;
            ddh.NgayDat = DateTime.Now;

            var ngaygiao = String.Format("{0:MM/dd/yyyy}", collection["NgayGiao"]);
            if (string.IsNullOrEmpty(ngaygiao))
            {
                ddh.NgayGiao = DateTime.Now.AddDays(3);
            }
            else
            {
                ddh.NgayGiao = DateTime.Parse(ngaygiao);
            }

            ddh.TinhTrangGiaoHang = false;
            ddh.DaThanhToan = false;

            db.DONDATHANGs.Add(ddh);
            db.SaveChanges(); // Lưu để lấy MaDonHang

            // Lưu chi tiết đơn hàng & Trừ kho
            foreach (var item in gh)
            {
                // Lưu chi tiết đơn hàng
                CHITIETDONHANG ctdh = new CHITIETDONHANG();
                ctdh.MaDonHang = ddh.MaDonHang;
                ctdh.MaSach = item.iMaSach;
                ctdh.SoLuong = item.iSoLuong;
                ctdh.DonGia = (decimal)item.dDonGia;
                db.CHITIETDONHANGs.Add(ctdh);

                // CẬP NHẬT TỒN KHO
                var sach = db.SACHes.Find(item.iMaSach);
                if (sach != null)
                {
                    // Kiểm tra nếu kho không đủ thì báo lỗi
                    if (sach.SoLuongTon < item.iSoLuong)
                    {
                    }

                    // Trừ số lượng tồn
                    sach.SoLuongTon -= item.iSoLuong;
                }
            }

            // Lưu tất cả thay đổi vào Database
            db.SaveChanges();

            Session["Giohang"] = null;
            return RedirectToAction("XacNhanDonHang", "GioHang");
        }

        public ActionResult XacNhanDonHang()
        {
            return View();
        }
    }
}