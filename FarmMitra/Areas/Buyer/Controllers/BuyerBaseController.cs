using FarmMitra.Data;
using FarmMitra.Models;
using FarmMitra.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FarmMitra.Areas.Buyer.Controllers;

[Area("Buyer")]
[Authorize(Roles = "Buyer")]
public abstract class BuyerBaseController : Controller
{
    protected readonly ApplicationDbContext Db;
    private readonly UserManager<ApplicationUser> _users;

    protected ApplicationUser CurrentUser { get; private set; } = null!;

    protected BuyerBaseController(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        Db = db;
        _users = users;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = await _users.GetUserAsync(User);
        if (user == null)
        {
            context.Result = RedirectToAction("Login", "Account", new { area = "" });
            return;
        }

        CurrentUser = user;
        await next();
    }

    protected IQueryable<OrderListRow> ToOrderRows(IQueryable<Order> orders) =>
        orders.Select(o => new OrderListRow
        {
            Id = o.Id,
            OrderDate = o.OrderDate,
            Status = o.Status,
            Total = o.Total,
            ItemCount = o.Items.Count,
            FirstProduct = o.Items.OrderBy(i => i.Id).Select(i => i.Product!.Name).FirstOrDefault()
        });

    protected IActionResult Back(string? returnUrl, string fallbackAction, string? fallbackController = null) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(fallbackAction, fallbackController);
}
