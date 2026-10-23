using FarmMitra.Data;
using FarmMitra.Models;
using FarmMitra.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmMitra.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public abstract class AdminBaseController : Controller
{
    protected readonly ApplicationDbContext Db;

    protected AdminBaseController(ApplicationDbContext db) => Db = db;

    // Ids of every user that belongs to a role (Farmer, Buyer, ...)
    protected IQueryable<string> UserIdsInRole(string role) =>
        from ur in Db.UserRoles
        join r in Db.Roles on ur.RoleId equals r.Id
        where r.Name == role
        select ur.UserId;

    protected IQueryable<UserRow> ToRows(IQueryable<ApplicationUser> users) =>
        users.Select(u => new UserRow
        {
            Id = u.Id,
            FullName = u.FullName,
            ProfileImagePath = u.ProfileImagePath,
            Email = u.Email,
            Phone = u.PhoneNumber,
            City = u.City,
            State = u.State,
            FarmLocation = u.FarmLocation,
            CropsGrown = u.CropsGrown,
            FarmSizeAcres = u.FarmSizeAcres,
            ExperienceYears = u.ExperienceYears,
            CreatedAt = u.CreatedAt,
            ApprovalStatus = u.ApprovalStatus,
            IsApproved = u.IsApproved,
            ProductCount = Db.Products.Count(p => p.FarmerId == u.Id),
            OrderCount = Db.Orders.Count(o => o.BuyerId == u.Id)
        });

    // Go back to the page the admin came from (keeps search / filters / page).
    protected IActionResult Back(string? returnUrl, string fallbackAction) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(fallbackAction);
}
