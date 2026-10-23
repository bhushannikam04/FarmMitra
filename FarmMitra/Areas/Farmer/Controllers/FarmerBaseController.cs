using FarmMitra.Data;
using FarmMitra.Models;
using FarmMitra.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FarmMitra.Areas.Farmer.Controllers;

[Area("Farmer")]
[Authorize(Roles = "Farmer")]
public abstract class FarmerBaseController : Controller
{
    protected readonly ApplicationDbContext Db;
    private readonly UserManager<ApplicationUser> _users;

    protected ApplicationUser CurrentUser { get; private set; } = null!;

    protected FarmerBaseController(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        Db = db;
        _users = users;
    }

    // A farmer whose approval was revoked must not keep using an old login session.
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = await _users.GetUserAsync(User);
        if (user == null || !user.IsApproved)
        {
            context.Result = RedirectToAction("PendingApproval", "Account", new { area = "", rejected = user?.ApprovalStatus == "Rejected" });
            return;
        }

        CurrentUser = user;
        await next();
    }

    // Order lines that belong to this farmer's products.
    protected IQueryable<OrderItem> MyOrderItems()
    {
        var uid = CurrentUser.Id;
        return Db.OrderItems.Where(i => i.Product!.FarmerId == uid);
    }

    protected IQueryable<FarmerOrderRow> ToOrderRows(IQueryable<OrderItem> items)
    {
        var uid = CurrentUser.Id;
        return items.Select(i => new FarmerOrderRow
        {
            OrderId = i.OrderId,
            ProductName = i.Product!.Name,
            ProductImage = i.Product.ImagePath,
            Unit = i.Product.Unit,
            BuyerName = i.Order!.Buyer!.FullName,
            Quantity = i.Quantity,
            Amount = i.Quantity * i.UnitPrice,
            OrderDate = i.Order.OrderDate,
            Status = i.Order.Status,
            OnlyMine = !i.Order.Items.Any(x => x.Product!.FarmerId != uid)
        });
    }

    protected IActionResult Back(string? returnUrl, string fallbackAction) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(fallbackAction);
}
