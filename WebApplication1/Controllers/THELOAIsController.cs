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
    public class THELOAIsController : BaseAdminController
    {
        private QLNHASACHEntities db = new QLNHASACHEntities();

        // --- 1. DANH SÁCH (CÓ TÌM KIẾM) ---
        public ActionResult Index(string searchString)
        {
            var list = db.THELOAIs.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                list = list.Where(t => t.TenTL.Contains(searchString));
            }
            return View(list.ToList());
        }

        // --- 2. CHI TIẾT ---
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            THELOAI tHELOAI = db.THELOAIs.Find(id);
            if (tHELOAI == null) return HttpNotFound();
            return View(tHELOAI);
        }

        // --- 3. THÊM MỚI ---
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaTL,TenTL")] THELOAI tHELOAI)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra trùng tên thể loại
                if (db.THELOAIs.Any(x => x.TenTL == tHELOAI.TenTL))
                {
                    ModelState.AddModelError("TenTL", "Tên thể loại này đã tồn tại!");
                    return View(tHELOAI);
                }

                db.THELOAIs.Add(tHELOAI);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(tHELOAI);
        }

        // --- 4. SỬA ---
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            THELOAI tHELOAI = db.THELOAIs.Find(id);
            if (tHELOAI == null) return HttpNotFound();
            return View(tHELOAI);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaTL,TenTL")] THELOAI tHELOAI)
        {
            if (ModelState.IsValid)
            {
                db.Entry(tHELOAI).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(tHELOAI);
        }

        // --- 5. XÓA ---
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            THELOAI tHELOAI = db.THELOAIs.Find(id);
            if (tHELOAI == null) return HttpNotFound();
            return View(tHELOAI);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            // Kiểm tra xem thể loại này có cuốn sách nào không
            bool coSach = db.SACHes.Any(s => s.MaTL == id);

            if (coSach)
            {
                // NẾU CÓ SÁCH: Không xóa ngay, chuyển hướng sang trang "Chuyển đổi và Xóa"
                return RedirectToAction("TransferAndDelete", new { id = id });
            }

            // NẾU KHÔNG CÓ SÁCH: Xóa bình thường
            THELOAI tHELOAI = db.THELOAIs.Find(id);
            db.THELOAIs.Remove(tHELOAI);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // --- 6. CHUYỂN ĐỔI SÁCH RỒI MỚI XÓA ---
        // GET: Hiển thị form chọn thể loại mới để chuyển sách sang
        public ActionResult TransferAndDelete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            THELOAI tHELOAI = db.THELOAIs.Find(id);
            if (tHELOAI == null) return HttpNotFound();

            int countSach = db.SACHes.Count(s => s.MaTL == id);

            // Nếu lỡ vào trang này mà không có sách thì quay về xóa thường
            if (countSach == 0) return RedirectToAction("Delete", new { id = id });

            ViewBag.SoLuongSach = countSach;

            // Lấy danh sách các thể loại KHÁC thể loại đang xóa
            var otherCategories = db.THELOAIs.Where(t => t.MaTL != id).ToList();
            ViewBag.MaTheLoaiMoi = new SelectList(otherCategories, "MaTL", "TenTL");

            return View(tHELOAI);
        }

        // POST: Thực thi Procedure chuyển sách và xóa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TransferAndDeleteConfirm(int MaTheLoaiCu, int MaTheLoaiMoi)
        {
            try
            {
                // Gọi Stored Procedure: sp_ChuyenDoiVaXoaTheLoai
                string sqlQuery = "EXEC sp_ChuyenDoiVaXoaTheLoai @MaTheLoaiCu, @MaTheLoaiMoi";
                db.Database.ExecuteSqlCommand(sqlQuery,
                    new SqlParameter("@MaTheLoaiCu", MaTheLoaiCu),
                    new SqlParameter("@MaTheLoaiMoi", MaTheLoaiMoi)
                );

                TempData["SuccessMessage"] = "Đã chuyển toàn bộ sách sang thể loại mới và xóa thể loại cũ thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi xử lý: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}