using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebApplication1.Filters;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class SACHesController : BaseAdminController
    {
        private QLNHASACHEntities db = new QLNHASACHEntities();

        // DANH SÁCH + TÌM KIẾM
        public ActionResult Index(string searchString)
        {
            var sACHes = db.SACHes.Include(s => s.NXB).Include(s => s.TACGIA).Include(s => s.THELOAI);

            if (!string.IsNullOrEmpty(searchString))
            {
                sACHes = sACHes.Where(s => s.TenSach.Contains(searchString));
            }

            return View(sACHes.ToList());
        }

        // BÁO CÁO KHO
        public ActionResult CanhBaoNhapHang()
        {
            var data = db.Database.SqlQuery<BaoCaoTonKhoViewModel>("EXEC sp_CanhBaoNhapHang").ToList();
            return View(data);
        }

        // THÊM MỚI
        public ActionResult Create()
        {
            ViewBag.MaNXB = new SelectList(db.NXBs, "MaNXB", "TenNXB");
            ViewBag.MaTG = new SelectList(db.TACGIAs, "MaTG", "TenTG");
            ViewBag.MaTL = new SelectList(db.THELOAIs, "MaTL", "TenTL");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Chỉ nhận uploadAnhBia
        public ActionResult Create(SACH sACH, HttpPostedFileBase uploadAnhBia)
        {
            // KIỂM TRA LOGIC GIÁ BÁN > GIÁ NHẬP
            if (sACH.GiaBan <= sACH.GiaNhap)
            {
                ModelState.AddModelError("GiaBan", "Giá bán phải lớn hơn giá nhập!");
            }

            // KIỂM TRA ẢNH BÌA
            if (uploadAnhBia == null && sACH.AnhBia == null)
            {
                ModelState.AddModelError("", "Vui lòng chọn ảnh bìa cho sách.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // --- XỬ LÝ LƯU ẢNH BÌA ---
                    if (uploadAnhBia != null && uploadAnhBia.ContentLength > 0)
                    {
                        // Tạo tên file ngẫu nhiên theo thời gian
                        string _FileName = DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + System.IO.Path.GetFileName(uploadAnhBia.FileName);
                        string _path = System.IO.Path.Combine(Server.MapPath("~/images/books"), _FileName);
                        uploadAnhBia.SaveAs(_path);

                        // Lưu tên file vào object
                        sACH.AnhBia = _FileName;
                    }

                    // --- LƯU VÀO DATABASE ---
                    db.SACHes.Add(sACH);
                    db.SaveChanges();

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi ghi dữ liệu: " + ex.Message);
                }
            }

            // Nếu có lỗi, load lại dropdown
            ViewBag.MaNXB = new SelectList(db.NXBs, "MaNXB", "TenNXB", sACH.MaNXB);
            ViewBag.MaTG = new SelectList(db.TACGIAs, "MaTG", "TenTG", sACH.MaTG);
            ViewBag.MaTL = new SelectList(db.THELOAIs, "MaTL", "TenTL", sACH.MaTL);
            return View(sACH);
        }

        // CÁC HÀM CƠ BẢN
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            SACH sACH = db.SACHes.Find(id);
            if (sACH == null) return HttpNotFound();
            return View(sACH);
        }

        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            SACH sACH = db.SACHes.Find(id);
            if (sACH == null) return HttpNotFound();
            ViewBag.MaNXB = new SelectList(db.NXBs, "MaNXB", "TenNXB", sACH.MaNXB);
            ViewBag.MaTG = new SelectList(db.TACGIAs, "MaTG", "TenTG", sACH.MaTG);
            ViewBag.MaTL = new SelectList(db.THELOAIs, "MaTL", "TenTL", sACH.MaTL);
            return View(sACH);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(SACH sACH, HttpPostedFileBase uploadAnhBia) // Nhận thêm file
        {
            // Kiểm tra giá
            if (sACH.GiaBan <= sACH.GiaNhap)
                ModelState.AddModelError("GiaBan", "Giá bán phải lớn hơn giá nhập!");

            if (ModelState.IsValid)
            {
                // Xử lý ảnh: Nếu có upload ảnh mới
                if (uploadAnhBia != null && uploadAnhBia.ContentLength > 0)
                {
                    string _FileName = DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + System.IO.Path.GetFileName(uploadAnhBia.FileName);
                    string _path = System.IO.Path.Combine(Server.MapPath("~/images/books"), _FileName);
                    uploadAnhBia.SaveAs(_path);
                    sACH.AnhBia = _FileName; // Cập nhật tên ảnh mới
                }

                db.Entry(sACH).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(sACH);
        }

        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            SACH sACH = db.SACHes.Find(id);
            if (sACH == null) return HttpNotFound();
            return View(sACH);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            // Tìm sách
            SACH sACH = db.SACHes.Find(id);
            if (sACH == null) return HttpNotFound();

            // Xóa các dòng liên quan trong PHIẾU NHẬP
            var lichSuNhap = db.CHITIETPHIEUNHAPs.Where(x => x.MaSach == id).ToList();
            if (lichSuNhap.Any()) { db.CHITIETPHIEUNHAPs.RemoveRange(lichSuNhap); }

            // Xóa các dòng liên quan trong HÓA ĐƠN
            var lichSuBan = db.CHITIETHOADONs.Where(x => x.MaSach == id).ToList();
            if (lichSuBan.Any()) { db.CHITIETHOADONs.RemoveRange(lichSuBan); }

            // Xóa Sách
            db.SACHes.Remove(sACH);
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }

    public class BaoCaoTonKhoViewModel
    {
        public int MaSach { get; set; }
        public string TenSach { get; set; }
        public int SoLuongTon { get; set; }
        public string TenNXB { get; set; }
    }
}