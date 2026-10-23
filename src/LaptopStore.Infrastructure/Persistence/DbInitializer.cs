using LaptopStore.Domain.Entities;
using LaptopStore.Domain.Enums;
using LaptopStore.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(LaptopStoreDbContext context, IPasswordHasher passwordHasher)
    {
        // Đảm bảo database đã được tạo
        await context.Database.EnsureCreatedAsync();

        // 1. Seed Users (Admin, Manager, Staff, Warehouse)
        if (!await context.Users.AnyAsync())
        {
            var users = new List<User>
            {
                new()
                {
                    Username = "admin",
                    PasswordHash = passwordHasher.HashPassword("Admin@123"),
                    FullName = "Nguyễn Văn Quản Trị",
                    Email = "admin@laptopstore.vn",
                    PhoneNumber = "0901234567",
                    Role = UserRole.Admin,
                    CreatedAt = DateTime.UtcNow.AddMonths(-6)
                },
                new()
                {
                    Username = "manager",
                    PasswordHash = passwordHasher.HashPassword("Manager@123"),
                    FullName = "Trần Thị Giám Sát",
                    Email = "manager@laptopstore.vn",
                    PhoneNumber = "0912345678",
                    Role = UserRole.Manager,
                    CreatedAt = DateTime.UtcNow.AddMonths(-5)
                },
                new()
                {
                    Username = "staff",
                    PasswordHash = passwordHasher.HashPassword("Staff@123"),
                    FullName = "Lê Hoàng Bán Hàng",
                    Email = "staff@laptopstore.vn",
                    PhoneNumber = "0923456789",
                    Role = UserRole.SalesStaff,
                    CreatedAt = DateTime.UtcNow.AddMonths(-4)
                },
                new()
                {
                    Username = "warehouse",
                    PasswordHash = passwordHasher.HashPassword("Warehouse@123"),
                    FullName = "Phạm Đình Quản Kho",
                    Email = "warehouse@laptopstore.vn",
                    PhoneNumber = "0934567890",
                    Role = UserRole.WarehouseStaff,
                    CreatedAt = DateTime.UtcNow.AddMonths(-3)
                }
            };
            context.Users.AddRange(users);
            await context.SaveChangesAsync();
        }

        // 2. Seed Categories (5 danh mục chuẩn theo đề bài)
        if (!await context.Categories.AnyAsync())
        {
            var categories = new List<Category>
            {
                new()
                {
                    Name = "Laptop Gaming",
                    Slug = "laptop-gaming",
                    Description = "Dòng máy hiệu năng đỉnh cao, card đồ họa rời RTX, tản nhiệt buồng hơi cho game thủ và lập trình viên nặng.",
                    DisplayOrder = 1
                },
                new()
                {
                    Name = "Laptop Doanh Nhân & Mỏng Nhẹ",
                    Slug = "laptop-doanh-nhan-mong-nhe",
                    Description = "Thiết kế kim loại siêu mỏng nhẹ, pin từ 10-18 tiếng, màn hình OLED độ nét cao, chuẩn Intel Evo.",
                    DisplayOrder = 2
                },
                new()
                {
                    Name = "Laptop Đồ Họa & Trạm (Workstation)",
                    Slug = "laptop-do-hoa-workstation",
                    Description = "Dành riêng cho kỹ sư thiết kế CAD, render 3D, kiến trúc và xử lý dữ liệu lớn chuyên nghiệp.",
                    DisplayOrder = 3
                },
                new()
                {
                    Name = "Linh Kiện Máy Tính & Nâng Cấp",
                    Slug = "linh-kien-may-tinh",
                    Description = "RAM DDR5 chính hãng, Ổ cứng SSD NVMe PCIe 4.0/5.0 tốc độ cao, Card đồ họa VGA rời.",
                    DisplayOrder = 4
                },
                new()
                {
                    Name = "Phụ Kiện & Gaming Gear",
                    Slug = "phu-kien-gaming-gear",
                    Description = "Bàn phím cơ không dây, Chuột công thái học, Balo chống sốc, Đế tản nhiệt cao cấp.",
                    DisplayOrder = 5
                }
            };
            context.Categories.AddRange(categories);
            await context.SaveChangesAsync();
        }

        // 3. Seed Products (24 sản phẩm laptop và linh kiện đa dạng)
        if (!await context.Products.AnyAsync())
        {
            var catGaming = await context.Categories.FirstAsync(c => c.Slug == "laptop-gaming");
            var catUltrabook = await context.Categories.FirstAsync(c => c.Slug == "laptop-doanh-nhan-mong-nhe");
            var catWorkstation = await context.Categories.FirstAsync(c => c.Slug == "laptop-do-hoa-workstation");
            var catComponents = await context.Categories.FirstAsync(c => c.Slug == "linh-kien-may-tinh");
            var catGear = await context.Categories.FirstAsync(c => c.Slug == "phu-kien-gaming-gear");

            var products = new List<Product>
            {
                // Laptop Gaming
                new()
                {
                    Sku = "LAP-ROG-G16-01",
                    Name = "ASUS ROG Zephyrus G16 (2026)",
                    Brand = "ASUS",
                    Model = "Zephyrus G16 GU605",
                    Description = "Laptop gaming mỏng nhẹ cao cấp nhất của Asus, vỏ nhôm CNC nguyên khối với dải đèn Slash Lighting độc bản.",
                    ImageUrl = "https://images.unsplash.com/photo-1603302576837-37561b2e2302?w=800",
                    Price = 52990000,
                    CostPrice = 45000000,
                    StockQuantity = 12,
                    MinStockLevel = 3,
                    Cpu = "Intel Core Ultra 9 185H (16 Cores, 22 Threads)",
                    Ram = "32GB LPDDR5X 7467MHz",
                    Storage = "1TB PCIe 4.0 NVMe M.2 SSD",
                    Gpu = "NVIDIA GeForce RTX 4070 8GB GDDR6",
                    ScreenSize = "16.0 inch 2.5K 240Hz ROG Nebula OLED",
                    OperatingSystem = "Windows 11 Home",
                    WeightKg = 1.85m,
                    WarrantyMonths = 24,
                    CategoryId = catGaming.Id
                },
                new()
                {
                    Sku = "LAP-LEG-P5-02",
                    Name = "Lenovo Legion Pro 5 16IRX9",
                    Brand = "Lenovo",
                    Model = "Legion Pro 5",
                    Description = "Quái vật tản nhiệt Coldfront 5.0, màn hình màu chuẩn 100% sRGB cho game thủ chuyên nghiệp và coder.",
                    ImageUrl = "https://images.unsplash.com/photo-1593642632823-8f785ba67e45?w=800",
                    Price = 38490000,
                    CostPrice = 32000000,
                    StockQuantity = 15,
                    MinStockLevel = 4,
                    Cpu = "Intel Core i7-14700HX (20 Cores, 28 Threads)",
                    Ram = "16GB DDR5 5600MHz (2 khe nâng cấp)",
                    Storage = "512GB PCIe 4.0 SSD",
                    Gpu = "NVIDIA GeForce RTX 4060 8GB 140W",
                    ScreenSize = "16.0 inch WQXGA 165Hz IPS 500 nits",
                    OperatingSystem = "Windows 11 Home",
                    WeightKg = 2.50m,
                    WarrantyMonths = 24,
                    CategoryId = catGaming.Id
                },
                new()
                {
                    Sku = "LAP-HEL-N16-03",
                    Name = "Acer Predator Helios Neo 16",
                    Brand = "Acer",
                    Model = "PHN16-72",
                    Description = "Dòng laptop gaming quốc dân cấu hình cực mạnh, tản nhiệt kim loại lỏng thế hệ mới.",
                    ImageUrl = "https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=800",
                    Price = 32990000,
                    CostPrice = 28000000,
                    StockQuantity = 8,
                    MinStockLevel = 3,
                    Cpu = "Intel Core i7-14650HX",
                    Ram = "16GB DDR5 5600MHz",
                    Storage = "512GB PCIe NVMe SSD",
                    Gpu = "NVIDIA GeForce RTX 4060 8GB",
                    ScreenSize = "16.0 inch WQXGA 240Hz sRGB 100%",
                    OperatingSystem = "Windows 11 Home",
                    WeightKg = 2.60m,
                    WarrantyMonths = 12,
                    CategoryId = catGaming.Id
                },
                new()
                {
                    Sku = "LAP-MSI-K15-04",
                    Name = "MSI Katana 15 B13VFK",
                    Brand = "MSI",
                    Model = "Katana 15",
                    Description = "Bàn phím đèn RGB 4 vùng phong cách kiếm sĩ Samurai, hiệu năng vượt tầm giá.",
                    ImageUrl = "https://images.unsplash.com/photo-1541807084-5c52b6b3adef?w=800",
                    Price = 25990000,
                    CostPrice = 21500000,
                    StockQuantity = 4, // Tồn kho thấp
                    MinStockLevel = 5,
                    Cpu = "Intel Core i7-13620H",
                    Ram = "16GB DDR5 5200MHz",
                    Storage = "1TB PCIe NVMe SSD",
                    Gpu = "NVIDIA GeForce RTX 4060 8GB",
                    ScreenSize = "15.6 inch FHD 144Hz IPS",
                    OperatingSystem = "Windows 11 Home",
                    WeightKg = 2.25m,
                    WarrantyMonths = 24,
                    CategoryId = catGaming.Id
                },
                new()
                {
                    Sku = "LAP-ALIEN-M16-05",
                    Name = "Dell Alienware m16 R2",
                    Brand = "Dell",
                    Model = "m16 R2",
                    Description = "Huyền thoại laptop ngoài hành tinh Alienware, vỏ hợp kim magie huyền bí Legend 3.",
                    ImageUrl = "https://images.unsplash.com/photo-1525547719571-a2d4ac8945e2?w=800",
                    Price = 64990000,
                    CostPrice = 55000000,
                    StockQuantity = 5,
                    MinStockLevel = 2,
                    Cpu = "Intel Core Ultra 9 185H",
                    Ram = "32GB DDR5 5600MHz",
                    Storage = "1TB PCIe 4.0 SSD",
                    Gpu = "NVIDIA GeForce RTX 4080 12GB",
                    ScreenSize = "16.0 inch QHD+ 240Hz ComfortView Plus",
                    OperatingSystem = "Windows 11 Home",
                    WeightKg = 2.61m,
                    WarrantyMonths = 24,
                    CategoryId = catGaming.Id
                },

                // Laptop Doanh Nhân & Mỏng Nhẹ
                new()
                {
                    Sku = "LAP-MAC-MBP14-06",
                    Name = "Apple MacBook Pro 14 M3 Pro",
                    Brand = "Apple",
                    Model = "MacBook Pro 14 (Late 2023/2026)",
                    Description = "Chip Apple M3 Pro siêu tiết kiệm điện, màn hình Liquid Retina XDR độ sáng tối đa 1600 nits đỉnh cao thế giới.",
                    ImageUrl = "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=800",
                    Price = 49990000,
                    CostPrice = 43000000,
                    StockQuantity = 14,
                    MinStockLevel = 3,
                    Cpu = "Apple M3 Pro (11-core CPU, 14-core GPU)",
                    Ram = "18GB Unified Memory",
                    Storage = "512GB SSD siêu tốc",
                    Gpu = "Apple 14-core GPU tích hợp",
                    ScreenSize = "14.2 inch Liquid Retina XDR 120Hz ProMotion",
                    OperatingSystem = "macOS Sonoma",
                    WeightKg = 1.61m,
                    WarrantyMonths = 12,
                    CategoryId = catUltrabook.Id
                },
                new()
                {
                    Sku = "LAP-MAC-AIR13-07",
                    Name = "Apple MacBook Air 13 M3 (8GB/256GB)",
                    Brand = "Apple",
                    Model = "MacBook Air 13 M3",
                    Description = "Mỏng chỉ 11.3mm, hoàn toàn không quạt tản nhiệt yên tĩnh tuyệt đối, thời lượng pin cả ngày 18 tiếng.",
                    ImageUrl = "https://images.unsplash.com/photo-1611186871348-b1ce696e52c9?w=800",
                    Price = 27490000,
                    CostPrice = 24000000,
                    StockQuantity = 20,
                    MinStockLevel = 5,
                    Cpu = "Apple M3 (8-core CPU, 8-core GPU)",
                    Ram = "8GB Unified Memory",
                    Storage = "256GB SSD",
                    Gpu = "Apple 8-core GPU",
                    ScreenSize = "13.6 inch Liquid Retina 500 nits",
                    OperatingSystem = "macOS Sonoma",
                    WeightKg = 1.24m,
                    WarrantyMonths = 12,
                    CategoryId = catUltrabook.Id
                },
                new()
                {
                    Sku = "LAP-DELL-XPS15-08",
                    Name = "Dell XPS 15 9530 OLED",
                    Brand = "Dell",
                    Model = "XPS 15 9530",
                    Description = "Biểu tượng ultrabook doanh nhân cao cấp, khung nhôm phay cắt CNC và chiếu nghỉ tay sợi carbon carbon fiber.",
                    ImageUrl = "https://images.unsplash.com/photo-1593642702821-c8da6771f0c6?w=800",
                    Price = 58990000,
                    CostPrice = 50000000,
                    StockQuantity = 6,
                    MinStockLevel = 2,
                    Cpu = "Intel Core i7-13700H (14 Cores, 20 Threads)",
                    Ram = "32GB DDR5 4800MHz",
                    Storage = "1TB PCIe NVMe SSD",
                    Gpu = "NVIDIA GeForce RTX 4060 8GB",
                    ScreenSize = "15.6 inch 3.5K OLED Cảm ứng Touch",
                    OperatingSystem = "Windows 11 Pro",
                    WeightKg = 1.92m,
                    WarrantyMonths = 24,
                    CategoryId = catUltrabook.Id
                },
                new()
                {
                    Sku = "LAP-TP-X1C-09",
                    Name = "Lenovo ThinkPad X1 Carbon Gen 12",
                    Brand = "Lenovo",
                    Model = "ThinkPad X1 Carbon Gen 12",
                    Description = "Đỉnh cao bàn phím gõ laptop văn phòng thế giới, sợi carbon siêu nhẹ và đạt chuẩn độ bền quân sự MIL-STD 810H.",
                    ImageUrl = "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=800",
                    Price = 46990000,
                    CostPrice = 40000000,
                    StockQuantity = 7,
                    MinStockLevel = 2,
                    Cpu = "Intel Core Ultra 7 155H NPU AI",
                    Ram = "32GB LPDDR5x 6400MHz",
                    Storage = "1TB SSD M.2 2280 PCIe Gen4",
                    Gpu = "Intel Arc Graphics tích hợp",
                    ScreenSize = "14.0 inch 2.8K OLED 120Hz 400 nits",
                    OperatingSystem = "Windows 11 Pro",
                    WeightKg = 1.09m,
                    WarrantyMonths = 36,
                    CategoryId = catUltrabook.Id
                },
                new()
                {
                    Sku = "LAP-HP-SPEC14-10",
                    Name = "HP Spectre x360 2-in-1 14 OLED",
                    Brand = "HP",
                    Model = "Spectre x360 14-eu000",
                    Description = "Laptop xoay gập 360 độ kèm bút cảm ứng thông minh, camera 9MP khử nhiễu AI tân tiến.",
                    ImageUrl = "https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=800",
                    Price = 41990000,
                    CostPrice = 36000000,
                    StockQuantity = 5,
                    MinStockLevel = 2,
                    Cpu = "Intel Core Ultra 7 155H",
                    Ram = "16GB LPDDR5x",
                    Storage = "1TB PCIe NVMe SSD",
                    Gpu = "Intel Arc Graphics",
                    ScreenSize = "14.0 inch 2.8K OLED Cảm ứng Touch 120Hz",
                    OperatingSystem = "Windows 11 Home",
                    WeightKg = 1.44m,
                    WarrantyMonths = 24,
                    CategoryId = catUltrabook.Id
                },
                new()
                {
                    Sku = "LAP-ASUS-ZEN14-11",
                    Name = "ASUS Zenbook 14 OLED UX3405",
                    Brand = "ASUS",
                    Model = "Zenbook 14 OLED",
                    Description = "Pin khủng 75Wh dùng 15 tiếng, thiết kế thanh lịch chuẩn văn phòng mỏng nhẹ.",
                    ImageUrl = "https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=800",
                    Price = 28990000,
                    CostPrice = 24500000,
                    StockQuantity = 11,
                    MinStockLevel = 3,
                    Cpu = "Intel Core Ultra 5 125H",
                    Ram = "16GB LPDDR5X",
                    Storage = "512GB PCIe 4.0 SSD",
                    Gpu = "Intel Arc Graphics",
                    ScreenSize = "14.0 inch 3K OLED 120Hz Lumina",
                    OperatingSystem = "Windows 11 Home",
                    WeightKg = 1.20m,
                    WarrantyMonths = 24,
                    CategoryId = catUltrabook.Id
                },

                // Laptop Đồ Họa & Trạm (Workstation)
                new()
                {
                    Sku = "LAP-DELL-P7680-12",
                    Name = "Dell Precision 7680 Workstation",
                    Brand = "Dell",
                    Model = "Precision 7680",
                    Description = "Máy trạm di động chuyên nghiệp, card đồ họa NVIDIA RTX 3500 Ada chứng nhận ISV cho AutoCAD, SolidWorks.",
                    ImageUrl = "https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=800",
                    Price = 78990000,
                    CostPrice = 68000000,
                    StockQuantity = 3,
                    MinStockLevel = 2,
                    Cpu = "Intel Core i9-13950HX vPro (24 Cores, 32 Threads)",
                    Ram = "64GB DDR5 5600MHz CAMM",
                    Storage = "2TB PCIe Gen4 NVMe SSD RAID",
                    Gpu = "NVIDIA RTX 3500 Ada Generation 12GB",
                    ScreenSize = "16.0 inch UHD+ OLED 100% DCI-P3 500 nits",
                    OperatingSystem = "Windows 11 Pro for Workstations",
                    WeightKg = 2.67m,
                    WarrantyMonths = 36,
                    CategoryId = catWorkstation.Id
                },
                new()
                {
                    Sku = "LAP-TP-P16G2-13",
                    Name = "Lenovo ThinkPad P16 Gen 2 Workstation",
                    Brand = "Lenovo",
                    Model = "ThinkPad P16 Gen 2",
                    Description = "Cỗ máy đồ họa trâu bò nhất của Lenovo, tản nhiệt buồng hơi kép và hỗ trợ tối đa 192GB RAM.",
                    ImageUrl = "https://images.unsplash.com/photo-1525547719571-a2d4ac8945e2?w=800",
                    Price = 72990000,
                    CostPrice = 63000000,
                    StockQuantity = 4,
                    MinStockLevel = 2,
                    Cpu = "Intel Core i7-13850HX vPro",
                    Ram = "32GB DDR5 5600MHz (4 khe cắm)",
                    Storage = "1TB PCIe Gen4 Performance SSD",
                    Gpu = "NVIDIA RTX 2000 Ada Generation 8GB",
                    ScreenSize = "16.0 inch WQXGA IPS 500 nits 165Hz",
                    OperatingSystem = "Windows 11 Pro",
                    WeightKg = 2.95m,
                    WarrantyMonths = 36,
                    CategoryId = catWorkstation.Id
                },
                new()
                {
                    Sku = "LAP-HP-ZB10-14",
                    Name = "HP ZBook Studio G10 Mobile Workstation",
                    Brand = "HP",
                    Model = "ZBook Studio G10",
                    Description = "Máy trạm đồ họa thiết kế mỏng nhẹ như ultrabook nhưng sức mạnh máy trạm công nghiệp.",
                    ImageUrl = "https://images.unsplash.com/photo-1593642632823-8f785ba67e45?w=800",
                    Price = 61500000,
                    CostPrice = 53000000,
                    StockQuantity = 5,
                    MinStockLevel = 2,
                    Cpu = "Intel Core i7-13700H",
                    Ram = "32GB DDR5 5200MHz",
                    Storage = "1TB PCIe NVMe SSD",
                    Gpu = "NVIDIA RTX 4070 8GB Studio Edition",
                    ScreenSize = "16.0 inch WQUXGA DreamColor 120Hz",
                    OperatingSystem = "Windows 11 Pro",
                    WeightKg = 1.73m,
                    WarrantyMonths = 36,
                    CategoryId = catWorkstation.Id
                },

                // Linh Kiện Máy Tính & Nâng Cấp
                new()
                {
                    Sku = "COMP-RAM-KNG32-15",
                    Name = "RAM Laptop Kingston Fury Impact 32GB DDR5 5600MHz",
                    Brand = "Kingston",
                    Model = "KF556S40IBK2-32",
                    Description = "Bộ kit 2 thanh 16GB RAM DDR5 cắm Plug N Play tự động ép xung lên 5600MHz cho laptop Intel thế hệ 13/14 và AMD Ryzen 7000/8000.",
                    ImageUrl = "https://images.unsplash.com/photo-1562976540-1502c2145186?w=800",
                    Price = 2850000,
                    CostPrice = 2200000,
                    StockQuantity = 28,
                    MinStockLevel = 5,
                    Ram = "32GB (2x16GB) DDR5 5600MHz CL40",
                    WarrantyMonths = 36,
                    CategoryId = catComponents.Id
                },
                new()
                {
                    Sku = "COMP-RAM-COR32-16",
                    Name = "RAM Desktop Corsair Vengeance RGB 32GB (2x16GB) DDR5 6000MHz",
                    Brand = "Corsair",
                    Model = "CMH32GX5M2B6000C30",
                    Description = "RAM PC hiệu năng cao hỗ trợ Intel XMP 3.0 và AMD EXPO, tản nhiệt nhôm có dải đèn LED RGB 10 vùng siêu đẹp.",
                    ImageUrl = "https://images.unsplash.com/photo-1562976540-1502c2145186?w=800",
                    Price = 3390000,
                    CostPrice = 2700000,
                    StockQuantity = 22,
                    MinStockLevel = 5,
                    Ram = "32GB (2x16GB) DDR5 6000MHz CL30",
                    WarrantyMonths = 36,
                    CategoryId = catComponents.Id
                },
                new()
                {
                    Sku = "COMP-SSD-SAM990-1T-17",
                    Name = "Ổ cứng SSD Samsung 990 Pro 1TB M.2 PCIe 4.0 NVMe",
                    Brand = "Samsung",
                    Model = "MZ-V9P1T0BW",
                    Description = "Ông vua tốc độ SSD PCIe 4.0 thế giới, tốc độ đọc tuần tự lên đến 7450 MB/s, ghi 6900 MB/s bền bỉ 600 TBW.",
                    ImageUrl = "https://images.unsplash.com/photo-1597872200969-2b65d56bd16b?w=800",
                    Price = 3090000,
                    CostPrice = 2450000,
                    StockQuantity = 35,
                    MinStockLevel = 8,
                    Storage = "1TB PCIe 4.0 NVMe M.2 2280",
                    WarrantyMonths = 60,
                    CategoryId = catComponents.Id
                },
                new()
                {
                    Sku = "COMP-SSD-SAM990-2T-18",
                    Name = "Ổ cứng SSD Samsung 990 Pro 2TB M.2 PCIe 4.0 NVMe",
                    Brand = "Samsung",
                    Model = "MZ-V9P2T0BW",
                    Description = "Dung lượng 2TB rộng rãi, tốc độ đọc 7450 MB/s lý tưởng cho cài đặt game nặng và dựng phim 4K/8K.",
                    ImageUrl = "https://images.unsplash.com/photo-1597872200969-2b65d56bd16b?w=800",
                    Price = 5290000,
                    CostPrice = 4300000,
                    StockQuantity = 18,
                    MinStockLevel = 4,
                    Storage = "2TB PCIe 4.0 NVMe M.2 2280",
                    WarrantyMonths = 60,
                    CategoryId = catComponents.Id
                },
                new()
                {
                    Sku = "COMP-SSD-KC3000-1T-19",
                    Name = "Ổ cứng SSD Kingston KC3000 1TB PCIe 4.0 NVMe",
                    Brand = "Kingston",
                    Model = "SKC3000S/1024G",
                    Description = "Trang bị bộ điều khiển Phison E18 mới nhất, tốc độ 7000 MB/s giá thành cực kỳ kinh tế.",
                    ImageUrl = "https://images.unsplash.com/photo-1597872200969-2b65d56bd16b?w=800",
                    Price = 2490000,
                    CostPrice = 1950000,
                    StockQuantity = 2, // Tồn kho thấp báo động
                    MinStockLevel = 5,
                    Storage = "1TB PCIe 4.0 NVMe",
                    WarrantyMonths = 60,
                    CategoryId = catComponents.Id
                },
                new()
                {
                    Sku = "COMP-VGA-TUF4070TIS-20",
                    Name = "Card Màn Hình ASUS TUF Gaming GeForce RTX 4070 Ti Super 16GB",
                    Brand = "ASUS",
                    Model = "TUF-RTX4070TIS-16G-GAMING",
                    Description = "Kiến trúc Ada Lovelace, 16GB VRAM GDDR6X thoải mái kéo game 4K Ray Tracing và huấn luyện mô hình AI Local.",
                    ImageUrl = "https://images.unsplash.com/photo-1587202372775-e229f172b9d7?w=800",
                    Price = 24990000,
                    CostPrice = 21500000,
                    StockQuantity = 7,
                    MinStockLevel = 2,
                    Gpu = "GeForce RTX 4070 Ti Super 16GB GDDR6X",
                    WarrantyMonths = 36,
                    CategoryId = catComponents.Id
                },

                // Phụ Kiện & Gaming Gear
                new()
                {
                    Sku = "GEAR-LOGI-MX3S-21",
                    Name = "Chuột Không Dây Logitech MX Master 3S",
                    Brand = "Logitech",
                    Model = "MX Master 3S Graphite",
                    Description = "Chuột công thái học hàng đầu cho lập trình viên và văn phòng, cuộn điện từ MagSpeed 1000 dòng/giây, click êm Quiet Clicks.",
                    ImageUrl = "https://images.unsplash.com/photo-1615663245857-ac93bb7c39e7?w=800",
                    Price = 2290000,
                    CostPrice = 1750000,
                    StockQuantity = 40,
                    MinStockLevel = 10,
                    WarrantyMonths = 12,
                    CategoryId = catGear.Id
                },
                new()
                {
                    Sku = "GEAR-RAZER-DAV3-22",
                    Name = "Chuột Gaming Không Dây Razer DeathAdder V3 Pro",
                    Brand = "Razer",
                    Model = "DeathAdder V3 Pro Black",
                    Description = "Siêu nhẹ chỉ 63 gram, cảm biến quang học Focus Pro 30K DPI, switch quang học Gen 3 chống double-click.",
                    ImageUrl = "https://images.unsplash.com/photo-1527864550417-7fd91fc51a46?w=800",
                    Price = 3390000,
                    CostPrice = 2650000,
                    StockQuantity = 19,
                    MinStockLevel = 5,
                    WarrantyMonths = 24,
                    CategoryId = catGear.Id
                },
                new()
                {
                    Sku = "GEAR-KEY-Q1PRO-23",
                    Name = "Bàn Phím Cơ Custom Không Dây Keychron Q1 Pro",
                    Brand = "Keychron",
                    Model = "Q1 Pro QMK/VIA Wireless",
                    Description = "Vỏ nhôm CNC nguyên khối, kết nối Bluetooth 5.1/Type-C, mạch hotswap, gasket mount gõ cực đầm và êm tai.",
                    ImageUrl = "https://images.unsplash.com/photo-1587829741301-dc798b83add3?w=800",
                    Price = 4690000,
                    CostPrice = 3800000,
                    StockQuantity = 14,
                    MinStockLevel = 3,
                    WarrantyMonths = 12,
                    CategoryId = catGear.Id
                },
                new()
                {
                    Sku = "GEAR-BAG-TAR15-24",
                    Name = "Balo Laptop Chống Sốc Targus Work+ 15.6 inch",
                    Brand = "Targus",
                    Model = "TBB610GL",
                    Description = "Chất liệu vải kháng nước chống rách cao cấp, đệm chống sốc SafePort bảo vệ máy tối đa khi di chuyển.",
                    ImageUrl = "https://images.unsplash.com/photo-1553062407-98eeb64c6a62?w=800",
                    Price = 1490000,
                    CostPrice = 950000,
                    StockQuantity = 25,
                    MinStockLevel = 5,
                    WarrantyMonths = 12,
                    CategoryId = catGear.Id
                }
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();
        }

        // 4. Seed Customers (12 khách hàng đa dạng)
        if (!await context.Customers.AnyAsync())
        {
            var customers = new List<Customer>
            {
                new()
                {
                    CustomerCode = "KH0001",
                    FullName = "Nguyễn Minh Tuấn",
                    PhoneNumber = "0988112233",
                    Email = "tuan.nguyen@fpt.com.vn",
                    Address = "128 Nguyễn Trãi, Quận 5, TP. Hồ Chí Minh",
                    TotalSpent = 91480000,
                    RewardPoints = 914,
                    Note = "Khách VIP, thường mua laptop gaming và nâng cấp RAM/SSD",
                    CreatedAt = DateTime.UtcNow.AddMonths(-5)
                },
                new()
                {
                    CustomerCode = "KH0002",
                    FullName = "Lê Thị Thảo",
                    PhoneNumber = "0977223344",
                    Email = "thao.le@vng.com.vn",
                    Address = "45 Lê Duẩn, Quận 1, TP. Hồ Chí Minh",
                    TotalSpent = 49990000,
                    RewardPoints = 500,
                    Note = "Lập trình viên iOS, chuộng hệ sinh thái Apple",
                    CreatedAt = DateTime.UtcNow.AddMonths(-4)
                },
                new()
                {
                    CustomerCode = "KH0003",
                    FullName = "Trần Đình Trọng",
                    PhoneNumber = "0966334455",
                    Email = "trong.tran@viettel.com.vn",
                    Address = "216 Hoàng Hoa Thám, Ba Đình, Hà Nội",
                    TotalSpent = 38490000,
                    RewardPoints = 384,
                    Note = "Kỹ sư mạng, thích laptop Lenovo Legion",
                    CreatedAt = DateTime.UtcNow.AddMonths(-4)
                },
                new()
                {
                    CustomerCode = "KH0004",
                    FullName = "Phạm Quỳnh Anh",
                    PhoneNumber = "0911445566",
                    Email = "quynhanh.pham@gmail.com",
                    Address = "88 Cầu Giấy, Quận Cầu Giấy, Hà Nội",
                    TotalSpent = 27490000,
                    RewardPoints = 275,
                    Note = "Marketing, chọn MacBook Air mỏng nhẹ",
                    CreatedAt = DateTime.UtcNow.AddMonths(-3)
                },
                new()
                {
                    CustomerCode = "KH0005",
                    FullName = "Vũ Hải Đăng",
                    PhoneNumber = "0933556677",
                    Email = "haidang.vu@outlook.com",
                    Address = "12 Nguyễn Văn Linh, Quận Hải Châu, Đà Nẵng",
                    TotalSpent = 78990000,
                    RewardPoints = 789,
                    Note = "Kiến trúc sư đồ họa, dùng Dell Precision",
                    CreatedAt = DateTime.UtcNow.AddMonths(-3)
                },
                new()
                {
                    CustomerCode = "KH0006",
                    FullName = "Đỗ Kim Ngân",
                    PhoneNumber = "0944667788",
                    Email = "ngan.dokim@gmail.com",
                    Address = "56 Hùng Vương, Ninh Kiều, Cần Thơ",
                    TotalSpent = 46990000,
                    RewardPoints = 470,
                    Note = "Quản lý nhân sự, chọn ThinkPad X1 Carbon",
                    CreatedAt = DateTime.UtcNow.AddMonths(-3)
                },
                new()
                {
                    CustomerCode = "KH0007",
                    FullName = "Hoàng Gia Bảo",
                    PhoneNumber = "0902778899",
                    Email = "giabao.hoang@gmail.com",
                    Address = "34 Võ Văn Ngân, TP. Thủ Đức, TP. Hồ Chí Minh",
                    TotalSpent = 32990000,
                    RewardPoints = 330,
                    Note = "Sinh viên CNTT trường ĐH Sư Phạm Kỹ Thuật",
                    CreatedAt = DateTime.UtcNow.AddMonths(-2)
                },
                new()
                {
                    CustomerCode = "KH0008",
                    FullName = "Bùi Phương Mai",
                    PhoneNumber = "0918889900",
                    Email = "mai.bp@techcombank.com.vn",
                    Address = "72 Trần Hưng Đạo, Hoàn Kiếm, Hà Nội",
                    TotalSpent = 41990000,
                    RewardPoints = 420,
                    Note = "Chuyên viên tài chính ngân hàng, mua HP Spectre",
                    CreatedAt = DateTime.UtcNow.AddMonths(-2)
                },
                new()
                {
                    CustomerCode = "KH0009",
                    FullName = "Ngô Quốc Huy",
                    PhoneNumber = "0929990011",
                    Email = "huy.ngo@vinai.io",
                    Address = "99 Quang Trung, Quận Gò Vấp, TP. Hồ Chí Minh",
                    TotalSpent = 24990000,
                    RewardPoints = 250,
                    Note = "Nghiên cứu sinh AI, mua GPU RTX 4070 Ti Super",
                    CreatedAt = DateTime.UtcNow.AddMonths(-1)
                },
                new()
                {
                    CustomerCode = "KH0010",
                    FullName = "Đặng Nhật Minh",
                    PhoneNumber = "0938001122",
                    Email = "minh.dang@gmail.com",
                    Address = "150 Pasteur, Quận 3, TP. Hồ Chí Minh",
                    TotalSpent = 10470000,
                    RewardPoints = 105,
                    Note = "Mua phụ kiện: Chuột MX Master 3S + Phím Keychron Q1",
                    CreatedAt = DateTime.UtcNow.AddMonths(-1)
                },
                new()
                {
                    CustomerCode = "KH0011",
                    FullName = "Phan Tấn Đạt",
                    PhoneNumber = "0949112233",
                    Email = "dat.phantan@gmail.com",
                    Address = "23 Nguyễn Thị Minh Khai, Quận 1, TP. Hồ Chí Minh",
                    TotalSpent = 25990000,
                    RewardPoints = 260,
                    Note = "Mua laptop MSI Katana 15",
                    CreatedAt = DateTime.UtcNow.AddDays(-20)
                },
                new()
                {
                    CustomerCode = "KH0012",
                    FullName = "Trương Ánh Tuyết",
                    PhoneNumber = "0959223344",
                    Email = "tuyet.truong@yahoo.com",
                    Address = "410 Cách Mạng Tháng 8, Quận 10, TP. Hồ Chí Minh",
                    TotalSpent = 28990000,
                    RewardPoints = 290,
                    Note = "Mua ASUS Zenbook 14 OLED",
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                }
            };

            context.Customers.AddRange(customers);
            await context.SaveChangesAsync();
        }

        // 5. Seed Orders (22 đơn hàng thực tế với nhiều trạng thái khác nhau)
        if (!await context.Orders.AnyAsync())
        {
            var customers = await context.Customers.ToListAsync();
            var products = await context.Products.ToListAsync();
            var staffUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "staff");

            var orders = new List<Order>();
            var rnd = new Random(42);

            var statuses = new[]
            {
                OrderStatus.Completed, OrderStatus.Completed, OrderStatus.Completed,
                OrderStatus.Completed, OrderStatus.Shipping, OrderStatus.Processing,
                OrderStatus.Pending, OrderStatus.Cancelled
            };

            var paymentMethods = new[]
            {
                PaymentMethod.Cash, PaymentMethod.BankTransfer, PaymentMethod.CreditCard, PaymentMethod.Installment
            };

            int orderSeq = 1;
            for (int i = 0; i < 22; i++)
            {
                var customer = customers[i % customers.Count];
                var status = statuses[i % statuses.Length];
                var method = paymentMethods[i % paymentMethods.Length];
                var payStatus = status == OrderStatus.Completed ? PaymentStatus.Paid : (status == OrderStatus.Cancelled ? PaymentStatus.Refunded : PaymentStatus.Unpaid);

                var orderDate = DateTime.UtcNow.AddDays(-60 + (i * 2.5));
                var orderNumber = $"HD{orderDate:yyyyMMdd}-{orderSeq:D4}";
                orderSeq++;

                // Chọn ngẫu nhiên 1 - 2 sản phẩm
                var p1 = products[(i * 3) % products.Count];
                var q1 = 1;

                var orderItems = new List<OrderItem>
                {
                    new()
                    {
                        ProductId = p1.Id,
                        ProductName = p1.Name,
                        ProductSku = p1.Sku,
                        Quantity = q1,
                        UnitPrice = p1.Price,
                        Discount = 0
                    }
                };

                decimal total = p1.Price * q1;
                decimal discount = i % 4 == 0 ? 500000 : 0;
                decimal final = total - discount;

                orders.Add(new Order
                {
                    OrderNumber = orderNumber,
                    CustomerId = customer.Id,
                    CreatedByUserId = staffUser?.Id,
                    TotalAmount = total,
                    DiscountAmount = discount,
                    FinalAmount = final,
                    Status = status,
                    PaymentMethod = method,
                    PaymentStatus = payStatus,
                    ShippingAddress = customer.Address,
                    CustomerNote = i % 3 == 0 ? "Giao hàng giờ hành chính, gọi trước khi đến." : null,
                    CreatedAt = orderDate,
                    CompletedAt = status == OrderStatus.Completed ? orderDate.AddHours(4) : null,
                    OrderItems = orderItems
                });
            }

            context.Orders.AddRange(orders);
            await context.SaveChangesAsync();
        }
    }
}
