Farm Mitra - Step 13 (Profile & Settings)
1. Copy these folders into your FarmMitra project (Replace when asked).
2. ApplicationDbContext.cs: add   public DbSet<PlatformSetting> PlatformSettings { get; set; } = null!;
3. Program.cs: add   builder.Services.AddScoped<SettingsService>();
4. From the folder containing FarmMitra.csproj:
   dotnet ef migrations add AddPlatformSettings
   dotnet ef database update
