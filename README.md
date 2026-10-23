# 🌾 Farm Mitra

**Connecting Farmers. Empowering Agriculture.**

Farm Mitra is a farmer product management and support system: a web marketplace where farmers sell directly to buyers, with transparent pricing and no middlemen. An admin verifies every farmer before they can sell.

![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-MVC-512BD4?logo=dotnet)
![C#](https://img.shields.io/badge/C%23-.NET%2010-239120?logo=csharp)
![SQL Server](https://img.shields.io/badge/SQL%20Server-LocalDB-CC2927?logo=microsoftsqlserver)
![License](https://img.shields.io/badge/license-MIT-green)

## 📸 Screenshots

> Add your screenshots to a `screenshots/` folder and update the paths below.

| Home | Marketplace | Savings calculator |
|------|-------------|--------------------|
| ![Home](screenshots/home.png) | ![Marketplace](screenshots/marketplace.png) | ![Calculator](screenshots/calculator.png) |

## ✨ Features

**Farmers**
- Register and wait for admin approval
- List products with price, stock, photo and specifications (grade, size, harvested, shelf life, minimum order, packing)
- Manage orders and chat with buyers
- Earn loyalty tiers (Seedling, Bronze, Silver, Gold) from delivered orders and repeat buyers

**Buyers**
- Browse and filter the marketplace; hover a product image to see its specifications
- View price history, place orders (cash on delivery) and track status from Pending to Delivered
- Chat with farmers

**Admin**
- Approve or reject farmer accounts
- Oversee users, products and orders
- Manage platform settings

**Platform**
- Home page with live farm-price strip, real statistics from the database, savings calculator, seasonal crop calendar and a live farmer spotlight
- Dark and light theme
- Language picker: English plus 21 Indian languages
- Apple-style scroll animations and a looping hero video (respects reduced-motion settings)
- Role-based access with ASP.NET Identity (Admin, Farmer, Buyer)

## 🛠️ Tech Stack

| Layer | Technology |
|-------|------------|
| Framework | ASP.NET Core MVC (.NET 10) with Areas |
| Language | C# |
| Database | SQL Server LocalDB |
| ORM | Entity Framework Core (code-first migrations) |
| Auth | ASP.NET Core Identity, role-based authorization |
| Frontend | Razor, HTML, CSS, JavaScript |
| IDE | Visual Studio 2026 |

## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Visual Studio 2026 (with the *ASP.NET and web development* workload), or any editor with the .NET CLI
- SQL Server LocalDB (installed with Visual Studio)

### Run locally

```bash
# 1. Clone
git clone https://github.com/bhushannikam04/FarmMitra.git
cd FarmMitra/FarmMitra   # the folder that contains Program.cs

# 2. Check the connection string in appsettings.json
#    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=FarmMitra;Trusted_Connection=True;MultipleActiveResultSets=true"

# 3. Create the database
dotnet ef database update

# 4. Run
dotnet run
```

In Visual Studio you can instead open the solution, run `Update-Database` in the **Package Manager Console**, and press **F5**.

> **Tip:** avoid special characters such as `#` in the folder path (for example `D:\c#.net`). The .NET CLI can fail to find the project file there. Package Manager Console in Visual Studio is not affected.

On first start the app seeds the three roles and some demo farmers and products.

### Demo accounts

| Role | Email | Password |
|------|-------|----------|
| Farmer | `rajesh@farmmitra.in` | `Farmer@123` |
| Farmer | `sunita@farmmitra.in` | `Farmer@123` |

These are local demo credentials only. Change or remove them before deploying anywhere public.

## 📁 Project Structure

```
FarmMitra/
├── Areas/            # Admin, Farmer and Buyer areas (controllers + views)
├── Controllers/      # Home, Account, Marketplace
├── Data/             # Seeder
├── Migrations/       # EF Core migrations
├── Models/           # Product, Order, Conversation, PriceHistory, ...
├── Services/         # Settings, weather
├── ViewModels/       # Data shaped for views
├── Views/            # Shared layouts, partials, home and account pages
├── wwwroot/          # css, js, images, videos
├── ApplicationDbContext.cs
├── ApplicationUser.cs
├── Product.cs
└── Program.cs
```

## 🗺️ Roadmap

- [ ] Forgot password by email
- [ ] Online payments (UPI)
- [ ] SMS / WhatsApp order alerts
- [ ] Delivery partner integration
- [ ] Farmer analytics dashboard
- [ ] Mobile app

## 🤝 Contributing

1. Fork the repository
2. Create a branch: `git checkout -b feature/your-feature`
3. Commit your changes: `git commit -m "Add your feature"`
4. Push the branch: `git push origin feature/your-feature`
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License. See the `LICENSE` file for details.

## 👤 Author

**[Your Name]**
- LinkedIn: [your-profile-link]
- GitHub: [@bhushannikam04](https://github.com/bhushannikam04)

---

⭐ If you like this project, give it a star!
