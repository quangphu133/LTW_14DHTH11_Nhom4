using System;
using System.Linq;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class UsersController : Controller
    {
        private QLNHASACHEntities db = new QLNHASACHEntities();

        // GET: Login
        public ActionResult Login()
        {
            return View();
        }

        // POST: Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = db.Users.FirstOrDefault(u => u.Username == model.Username && u.Password == model.Password);

                if (user != null)
                {
                    // 1. Lưu Session cơ bản
                    Session["UserID"] = user.UserID;
                    Session["Username"] = user.Username;
                    Session["Role"] = user.Role;
                    Session["FullName"] = user.FullName;

                    // 2. Phân loại và xử lý
                    if (user.Role == "NhanVien")
                    {
                        Session["MaNV"] = user.MaNV;
                        return RedirectToAction("Index", "PHIEUNHAPs", new { area = "Admin" });
                    }
                    else // Là Khách hàng
                    {
                        Session["MaKH"] = user.MaKH;

                        // --- ĐỒNG BỘ CHO THANH TOÁN ---
                        // Lấy đầy đủ thông tin khách hàng từ bảng KHACHHANG
                        var khachHangInfo = db.KHACHHANGs.FirstOrDefault(k => k.MaKH == user.MaKH);
                        if (khachHangInfo != null)
                        {
                            Session["TaiKhoan"] = khachHangInfo;
                        }

                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng!";
                }
            }
            return View(model);
        }

        // GET: Register
        public ActionResult Register()
        {
            return View();
        }

        // POST: Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (db.Users.Any(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập này đã có người dùng.");
                    return View(model);
                }

                if (db.KHACHHANGs.Any(k => k.SDT == model.Phone))
                {
                    ModelState.AddModelError("Phone", "Số điện thoại này đã được đăng ký.");
                    return View(model);
                }

                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var newKhach = new KHACHHANG
                        {
                            HoTen = model.FullName,
                            Email = model.Email,
                            SDT = model.Phone,
                            DiaChi = "",
                            DiemTichLuy = 0
                        };
                        db.KHACHHANGs.Add(newKhach);
                        db.SaveChanges();

                        var newUser = new User
                        {
                            Username = model.Username,
                            Password = model.Password,
                            FullName = model.FullName,
                            Email = model.Email,
                            Role = "KhachHang",
                            MaKH = newKhach.MaKH,
                            MaNV = null
                        };

                        db.Users.Add(newUser);
                        db.SaveChanges();

                        transaction.Commit();

                        TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                        return RedirectToAction("Login");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        ViewBag.Error = "Có lỗi xảy ra: " + ex.Message;
                    }
                }
            }
            return View(model);
        }

        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login", "Users");
        }
    }
}