﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using AutoMapper;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using SimpleBlog.DAL.Context;
using SimpleBlog.DAL.DataService;
using SimpleBlog.DAL.Identity;
using SimpleBlog.DAL.Object_Model;
using SimpleBlog.DAL.ViewModel;

namespace SimpleBlog.WebUI.Areas.Admin.Controllers
{
    [Authorize(Roles = "admin")]
    public class UserController : Controller
    {
        private UnitOfWork _unitOfWork;

        public UserController()
        {
            _unitOfWork = new UnitOfWork(ApplicationContext.Create());
        }

        public ActionResult List()
        {
            var users = _unitOfWork.DataContext.Users.ToList();
            if (users.Any())
            {
                return View(Mapper.Map<IEnumerable<ApplicationUser>, IEnumerable<UserViewModel>>(users));
            }
            return View();
        }

        [HttpGet]
        public ActionResult Edit(string id)
        {
            var user = _unitOfWork.DataContext.Users.Find(id);
            if (user != null)
            {
                return View(user);
            }
            return HttpNotFound();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ApplicationUser model)
        {
            if (ModelState.IsValid)
            {
                var passwordHash = new PasswordHasher();
                if (!string.IsNullOrWhiteSpace(model.NewPassword))
                {
                    model.PasswordHash = passwordHash.HashPassword(model.NewPassword);
                }
                _unitOfWork.DataContext.Entry(model).State = EntityState.Modified;
                _unitOfWork.Save();
                return RedirectToAction("LogOff", "Account", new { @area = "" });
            }
            return RedirectToAction("List");
        }

        [HttpGet]
        public ActionResult Remove(string id)
        {
            if (_unitOfWork.DataContext.Users.Count() > 1)
            {
                var user = _unitOfWork.DataContext.Users.Find(id);
                if (user != null)
                {
                    return View(user);
                }
                ModelState.AddModelError("OnlyOneUser", "В системе есть только один пользователь. Удаление невозможно");
                return RedirectToAction("List");
            }
            return RedirectToAction("List");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Remove(ApplicationUser model)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.DataContext.Entry(model).State = EntityState.Deleted;
                _unitOfWork.Save();
                return RedirectToAction("List");
            }
            return RedirectToAction("List");
        }


        [HttpGet]
        public ActionResult Add()
        {
            return View(new UserViewModel());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Add(UserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var manager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(_unitOfWork.DataContext));

                var user = new ApplicationUser()
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    EmailConfirmed = true,
                    FullName = model.FullName
                };

                var roleManager = new RoleManager<ApplicationRole>(new RoleStore<ApplicationRole>(_unitOfWork.DataContext));

                var role = await roleManager.FindByNameAsync("roleAdmin");
                var result = await manager.CreateAsync(user, model.PasswordHash);
                if (result.Succeeded && role != null)
                {
                    manager.AddToRole(user.Id, role.Name);
                }
                else
                {
                    throw new NullReferenceException();
                }

                return RedirectToAction("List");
            }
            return HttpNotFound();
        }
    }
}