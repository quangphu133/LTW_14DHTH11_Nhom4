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
    public class NXBsController : BaseAdminController
    {
        private QLNHASACHEntities db = new QLNHASACHEntities();

        // --- 1. DANH SÁCH NXB ---
        public ActionResult Index()
        {
            return View(db.NXBs.ToList());
        }

        // --- 2. XEM CHI TIẾT ---
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            NXB nxb = db.NXBs.Find(id);
            if (nxb == null) return HttpNotFound();
            return View(nxb);
        }

        // --- 3. THÊM MỚI ---
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "TenNXB,DiaChi,SDT")] NXB nxb)
        {
            if (ModelState.IsValid)
            {
                db.NXBs.Add(nxb);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(nxb);
        }

        // --- 4. SỬA (EDIT) ---
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var nxb = db.NXBs.Find(id);
            if (nxb == null) return HttpNotFound();
            return View(nxb);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaNXB,TenNXB,DiaChi,SDT")] NXB nxb)
        {
            if (ModelState.IsValid)
            {
                db.Entry(nxb).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(nxb);
        }

        // --- 5. XÓA (DELETE) ---
        // Bước 1: Hiện trang xác nhận xóa
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var nxb = db.NXBs.Find(id);
            if (nxb == null) return HttpNotFound();
            return View(nxb);
        }

        // Bước 2: Thực hiện xóa khi bấm nút "Xóa"
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var nxb = db.NXBs.Find(id);
            try
            {
                db.NXBs.Remove(nxb);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Không thể xóa Nhà Xuất Bản này vì đã có dữ liệu liên quan (Sách hoặc Phiếu nhập).");
                return View(nxb);
            }
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