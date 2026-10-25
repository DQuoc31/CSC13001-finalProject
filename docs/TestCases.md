# 🧪 TÀI LIỆU KIỂM THỬ CHẤT LƯỢNG HỆ THỐNG (TEST CASES)

> **Môn học:** UDQL2 2026 / Lập trình Desktop  
> **Dự án:** TechGear Laptop Store (Nhóm SV: 23120068 - 23120069 - 23210078)  
> **Phương pháp kiểm thử:** Automated Unit Tests (xUnit) & API Integration Tests

---

## 📋 1. Bảng ma trận các Test Cases tiêu biểu

| Mã Test Case | Nhóm kiểm thử | Dữ liệu đầu vào (Input) | Kết quả mong đợi (Expected) | Kết quả thực tế (Actual) | Trạng thái (Status) |
| :--- | :--- | :--- | :--- | :--- | :---: |
| **Login-01** | Xác thực | `admin` / `Admin@123` | Đăng nhập thành công, nhận JWT Bearer Token hợp lệ | Nhận Token JWT, HTTP 200 | **PASS** ✅ |
| **Login-02** | Xác thực | `admin` / `WrongPass` | Báo lỗi mật khẩu không chính xác, từ chối đăng nhập | Báo lỗi, HTTP 401 Unauthorized | **PASS** ✅ |
| **Product-01** | Nghiệp vụ SP | Thêm laptop mới hợp lệ (SKU: `TEST-01`, Giá: 25tr, Tồn: 10) | Sản phẩm được tạo thành công trong CSDL | Sản phẩm được tạo, HTTP 201 | **PASS** ✅ |
| **Product-02** | Kiểm thử biên | Thêm sản phẩm với `Price = -1000` | Hệ thống chặn dữ liệu, báo lỗi giá không được nhỏ hơn 0 | Báo lỗi xác thực, HTTP 400 | **PASS** ✅ |
| **Product-03** | Ràng buộc duy nhất | Thêm sản phẩm có SKU trùng với sản phẩm đã có | Từ chối tạo, báo SKU đã tồn tại | Báo lỗi trùng SKU, HTTP 400 | **PASS** ✅ |
| **Order-01** | Nghiệp vụ Đơn hàng | Mua 2 laptop Asus (tồn: 5 máy), giảm giá 1 triệu | Đơn hàng tạo thành công, tồn kho còn 3 máy | Đơn tạo thành công, tồn kho cập nhật chính xác | **PASS** ✅ |
| **Order-02** | Nghiệp vụ Tồn kho | Mua 5 laptop Dell (tồn kho hiện có: 2 máy) | Hệ thống từ chối lập đơn, báo lỗi số lượng vượt tồn kho | Báo lỗi thiếu tồn kho, HTTP 400 | **PASS** ✅ |
| **Order-03** | Kiểm thử biên | Lập đơn với `Quantity = 0` hoặc âm | Hệ thống từ chối, báo số lượng phải lớn hơn 0 | Báo lỗi số lượng không hợp lệ | **PASS** ✅ |
| **Auth-01** | Phân quyền RBAC | Tài khoản `SalesStaff` truy cập API Báo cáo Doanh thu | Từ chối truy cập do không đủ quyền hạn | Trả về mã lỗi HTTP 403 Forbidden | **PASS** ✅ |
| **Auth-02** | Xác thực API | Request gọi API không gửi kèm Token Bearer | Từ chối truy cập | Trả về mã lỗi HTTP 401 Unauthorized | **PASS** ✅ |
| **AI-01** | Trợ lý AI & RBAC | Tài khoản `SalesStaff` hỏi AI: "Cho tôi doanh thu toàn hệ thống" | AI từ chối tiết lộ do role là SalesStaff, không gọi tool báo cáo | AI từ chối và cảnh báo bảo mật | **PASS** ✅ |
| **AI-02** | Trợ lý AI & Tool | Quản lý (`Manager`) hỏi AI: "Báo cáo doanh thu tháng này" | AI gọi Tool `get_sales_report`, tính toán và trả lời doanh thu | AI gọi Tool thành công và giải thích chi tiết | **PASS** ✅ |
| **AI-03** | Trợ lý AI & CSDL | Nhân viên hỏi AI: "Tư vấn laptop 25 triệu để chơi game" | AI truy vấn CSDL sản phẩm thực tế, trả về gợi ý máy phù hợp | AI trả về các model có sẵn trong kho | **PASS** ✅ |

---

## 🚀 2. Kết quả chạy Kiểm thử tự động (Automated Tests)

Chạy lệnh kiểm thử tự động trên toàn bộ giải pháp:
```bash
dotnet test tests/LaptopStore.Application.Tests/LaptopStore.Application.Tests.csproj
```

**Kết quả thực thi:**
```
Test run for LaptopStore.Application.Tests.dll (.NETCoreApp,Version=v8.0)
Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed: 0, Passed: 12, Skipped: 0, Total: 12, Duration: 2 s
```
- **100% test cases đã vượt qua thành công (12/12 Passed).**
- Đảm bảo tính toàn vẹn dữ liệu, kiểm soát phân quyền chặt chẽ và không có hồi quy lỗi.
