using FarmMitra.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FarmMitra.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        await SeedCoreAsync(services);
        await SeedSampleChatsAsync(services);
        await SeedCompetingListingsAsync(services);
        await BackfillPriceHistoryAsync(services);
        await BackfillSpecsAsync(services);
    }

    private static async Task SeedCoreAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<ApplicationDbContext>();

        // 1. Roles
        foreach (var role in new[] { "Admin", "Farmer", "Buyer" })
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        // 2. Default admin
        await EnsureUserAsync(userManager, "admin@farmmitra.in", "Admin@123", "System Admin", "Admin");
        await EnsureUserAsync(userManager, "buyer@farmmitra.in", "Buyer@123", "Ankita Sharma", "Buyer", "Pune", "Maharashtra");

        // 3. Sample farmers + products (only when the Products table is empty)
        if (await db.Products.AnyAsync()) return;

        var rajesh = await EnsureUserAsync(userManager, "rajesh@farmmitra.in", "Farmer@123", "Rajesh Patil", "Farmer", "Nashik", "Maharashtra", 12, "Onion, Grapes, Potato");
        var sunita = await EnsureUserAsync(userManager, "sunita@farmmitra.in", "Farmer@123", "Sunita More", "Farmer", "Nashik", "Maharashtra", 8, "Grapes, Cabbage");
        var vikram = await EnsureUserAsync(userManager, "vikram@farmmitra.in", "Farmer@123", "Vikram Deshmukh", "Farmer", "Kolhapur", "Maharashtra", 15, "Corn, Tomato");
        var anita  = await EnsureUserAsync(userManager, "anita@farmmitra.in", "Farmer@123", "Anita Jadhav", "Farmer", "Solapur", "Maharashtra", 10, "Pomegranate, Chilli, Mango");

        if (rajesh == null || sunita == null || vikram == null || anita == null) return;

        const string u = "https://images.unsplash.com/";
        const string q = "?w=600&q=80&auto=format&fit=crop";

        db.Products.AddRange(
            P("Fresh Nashik Onion",   "Vegetables", 2500, 120, "Quintal", "Nashik, Maharashtra",   u + "photo-1580201092675-a0a6a6cafbb1" + q, rajesh, "Firm, deep-red onions harvested this week. Graded and sorted by hand."),
            P("Nashik Green Grapes",  "Fruits",     4200,  65, "Quintal", "Nashik, Maharashtra",   u + "photo-1599819055345-b70e5ce7f6c8" + q, sunita, "Seedless green grapes, naturally sweet and picked at peak ripeness."),
            P("Sweet Corn",           "Grains",     1800, 200, "Quintal", "Kolhapur, Maharashtra", u + "photo-1601593768799-76bf3b1c4c2b" + q, vikram, "Tender sweet corn cobs, harvested the same morning they are listed."),
            P("Solapur Pomegranate",  "Fruits",     5600,  40, "Quintal", "Solapur, Maharashtra",  u + "photo-1570913149827-d2ac84ab3f9a" + q, anita,  "Bhagwa pomegranates with deep red arils and a rich, balanced flavour."),
            P("Vine-Ripened Tomato",  "Vegetables", 1600, 150, "Quintal", "Kolhapur, Maharashtra", u + "photo-1592924357228-91a4daadcfea" + q, vikram, "Juicy tomatoes ripened on the vine, ideal for retail and kitchens."),
            P("Farm-Fresh Potato",    "Vegetables", 1400, 300, "Quintal", "Nashik, Maharashtra",   u + "photo-1518977676601-b53f82aba655" + q, rajesh, "Clean, medium-size potatoes with good storage life."),
            P("Green Chilli",         "Vegetables", 3200,   0, "Quintal", "Solapur, Maharashtra",  u + "photo-1587049633312-d628ae50a8ae" + q, anita,  "Medium-hot green chillies. Currently out of stock - next harvest soon.", "OutOfStock"),
            P("Cabbage",              "Vegetables", 1100,  90, "Quintal", "Nashik, Maharashtra",   u + "photo-1571575173700-afb9492e6a50" + q, sunita, "Compact, crisp cabbage heads with tight leaves."),
            P("Alphonso Mango",       "Fruits",     8500,  25, "Quintal", "Solapur, Maharashtra",  u + "photo-1615485500704-8e990f9900f7" + q, anita,  "Premium Alphonso mangoes, naturally ripened, no carbide.")
        );
        await db.SaveChangesAsync();
    }

    // Demo data: one friendly conversation and one reported conversation.
    private static async Task SeedSampleChatsAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        if (await db.Conversations.AnyAsync()) return;

        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var buyer = await users.FindByEmailAsync("buyer@farmmitra.in");
        var rajesh = await users.FindByEmailAsync("rajesh@farmmitra.in");
        var anita = await users.FindByEmailAsync("anita@farmmitra.in");
        if (buyer == null || rajesh == null || anita == null) return;

        var onion = await db.Products.FirstOrDefaultAsync(p => p.FarmerId == rajesh.Id);
        var mango = await db.Products.FirstOrDefaultAsync(p => p.FarmerId == anita.Id);
        var now = DateTime.UtcNow;

        var friendly = new Conversation
        {
            BuyerId = buyer.Id, FarmerId = rajesh.Id, ProductId = onion?.Id,
            CreatedAt = now.AddHours(-5), LastMessageAt = now.AddHours(-1)
        };
        friendly.Messages.Add(new Message { SenderId = buyer.Id, SentAt = now.AddHours(-5), IsRead = true, Body = "Hello! Is this from this week's harvest? I need about 20 quintals." });
        friendly.Messages.Add(new Message { SenderId = rajesh.Id, SentAt = now.AddHours(-4), IsRead = true, Body = "Namaste! Yes, harvested on Monday. 20 quintals is no problem." });
        friendly.Messages.Add(new Message { SenderId = buyer.Id, SentAt = now.AddHours(-2), IsRead = true, Body = "Great, can you deliver to Pune by Friday?" });
        friendly.Messages.Add(new Message { SenderId = rajesh.Id, SentAt = now.AddHours(-1), IsRead = false, Body = "Yes, delivery to Pune takes one day. Place the order and I will confirm it today." });

        var reported = new Conversation
        {
            BuyerId = buyer.Id, FarmerId = anita.Id, ProductId = mango?.Id,
            CreatedAt = now.AddHours(-3), LastMessageAt = now.AddHours(-2),
            IsReported = true, ReportReason = "Sample report (demo data): asked to pay outside the platform"
        };
        reported.Messages.Add(new Message { SenderId = buyer.Id, SentAt = now.AddHours(-3), IsRead = true, Body = "Can I pay you directly instead of through the site? I can offer a small discount." });
        reported.Messages.Add(new Message { SenderId = anita.Id, SentAt = now.AddHours(-2), IsRead = true, Body = "Please keep all orders on Farm Mitra, it protects both of us." });

        db.Conversations.AddRange(friendly, reported);
        await db.SaveChangesAsync();
    }

    // Demo data: a few more sellers of the same crops so price comparisons have something to compare.
    private static async Task SeedCompetingListingsAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();

        var rajesh = await users.FindByEmailAsync("rajesh@farmmitra.in");
        var sunita = await users.FindByEmailAsync("sunita@farmmitra.in");
        var vikram = await users.FindByEmailAsync("vikram@farmmitra.in");
        var anita = await users.FindByEmailAsync("anita@farmmitra.in");
        if (rajesh == null || sunita == null || vikram == null || anita == null) return;

        const string u = "https://images.unsplash.com/";
        const string q = "?w=600&q=80&auto=format&fit=crop";

        var samples = new[]
        {
            P("Red Onion",       "Vegetables", 2700, 80,  "Quintal", "Kolhapur, Maharashtra", u + "photo-1580201092675-a0a6a6cafbb1" + q, vikram, "Red onions, hand sorted."),
            P("Onion Grade A",   "Vegetables", 2200, 150, "Quintal", "Nashik, Maharashtra",   u + "photo-1580201092675-a0a6a6cafbb1" + q, sunita, "Large, firm onions."),
            P("Hybrid Tomato",   "Vegetables", 1900, 60,  "Quintal", "Nashik, Maharashtra",   u + "photo-1592924357228-91a4daadcfea" + q, rajesh, "Uniform hybrid tomatoes."),
            P("Table Potato",    "Vegetables", 15,   4000, "Kg",     "Nashik, Maharashtra",   u + "photo-1518977676601-b53f82aba655" + q, sunita, "Sold per kg. Good for chips and curries."),
            P("Pomegranate Premium", "Fruits", 6200, 20,  "Quintal", "Solapur, Maharashtra",  u + "photo-1570913149827-d2ac84ab3f9a" + q, anita, "Large premium-grade pomegranates.")
        };

        foreach (var s in samples)
            if (!await db.Products.AnyAsync(p => p.Name == s.Name && p.FarmerId == s.FarmerId))
                db.Products.Add(s);

        await db.SaveChangesAsync();
    }

    // Gives every product at least one price-history entry (its current price).
    private static async Task BackfillPriceHistoryAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();

        var missing = await db.Products
            .Where(p => !db.PriceHistories.Any(h => h.ProductId == p.Id))
            .Select(p => new { p.Id, p.Price })
            .ToListAsync();

        if (missing.Count == 0) return;

        db.PriceHistories.AddRange(missing.Select(m => new PriceHistory { ProductId = m.Id, Price = m.Price, ChangedAt = DateTime.UtcNow }));
        await db.SaveChangesAsync();
    }

    // Gives the demo farmers' sample products some specifications so the hover panel has something to show.
    // Only products of @farmmitra.in demo accounts that have no specifications yet are touched.
    private static async Task BackfillSpecsAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var demo = await db.Products
            .Where(p => p.Grade == null && p.Size == null && p.Harvested == null && p.ShelfLife == null
                        && p.MinOrder == null && p.Packing == null && p.Farmer!.Email!.EndsWith("@farmmitra.in"))
            .ToListAsync();
        if (demo.Count == 0) return;

        foreach (var p in demo)
        {
            switch (p.Category)
            {
                case "Fruits":
                    p.Grade = "A, export quality"; p.Size = "55-65 mm"; p.ShelfLife = "30-45 days"; p.MinOrder = "5 quintal"; p.Packing = "20 kg crates"; break;
                case "Grains":
                case "Pulses":
                    p.Grade = "Premium"; p.Size = "Uniform"; p.ShelfLife = "6-12 months"; p.MinOrder = "10 quintal"; p.Packing = "50 kg PP bags"; break;
                case "Spices":
                    p.Grade = "A, sun-dried"; p.Size = "Whole"; p.ShelfLife = "8-12 months"; p.MinOrder = "1 quintal"; p.Packing = "25 kg sacks"; break;
                default:
                    p.Grade = "A, fresh"; p.Size = "Medium"; p.ShelfLife = "7-14 days"; p.MinOrder = "2 quintal"; p.Packing = "50 kg jute bags"; break;
            }
            p.Harvested = "This week";
        }
        await db.SaveChangesAsync();
    }

    private static Product P(string name, string cat, decimal price, int qty, string unit, string loc,
        string img, ApplicationUser farmer, string desc, string status = "Available") => new()
    {
        Name = name, Category = cat, Price = price, Quantity = qty, Unit = unit, Location = loc,
        ImagePath = img, FarmerId = farmer.Id, Description = desc, Status = status
    };

    private static async Task<ApplicationUser?> EnsureUserAsync(UserManager<ApplicationUser> um,
        string email, string password, string fullName, string role,
        string? city = null, string? state = null, int? years = null, string? crops = null)
    {
        var user = await um.FindByEmailAsync(email);
        if (user != null) return user;

        user = new ApplicationUser
        {
            UserName = email, Email = email, FullName = fullName, EmailConfirmed = true,
            IsApproved = true, City = city, State = state, ExperienceYears = years, CropsGrown = crops
        };
        var result = await um.CreateAsync(user, password);
        if (!result.Succeeded) return null;

        await um.AddToRoleAsync(user, role);
        return user;
    }
}
