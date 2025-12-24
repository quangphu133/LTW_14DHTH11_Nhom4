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
    public class NHANVIENsController : BaseAdminController
    {
        private QLNHASACHEntities db = new QLNHASACHEntities();

        // --- HIỂN THỊ DANH SÁCH NHÂN VIÊN ---
        public ActionResult Index()
        {
            return View(db.NHANVIENs.ToList());
        }

        // --- XEM CHI TIẾT ---
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            NHANVIEN nHANVIEN = db.NHANVIENs.Find(id);
            if (nHANVIEN == null) return HttpNotFound();
            return View(nHANVIEN);
        }

        // --- THÊM NHÂN VIÊN MỚI ---
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaNV,HoTen,NgaySinh,SDT,ChucVu")] NHANVIEN nhanVien, string username, string password)
        {
            if (ModelState.IsValid)
            {
                using (var scope = db.Database.BeginTransaction())
                {
                    try
                    {
                        // Kiểm tra xem username đã có người dùng chưa
                        var checkUser = db.Users.FirstOrDefault(u => u.Username == username);
                        if (checkUser != null)
                        {
                            ModelState.AddModelError("", "Tên đăng nhập này đã tồn tại!");
                            return View(nhanVien);
                        }

                        // Lưu thông tin nhân viên vào bảng NHANVIEN
                        db.NHANVIENs.Add(nhanVien);
                        db.SaveChanges(); // Lưu ngay để lấy MaNV vừa sinh ra

                        // Tạo tài khoản bên bảng Users
                        User newUser = new User();
                        newUser.Username = username;
                        newUser.Password = password; // Chỉ lưu mật khẩu ở đây
                        newUser.FullName = nhanVien.HoTen; // Lấy tên nhân viên qua cho tiện
                        newUser.Role = "NhanVien";         // Gán quyền mặc định

                        // Liên kết User này với Nhân viên vừa tạo
                        newUser.MaNV = nhanVien.MaNV;

                        db.Users.Add(newUser);
                        db.SaveChanges();

                        // Xác nhận giao dịch thành công
                        scope.Commit();

                        // Quay về trang danh sách
                        return RedirectToAction("Index");
                    }
                    catch (Exception ex)
                    {
                        scope.Rollback();
                        ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                    }
                }
            }

            return View(nhanVien);
        }

        // --- SỬA THÔNG TIN (EDIT) ---
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NHANVIEN nhanVien = db.NHANVIENs.Find(id);
            if (nhanVien == null)
            {
                return HttpNotFound();
            }
            // Tìm user có MaNV trùng với nhân viên đang sửa
            var userAccount = db.Users.FirstOrDefault(u => u.MaNV == id);

            // Đẩy username sang View để hiển thị 
            ViewBag.Username = userAccount != null ? userAccount.Username : "";
            // -------------------------------------------------------------

            return View(nhanVien);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaNV,HoTen,NgaySinh,SDT,ChucVu")] NHANVIEN nhanVien, string username, string newPassword)
        {
            if (ModelState.IsValid)
            {
                // --- Cập nhật cả 2 bảng ---
                using (var scope = db.Database.BeginTransaction())
                {
                    try
                    {
                        // 1. Cập nhật bảng NHANVIEN
                        db.Entry(nhanVien).State = EntityState.Modified;

                        // 2. Cập nhật bảng Users
                        var userAccount = db.Users.FirstOrDefault(u => u.MaNV == nhanVien.MaNV);

                        if (userAccount != null)
                        {
                            // Cập nhật Username
                            if (userAccount.Username != username)
                            {
                                var checkExist = db.Users.FirstOrDefault(u => u.Username == username && u.MaNV != nhanVien.MaNV);
                                if (checkExist != null)
                                {
                                    ModelState.AddModelError("", "Tên đăng nhập mới bị trùng với người khác!");
                                    scope.Rollback();
                                    ViewBag.Username = username; // Giữ lại giá trị vừa nhập
                                    return View(nhanVien);
                                }
                                userAccount.Username = username;
                            }

                            // Cập nhật Password (chỉ cập nhật nếu người dùng có nhập mật khẩu mới)
                            if (!string.IsNullOrEmpty(newPassword))
                            {
                                userAccount.Password = newPassword;
                            }

                            // Cập nhật lại Họ tên User cho khớp với Nhân viên
                            userAccount.FullName = nhanVien.HoTen;
                        }

                        db.SaveChanges();
                        scope.Commit(); // Lưu thành công

                        return RedirectToAction("Index");
                    }
                    catch (Exception ex)
                    {
                        scope.Rollback();
                        ModelState.AddModelError("", "Lỗi cập nhật: " + ex.Message);
                    }
                }
            }

            // Nếu lỗi validate, trả lại view kèm username cũ
            ViewBag.Username = username;
            return View(nhanVien);
        }

        // --- XÓA NHÂN VIÊN (DELETE) ---
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            NHANVIEN nHANVIEN = db.NHANVIENs.Find(id);
            if (nHANVIEN == null) return HttpNotFound();
            return View(nHANVIEN);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            // Tìm nhân viên cần xóa
            NHANVIEN nhanVien = db.NHANVIENs.Find(id);

            // Tìm tất cả user có MaNV tương ứng 
            var relatedUsers = db.Users.Where(u => u.MaNV == id).ToList();

            if (relatedUsers.Count > 0)
            {
                // Xóa hết các user liên quan này
                db.Users.RemoveRange(relatedUsers);
            }

            // Sau khi đã dọn dẹp User, tiến hành xóa Nhân viên
            db.NHANVIENs.Remove(nhanVien);

            // Lưu thay đổi vào Database
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}