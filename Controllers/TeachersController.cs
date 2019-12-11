﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using School.Data;
using School.Models;

namespace School.Controllers
{

    [Authorize(Roles = "Admin")]
    public class TeachersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IHostingEnvironment _env;

        public TeachersController(UserManager<ApplicationUser> manager, ApplicationDbContext context, RoleManager<ApplicationRole> rolemanager, IHostingEnvironment env)
        {
            _userManager = manager;
            _context = context;
            _roleManager = rolemanager;
            _env = env;
        }
        public ActionResult Index()
        {
            var teachers = _context.Teachers;
            ViewData["isToast"] = false;
            return View(teachers);
        }

        public IActionResult Details(string id)
        {
            var teacher = _context.Teachers.Find(id);
            return View(teacher);
        }

        public ActionResult Create()
        {
            ViewBag.ClassId = new SelectList(_context.Classes, "Id", "Name");
            var model = new CreateUserViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateUserViewModel model, IFormFile Avatar)
        {
            if (!ModelState.IsValid)
            {
                return View("Create", model);
            }
            ViewData["isToast"] = true;
            var createresult = await _userManager.CreateAsync(model.Teacher, model.Password);
            var teachers = _context.Teachers;
            var notification = new Notification();
            if (!createresult.Succeeded)
            {
                notification.Text = "Error";
                notification.Text = "Error ecountered while registering student";
                notification.Type = "error";
                return RedirectToAction("Index", teachers);
            }
            //create student role if it doesn't exist
            if (_context.Roles.SingleOrDefault(role => role.Name == RoleNames.Student) == null)
            {
                await _roleManager.CreateAsync(new ApplicationRole(RoleNames.Student));
            }
            string ImageExtension = Avatar.FileName.Split('.').Last();
            model.Student.ProfilePhotoExtension = ImageExtension;
            var addtoroleresult = await _userManager.AddToRoleAsync(model.Student, RoleNames.Student);
            if (!addtoroleresult.Succeeded)
            {
                notification.Title = "Error";
                notification.Text = "Error ecountered while registering student";
                notification.Type = "error";
                await _userManager.DeleteAsync(model.Student);
                return RedirectToAction("Index", teachers);
            }
            string AvatarPath = Path.Combine(_env.WebRootPath, "Images", "Avatars");
            Directory.CreateDirectory(AvatarPath);
            var stream = new FileStream(Path.Combine(AvatarPath, model.Student.Id.ToString() + '.' + ImageExtension), FileMode.CreateNew, FileAccess.ReadWrite);
            await Avatar.CopyToAsync(stream);
            stream.Close();
            notification.Title = "Registration succesful";
            notification.Text = model.Student.FirstName + " " + model.Student.MiddleName + " successfully registered";
            notification.Type = "success";
            return RedirectToActionPermanent("Index", notification);
        }

        public ActionResult Edit(string id)
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Teacher teacher)
        {
            var teacherInDb = _context.Teachers.Find(teacher.Id);
            teacherInDb.FirstName = teacher.FirstName;
            teacherInDb.MiddleName = teacher.MiddleName;
            teacherInDb.LastName = teacher.LastName;
            teacherInDb.Email = teacher.Email;
            teacherInDb.DOB = teacher.DOB;
            teacherInDb.Address = teacher.Address;
            teacherInDb.ClassId = teacher.ClassId;
            teacherInDb.PhoneNumber = teacher.PhoneNumber;
            teacherInDb.EmploymentDate = teacher.EmploymentDate;

            await _userManager.UpdateAsync(teacherInDb);
            var notification = new Notification()
            {
                Title = "Update successfull",
                Text = teacher.FirstName + " " + teacher.MiddleName + " updated successfully",
                Type = "success"
            };
            return RedirectToAction("Index", notification);
        }

        public ActionResult Delete(string id)
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string id, IFormCollection collection)
        {
            try
            {

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}