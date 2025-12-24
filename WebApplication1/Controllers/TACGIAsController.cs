using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class TACGIAsController : BaseAdminController
    {
        private QLNHASACHEntities db = new QLNHASACHEntities();

        // GET: TACGIAs
        public ActionResult Index(string searchString)
        {
            ViewBag.Title = "Danh sách Tác giả";

            var tacGias = from tg in db.TACGIAs
                          select tg;

            // Nếu có từ khóa tìm kiếm thì lọc theo tên
            if (!String.IsNullOrEmpty(searchString))
            {
                tacGias = tacGias.Where(s => s.TenTG.Contains(searchString));
            }

            return View(tacGias.ToList());
        }

        // GET: TACGIAs/Details/5
        public ActionResult Details(int? id)
        {
            ViewBag.Title = "Chi tiết Tác giả";
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TACGIA tACGIA = db.TACGIAs.Find(id);
            if (tACGIA == null)
            {
                return HttpNotFound();
            }
            return View(tACGIA);
        }

        // GET: TACGIAs/Create
        public ActionResult Create()
        {
            ViewBag.Title = "Thêm mới Tác giả";
            return View();
        }

        // POST: TACGIAs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaTG,TenTG,QueQuan")] TACGIA tACGIA)
        {
            ViewBag.Title = "Thêm mới Tác giả";
            if (ModelState.IsValid)
            {
                db.TACGIAs.Add(tACGIA);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(tACGIA);
        }

        // GET: TACGIAs/Edit/5
        public ActionResult Edit(int? id)
        {
            ViewBag.Title = "Chỉnh sửa Tác giả";
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TACGIA tACGIA = db.TACGIAs.Find(id);
            if (tACGIA == null)
            {
                return HttpNotFound();
            }
            return View(tACGIA);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaTG,TenTG,QueQuan")] TACGIA tACGIA)
        {
            ViewBag.Title = "Chỉnh sửa Tác giả";
            if (ModelState.IsValid)
            {
                db.Entry(tACGIA).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(tACGIA);
        }

        // GET: TACGIAs/Delete/5
        public ActionResult Delete(int? id)
        {
            ViewBag.Title = "Xóa Tác giả";
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TACGIA tACGIA = db.TACGIAs.Find(id);
            if (tACGIA == null)
            {
                return HttpNotFound();
            }
            return View(tACGIA);
        }

        // POST: TACGIAs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            TACGIA tACGIA = db.TACGIAs.Find(id);
            db.TACGIAs.Remove(tACGIA);
            db.SaveChanges();
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