# SimpleStore — Today / Tổng quan hôm nay Interactive HTML Reference v1.0

Mở `index.html` trực tiếp trong trình duyệt. Mini-project là **một file HTML tự chứa**, không cần máy chủ, tài khoản hay thư viện ngoài.

## Những phần có thể tương tác

- Chuyển ngày bằng hai nút mũi tên hoặc lịch; chọn ca sáng, chiều, tối hay tất cả các ca. KPI và biểu đồ cập nhật theo lựa chọn.
- Mở thẻ **Doanh thu** để xem drawer **Cách tính doanh thu**, gồm định nghĩa, công thức, phạm vi thời gian, giao dịch được tính và không được tính.
- Đổi biểu đồ theo giờ giữa doanh thu, số hóa đơn và số sản phẩm; trỏ hoặc nhấn cột để xem giá trị.
- Chọn nhóm trong chú giải donut để xem giá trị doanh thu nhóm.
- Mở chi tiết sản phẩm bán chạy, giao dịch, tình trạng tồn kho và công nợ từ từng thẻ hoặc nút **Xem tất cả**.
- Dùng phím `Esc` để đóng drawer và `F12` để mở mục Bán hàng mô phỏng. Menu trên màn hình hẹp có thể mở bằng nút menu.

## Dữ liệu và phạm vi

Dữ liệu là mẫu thiết kế, neo vào ngày **16/12/2024** để khớp ảnh tham chiếu. Các ngày khác được tạo biến thể có thể tương tác. Sidebar và nút Bán hàng chỉ minh họa điểm chuyển sang các module SimpleStore khác; mini-project này tập trung vào màn Today.

## File

- `index.html`: toàn bộ giao diện, CSS và JavaScript.
- `preview-1536x1024.png`: dashboard mặc định.
- `preview-explainability-1536x1024.png`: dashboard với drawer Doanh thu mở.
- `design-reference.png`: ảnh thiết kế người dùng cung cấp.
