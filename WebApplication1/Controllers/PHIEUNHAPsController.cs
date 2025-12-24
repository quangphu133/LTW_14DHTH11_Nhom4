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
    public class PHIEUNHAPsController : BaseAdminController
    {
        private QLNHASACHEntities db = new QLNHASACHEntities();

        // 1. DANH SÁCH PHIẾU NHẬP
        public ActionResult Index()
        {
            var listPhieu = db.PHIEUNHAPs
                              .Include("NXB")
                              .Include("NHANVIEN")
                              .OrderByDescending(p => p.NgayNhap)
                              .ToList();
            return View(listPhieu);
        }

        // 2. TẠO PHIẾU NHẬP MỚI (GIAO DIỆN)
        public ActionResult Create()
        {
            ViewBag.MaNXB = new SelectList(db.NXBs, "MaNXB", "TenNXB");
            return View();
        }

        // 3. XỬ LÝ TẠO PHIẾU (POST)
        [HttpPost]
        public ActionResult Create(int MaNXB)
        {
            // Kiểm tra đăng nhập (bắt buộc phải có nhân viên thực hiện)
            if (Session["MaNV"] == null)
            {
                return RedirectToAction("Login", "Users");
            }

            int maNV = (int)Session["MaNV"];

            try
            {
                // Gọi Stored Procedure: sp_TaoPhieuNhapMoi
                var maPhieuMoi = db.Database.SqlQuery<decimal>("EXEC sp_TaoPhieuNhapMoi @MaNXB, @MaNV",
                    new System.Data.SqlClient.SqlParameter("@MaNXB", MaNXB),
                    new System.Data.SqlClient.SqlParameter("@MaNV", maNV)
                ).Single();

                return RedirectToAction("NhapChiTiet", new { id = (int)maPhieuMoi });
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi tạo phiếu: " + ex.Message;
                ViewBag.MaNXB = new SelectList(db.NXBs, "MaNXB", "TenNXB");
                return View();
            }
        }

        // 4. NHẬP CHI TIẾT SÁCH CHO PHIẾU
        public ActionResult NhapChiTiet(int id)
        {
            var phieu = db.PHIEUNHAPs.Find(id);
            if (phieu == null) return HttpNotFound();

            if (phieu.TrangThai == "Đã hủy")
            {
                TempData["ThongBao"] = "Phiếu này đã bị hủy, không thể thao tác!";
                return RedirectToAction("Index");
            }

            // --- Lọc sách theo NXB của phiếu nhập ---
            // Chỉ lấy những sách có MaNXB trùng với MaNXB của phiếu nhập
            var listSachTheoNXB = db.SACHes.Where(s => s.MaNXB == phieu.MaNXB).ToList();

            // Kiểm tra nếu NXB này chưa có đầu sách nào
            if (listSachTheoNXB.Count == 0)
            {
                ViewBag.CanhBaoSach = "Nhà xuất bản này chưa có đầu sách nào trong hệ thống.";
            }

            ViewBag.MaSach = new SelectList(listSachTheoNXB, "MaSach", "TenSach");

            // Lấy danh sách chi tiết cũ
            ViewBag.ListChiTiet = db.CHITIETPHIEUNHAPs.Where(c => c.MaPN == id).ToList();

            return View(phieu);
        }

        [HttpPost]
        public ActionResult ThemSachVaoPhieu(int MaPN, int MaSach, int SoLuong)
        {
            try
            {
                // --- Tự động lấy giá từ bảng SACH ---
                var sach = db.SACHes.Find(MaSach);
                if (sach == null)
                {
                    TempData["Loi"] = "Sách không tồn tại!";
                    return RedirectToAction("NhapChiTiet", new { id = MaPN });
                }

                decimal giaTuDong = sach.GiaBan ?? 0;

                var chiTiet = new CHITIETPHIEUNHAP();
                chiTiet.MaPN = MaPN;
                chiTiet.MaSach = MaSach;
                chiTiet.SoLuong = SoLuong;
                chiTiet.DonGiaNhap = giaTuDong; // Gán giá tự động lấy từ bảng Sách

                db.CHITIETPHIEUNHAPs.Add(chiTiet);

                // Trigger SQL sẽ tự động cập nhật kho và tổng tiền
                db.SaveChanges();

                TempData["ThongBao"] = "Đã thêm sách thành công!";
            }
            catch (Exception ex)
            {
                TempData["Loi"] = "Lỗi thêm sách: " + ex.Message;
            }

            return RedirectToAction("NhapChiTiet", new { id = MaPN });
        }

        // 5. HỦY PHIẾU
        public ActionResult HuyPhieu(int id)
        {
            try
            {
                // Gọi Transaction sp_HuyPhieuNhap
                db.Database.ExecuteSqlCommand("EXEC sp_HuyPhieuNhap @MaPN",
                    new System.Data.SqlClient.SqlParameter("@MaPN", id));

                TempData["ThongBao"] = "Đã hủy phiếu và hoàn tác kho thành công!";
            }
            catch (Exception ex)
            {
                TempData["Loi"] = "Lỗi hủy phiếu: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        // 6. BÁO CÁO NHẬP HÀNG
        public ActionResult BaoCao()
        {
            ViewBag.Thang = DateTime.Now.Month;
            ViewBag.Nam = DateTime.Now.Year;
            return View(new List<BaoCaoNXB>());
        }

        [HttpPost]
        public ActionResult BaoCao(int Thang, int Nam)
        {
            try
            {
                var ketQua = db.Database.SqlQuery<BaoCaoNXB>(
                    "EXEC sp_BaoCaoNhapHangTheoNXB @Thang, @Nam",
                    new System.Data.SqlClient.SqlParameter("@Thang", Thang),
                    new System.Data.SqlClient.SqlParameter("@Nam", Nam)
                ).ToList();

                ViewBag.Thang = Thang;
                ViewBag.Nam = Nam;

                if (ketQua.Count == 0)
                {
                    TempData["ThongBao"] = "Không tìm thấy dữ liệu nhập hàng trong tháng này.";
                }

                return View(ketQua);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi khi chạy báo cáo: " + ex.Message;
                return View(new List<BaoCaoNXB>());
            }
        }

        // XÁC NHẬN HOÀN THÀNH PHIẾU (CẬP NHẬT KHO)
        public ActionResult HoanThanhPhieu(int id)
        {
            try
            {
                var phieu = db.PHIEUNHAPs.Find(id);

                // Kiểm tra hợp lệ
                if (phieu == null) return HttpNotFound();
                if (phieu.TrangThai == "Hoàn thành")
                {
                    TempData["Loi"] = "Phiếu này đã hoàn thành rồi, không thể xác nhận lại!";
                    return RedirectToAction("Index");
                }
                if (phieu.TrangThai == "Đã hủy")
                {
                    TempData["Loi"] = "Phiếu này đã bị hủy, không thể hoàn thành!";
                    return RedirectToAction("Index");
                }

                // --- CẬP NHẬT SỐ LƯỢNG TỒN KHO ---
                // Lấy danh sách chi tiết của phiếu này
                var chiTietPhieu = db.CHITIETPHIEUNHAPs.Where(ct => ct.MaPN == id).ToList();

                if (chiTietPhieu.Count == 0)
                {
                    TempData["Loi"] = "Phiếu chưa có sách nào, không thể hoàn thành!";
                    return RedirectToAction("NhapChiTiet", new { id = id });
                }

                foreach (var item in chiTietPhieu)
                {
                    var sach = db.SACHes.Find(item.MaSach);
                    if (sach != null)
                    {
                        // Cộng số lượng nhập vào số lượng tồn hiện tại
                        sach.SoLuongTon = (sach.SoLuongTon ?? 0) + item.SoLuong;
                    }
                }

                // Đổi trạng thái phiếu
                phieu.TrangThai = "Hoàn thành";

                db.SaveChanges();
                TempData["ThongBao"] = "Xác nhận nhập kho thành công! Số lượng sách đã được cập nhật.";
            }
            catch (Exception ex)
            {
                TempData["Loi"] = "Lỗi khi hoàn thành phiếu: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}