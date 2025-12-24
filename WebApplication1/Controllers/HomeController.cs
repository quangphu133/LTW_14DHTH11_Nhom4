using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        // Khởi tạo DbContext 
        private QLNHASACHEntities db = new QLNHASACHEntities();

        // ==========================================
        // CÁC ACTION VIEW
        // ==========================================

        public ActionResult Index()
        {
            var model = new HomeViewModel();

            // SÁCH MỚI
            model.SachMoi = db.SACHes.OrderByDescending(s => s.MaSach).Take(10).ToList();

            // SÁCH BÁN CHẠY
            var topSellingIds = db.CHITIETHOADONs
                                    .GroupBy(x => x.MaSach)
                                    .OrderByDescending(g => g.Sum(x => x.SoLuong))
                                    .Select(g => g.Key)
                                    .Take(10)
                                    .ToList();

            if (topSellingIds.Count > 0)
            {
                model.SachBanChay = db.SACHes.Where(s => topSellingIds.Contains(s.MaSach)).ToList();
            }
            else
            {
                // Fallback: Lấy sách giá cao nhất nếu chưa có dữ liệu bán hàng
                model.SachBanChay = db.SACHes.OrderByDescending(s => s.GiaBan).Take(10).ToList();
            }

            // DANH SÁCH THỂ LOẠI
            model.DsTheLoai = db.THELOAIs.Where(t => t.SACHes.Count > 0).ToList();

            return View(model);
        }

        // Action này chỉ được gọi từ Layout (Partial View)
        [ChildActionOnly]
        public ActionResult _MenuPartial()
        {
            var danhSachTheLoai = db.THELOAIs.ToList();
            ViewBag.DsTacGia = db.TACGIAs.Where(t => t.SACHes.Count > 0).ToList();
            return PartialView(danhSachTheLoai);
        }

        public ActionResult Sach()
        {
            // Lấy tất cả sách trong database
            var listTatCaSach = db.SACHes.OrderByDescending(s => s.MaSach).ToList();

            // Gán tiêu đề để hiển thị bên View
            ViewBag.TieuDe = "Tất cả sách";

            return View(listTatCaSach);
        }

        public ActionResult SachTheoTheLoai(int id)
        {
            // Tìm thể loại trước để kiểm tra tồn tại
            var theLoai = db.THELOAIs.Find(id);

            // Nếu không tìm thấy thể loại nào
            if (theLoai == null)
            {
                return HttpNotFound(); // Trả về trang lỗi 404
            }

            // Nếu tồn tại thì mới đi lấy sách
            var listSach = db.SACHes
                             .Where(s => s.MaTL == id)
                             .OrderByDescending(s => s.MaSach) // Sắp xếp sách mới nhập lên đầu
                             .ToList();

            // Gán tiêu đề
            ViewBag.TieuDe = theLoai.TenTL;

            return View(listSach);
        }

        // GET: Hien thi sach theo Tac Gia
        public ActionResult SachTheoTacGia(int id)
        {
            // Tìm tác giả theo ID
            var tacGia = db.TACGIAs.Find(id);
            if (tacGia == null)
            {
                return HttpNotFound(); // Trả về lỗi 404 nếu ID không tồn tại
            }

            // Lấy sách của tác giả đó
            var listSach = db.SACHes
                             .Where(s => s.MaTG == id)
                             .OrderByDescending(s => s.MaSach)
                             .ToList();

            // Gán tiêu đề để hiển thị bên View
            ViewBag.Title = "Tác giả: " + tacGia.TenTG;

            return View(listSach);
        }

        public ActionResult TimKiem(string search = "")
        {
            if (string.IsNullOrEmpty(search))
            {
                ViewBag.TieuDe = "Kết quả tìm kiếm";
                return View(new List<SACH>());
            }

            var listSach = db.SACHes.Where(s => s.TenSach.Contains(search) ||
                                            s.TACGIA.TenTG.Contains(search)).ToList();

            ViewBag.TieuDe = "Kết quả tìm kiếm: " + search;
            return View(listSach);
        }

        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            SACH sach = db.SACHes.Find(id);
            if (sach == null) return HttpNotFound();

            ViewBag.SachLienQuan = db.SACHes
                .Where(s => s.MaTL == sach.MaTL && s.MaSach != sach.MaSach)
                .Take(4)
                .ToList();

            return View(sach);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Giới thiệu về nhà sách.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Title = "Liên hệ với chúng tôi";
            return View();
        }

        // ==========================================
        // 2. CÁC ACTION GIỎ HÀNG (CART)
        // ==========================================

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

        // Partial View hiển thị icon giỏ hàng trên menu
        public ActionResult GioHangPartial()
        {
            ViewBag.TongSoLuong = TongSoLuong();
            ViewBag.TongTien = TongTien();
            List<Giohang> lstGiohang = Laygiohang();
            return PartialView(lstGiohang);
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

        // ==========================================
        // 3. CÁC ACTION ĐẶT HÀNG (CHECKOUT)
        // ==========================================

        [HttpGet]
        public ActionResult DatHang()
        {
            // 1. Kiểm tra đăng nhập
            if (Session["TaiKhoan"] == null)
            {
                // Chưa đăng nhập thì chuyển hướng sang trang Login của UsersController
                return RedirectToAction("Login", "Users");
            }

            // 2. Kiểm tra giỏ hàng có trống không
            if (Session["Giohang"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // 3. Lấy thông tin giỏ hàng để hiển thị
            List<Giohang> lstGiohang = Laygiohang();
            if (lstGiohang.Count == 0)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.TongSoLuong = TongSoLuong();
            ViewBag.TongTien = TongTien();

            // Lấy thông tin khách hàng từ Session["TaiKhoan"] để điền vào form
            ViewBag.KhachHang = (KHACHHANG)Session["TaiKhoan"];

            return View(lstGiohang);
        }

        [HttpPost]
        public ActionResult DatHang(FormCollection collection)
        {
            // 1. Lấy thông tin đơn hàng
            DONDATHANG ddh = new DONDATHANG();
            KHACHHANG kh = (KHACHHANG)Session["TaiKhoan"];
            List<Giohang> gh = Laygiohang();

            if (kh == null) return RedirectToAction("DangNhap", "Home");

            ddh.MaKH = kh.MaKH;
            ddh.NgayDat = DateTime.Now;

            // 2. Xử lý ngày giao hàng
            var ngaygiao = collection["NgayGiao"];
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
            db.SaveChanges(); // Lưu xong để lấy MaDonHang

            // 3. Lưu Chi tiết đơn hàng & CẬP NHẬT TỒN KHO
            foreach (var item in gh)
            {
                // A. Lưu chi tiết đơn hàng
                CHITIETDONHANG ctdh = new CHITIETDONHANG();
                ctdh.MaDonHang = ddh.MaDonHang;
                ctdh.MaSach = item.iMaSach;
                ctdh.SoLuong = item.iSoLuong;
                ctdh.DonGia = (decimal)item.dDonGia;

                db.CHITIETDONHANGs.Add(ctdh);

                // B. Cập nhật giảm số lượng tồn kho
                // Dùng Find để lấy sách theo khóa chính
                var sach = db.SACHes.Find(item.iMaSach);

                if (sach != null)
                {
                    // Xử lý trường hợp SoLuongTon bị null trong DB
                    int tonHienTai = sach.SoLuongTon ?? 0;

                    // Trừ tồn kho
                    sach.SoLuongTon = tonHienTai - item.iSoLuong;

                    db.Entry(sach).State = System.Data.Entity.EntityState.Modified;
                }
            }

            // Lưu tất cả thay đổi vào DB
            db.SaveChanges();

            // 4. Xóa giỏ hàng
            Session["Giohang"] = null;

            return RedirectToAction("XacNhanDonHang", "GioHang");
        }

        public ActionResult XacNhanDonHang()
        {
            return View();
        }

        // ==========================================
        // 4. QUẢN LÝ LỊCH SỬ ĐƠN HÀNG 
        // ==========================================

        // Xem danh sách đơn hàng của khách
        public ActionResult LichSuDonHang()
        {
            // 1. Kiểm tra đăng nhập
            if (Session["TaiKhoan"] == null)
            {
                return RedirectToAction("Login", "Users");
            }

            // 2. Lấy MaKH từ Session
            KHACHHANG kh = (KHACHHANG)Session["TaiKhoan"];

            // 3. Lấy danh sách đơn hàng của khách đó, sắp xếp ngày đặt mới nhất lên đầu
            var listDonHang = db.DONDATHANGs
                                .Where(n => n.MaKH == kh.MaKH)
                                .OrderByDescending(n => n.NgayDat)
                                .ToList();

            return View(listDonHang);
        }

        // Xem chi tiết một đơn hàng cụ thể
        public ActionResult ChiTietDonHang(int id)
        {
            // 1. Kiểm tra đăng nhập
            if (Session["TaiKhoan"] == null)
            {
                return RedirectToAction("Login", "Users");
            }

            KHACHHANG kh = (KHACHHANG)Session["TaiKhoan"];

            // 2. Lấy đơn hàng theo ID và kiểm tra xem đơn hàng đó có phải của khách này không?
            var donHang = db.DONDATHANGs.SingleOrDefault(n => n.MaDonHang == id && n.MaKH == kh.MaKH);

            if (donHang == null)
            {
                return HttpNotFound(); // Không tìm thấy hoặc không có quyền xem
            }

            // 3. Lấy danh sách chi tiết (sách) trong đơn hàng đó
            // Include("SACH") để lấy thông tin tên sách, ảnh bìa từ bảng SACH
            var chiTiet = db.CHITIETDONHANGs
                            .Where(n => n.MaDonHang == id)
                            .ToList();

            ViewBag.DonHang = donHang; // Truyền thông tin chung của đơn hàng qua ViewBag
            return View(chiTiet);
        }
    }
}