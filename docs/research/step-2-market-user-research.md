# Step 2 Market & User Research — Chủ cửa hàng tạp hóa nhỏ tại Việt Nam

**Trạng thái:** Draft for Product Owner review — không phải quyết định đã APPROVED
**Ngày nghiên cứu:** 2026-09-20
**Mục đích:** Cung cấp bằng chứng để BA hoàn thiện Target Persona + Jobs To Be Done v0.1. Tài liệu không thiết kế solution và không tự quyết định product requirements.

## 1. Executive Summary

Các nguồn chính thức của KiotViet, Sapo, MISA eShop và POS365 cho thấy một cấu trúc công việc rất nhất quán đối với bán lẻ nhỏ: khởi tạo danh mục và tồn đầu kỳ; bán tại quầy; nhận hàng và cập nhật giá vốn/công nợ; kiểm kho và xử lý chênh lệch; đổi trả; ghi nhận thu–chi; theo dõi công nợ; xem báo cáo; và, với nhóm thuộc diện áp dụng hoặc có nhu cầu, phát hành hóa đơn điện tử. Đây là bằng chứng mạnh rằng các nghiệp vụ này là **table stakes của category**, dù chưa chứng minh mọi cửa hàng tạp hóa nhỏ đều dùng tất cả ở cùng mức độ.

Friction tập trung ở bốn cụm:

1. **Khởi tạo và duy trì dữ liệu đúng:** danh mục hàng, mã vạch, đơn vị tính, tồn đầu kỳ, giá vốn, file Excel và phân quyền đều có quy tắc; sai đầu vào làm suy giảm độ tin cậy của tồn kho/lợi nhuận về sau.
2. **Độ chính xác xuyên suốt các giao dịch:** bán, nhập, trả, hủy, bán offline và kiểm kho đều tác động đồng thời đến tồn, tiền, công nợ và lợi nhuận.
3. **Thiết bị và độ ổn định:** phản hồi công khai ghi nhận lỗi/chậm, tìm kiếm mất thời gian, tồn kho hiển thị chưa phù hợp, và vấn đề khổ in/kết nối máy in.
4. **Từ báo cáo đến quyết định:** có bằng chứng trực tiếp rằng ít nhất một người dùng Sapo thấy giao diện rườm rà và báo cáo tồn kho/so sánh kho khó xem; đồng thời tài liệu sản phẩm cho thấy báo cáo có nhiều “mối quan tâm”, bộ lọc và khái niệm kế toán. Tuy nhiên chưa có đủ bằng chứng để kết luận rộng rằng chủ tạp hóa Việt Nam nói chung “không hiểu báo cáo” hoặc sẽ trả tiền cho lời khuyên hành động. Giả thuyết này **có tín hiệu, nhưng chưa được xác nhận**.

Recommendation cho BA: dùng một **primary persona candidate** hẹp — chủ cửa hàng tạp hóa nhỏ, trực tiếp vận hành, chịu trách nhiệm cả quầy bán và quyết định nhập hàng/tiền — và mô tả JTBD theo outcome thực tế, không theo module phần mềm. Giữ “biến dữ liệu thành hành động” là hypothesis ưu tiên kiểm chứng trong giai đoạn demo/bán thật.

## 2. Research Questions

Nghiên cứu trả lời các câu hỏi sau:

- Chủ cửa hàng thường phải hoàn thành những workflow nào?
- Những điểm nào gây sai sót, gián đoạn hoặc cần hỗ trợ?
- Các quyết định kinh doanh nào lặp lại theo ngày/tuần/tháng?
- Job nào có bằng chứng mạnh; job nào mới là suy luận?
- Chức năng nào là table stakes của thị trường và đâu mới là cơ hội khác biệt?
- Có bằng chứng rằng người dùng khó hiểu báo cáo/chỉ số hoặc khó chuyển dữ liệu thành hành động không?

## 3. Method, Sources and Limitations

### 3.1 Source hierarchy

1. **Nguồn pháp lý/chính phủ:** dùng để xác nhận nghĩa vụ hóa đơn điện tử.
2. **Help center/tài liệu/video chính thức của nhà cung cấp:** dùng để xác nhận workflow, đối tượng dữ liệu và độ rộng nghiệp vụ mà sản phẩm hỗ trợ.
3. **App Store/Google Play reviews:** dùng để tìm friction thực tế, nhưng chỉ là bằng chứng định tính và có thiên lệch tự chọn.
4. **Nghiên cứu độc lập:** dùng để đặt adoption barrier trong bối cảnh doanh nghiệp nhỏ Việt Nam; không mặc định đồng nhất SME với cửa hàng tạp hóa.
5. **Trang marketing của nhà cung cấp:** chỉ dùng như tín hiệu về định vị category, không coi claim lợi ích là bằng chứng độc lập về outcome.

### 3.2 Important limitations

- Không có phỏng vấn trực tiếp, quan sát tại cửa hàng hoặc dữ liệu usage của chính SimpleStore.
- Help center chứng minh một workflow **được phần mềm hỗ trợ**, không tự nó chứng minh tần suất dùng hoặc mức độ đau của người dùng.
- App reviews phản ánh một số người dùng, phiên bản và thiết bị cụ thể; không đại diện thống kê cho toàn thị trường.
- Một số nguồn nghiên cứu nói về SME/doanh nghiệp nói chung, không riêng micro-retailer hoặc tạp hóa.
- Các nền tảng thay đổi nhanh; chi tiết UI không nên được chuyển thẳng thành requirement.

### 3.3 Labeling convention

- **Fact:** thông tin có thể kiểm tra trực tiếp từ nguồn đáng tin cậy.
- **Evidence:** quan sát hoặc dữ liệu hỗ trợ một kết luận, nhưng cần diễn giải.
- **Hypothesis:** kết luận hợp lý cần kiểm chứng thêm với người dùng/thị trường.
- **Assumption:** điều đang tạm chấp nhận để tiếp tục discovery nhưng bằng chứng yếu hoặc chưa có.

## 4. Candidate Target Persona (Not Approved)

### 4.1 Primary persona candidate

> **Chủ cửa hàng tạp hóa nhỏ tại Việt Nam, trực tiếp tham gia bán hàng và đồng thời chịu trách nhiệm nhập hàng, tồn kho, tiền/công nợ và kết quả kinh doanh; không có vai trò chuyên trách về CNTT hoặc phân tích dữ liệu.**

### 4.2 Why this is a reasonable candidate

- **Evidence:** các luồng “làm quen” và app quản lý của các nhà cung cấp gom cả bán hàng, kho, tiền, công nợ và báo cáo vào cùng một hệ thống/tài khoản quản lý. KiotViet mô tả ứng dụng quản lý để chủ cửa hàng giám sát giao dịch, doanh thu và tồn kho; Sapo mô tả kênh POS bao gồm giao dịch, kho tại chỗ, doanh số theo ca và công nợ; video tạp hóa của POS365 đi qua toàn bộ chuỗi từ tạo hàng đến báo cáo trong một phiên hướng dẫn.
- **Hypothesis:** ở cửa hàng nhỏ, một người thường đội nhiều vai và là economic buyer lẫn operator.
- **Assumption chưa chứng minh:** persona này có độ tuổi, trình độ công nghệ hoặc quy mô SKU cụ thể. Không nên gắn nhãn “lớn tuổi”, “ít học” hay “không biết công nghệ” nếu chưa có dữ liệu.

### 4.3 Context and pressures to capture in the persona

- Giao dịch tại quầy phải nhanh; khách đang chờ nên thời gian tìm hàng/thanh toán/in là nhạy cảm.
- Hàng hóa nhiều mã, đơn vị tính, lô/hạn dùng; tồn thực tế có thể lệch dữ liệu.
- Vốn nằm trong hàng tồn; thiếu hàng làm mất doanh thu, dư/chậm bán hoặc cận hạn làm đọng vốn/thất thoát.
- Tiền mặt, chuyển khoản, nợ khách và nợ nhà cung cấp cần được đối soát.
- Chủ cần biết “hôm nay có gì bất thường?” và “nên làm gì tiếp theo?”, nhưng mức sẵn sàng trả tiền cho hỗ trợ quyết định chưa được chứng minh.
- Nghĩa vụ thuế/hóa đơn điện tử có thể tạo urgency cho một phân khúc, không nên mặc định cho mọi cửa hàng.

## 5. Common Workflows and Decisions

### 5.1 Khởi tạo cửa hàng và dữ liệu ban đầu

**Typical flow**

1. Khai báo thông tin cửa hàng, kho/chi nhánh và người dùng.
2. Tạo danh mục hàng thủ công hoặc nhập file Excel.
3. Gán tên, SKU/mã vạch, đơn vị tính, giá bán, giá mua/giá vốn, tồn đầu kỳ; có thể thêm lô/hạn dùng.
4. Thiết lập/phân quyền nhân viên và thiết bị ngoại vi.
5. Thử bán, in và đối chiếu dữ liệu trước khi vận hành thật.

**Fact / Evidence**

- MISA eShop hướng dẫn hai cách khai báo hàng: nhập Excel hoặc từng mặt hàng; quy trình Excel gồm tải mẫu, sao chép dữ liệu, tải tệp, kiểm tra dòng hợp lệ/không hợp lệ và sửa lỗi. SKU và mã vạch là hai khái niệm riêng, dù hệ thống có thể tự sinh nếu bỏ trống. [MISA eShop — Khai báo hàng hóa](https://helpeshop.misa.vn/v1/kb/170100_khai_bao_hang_hoa)
- Sapo quy định điều kiện phân quyền, cấu trúc file, giới hạn ký tự barcode/SKU và các trường thuế/lô-hạn dùng; một số thuộc tính không thể đổi sau khi tạo và tồn đầu kỳ không sửa trực tiếp sau khởi tạo. [Sapo — Thêm sản phẩm](https://help.sapo.vn/thao-tac-them-moi-san-pham-thuong-tren-phan-mem-quan-ly-ban-hang-sapo), [Sapo — Nhập file sản phẩm](https://help.sapo.vn/them-moi-san-pham-bang-file-excel-tren-phan-mem-quan-ly-ban-hang-sapo)
- POS365 có workflow import hàng hóa với các lựa chọn riêng về cập nhật tồn kho/giá vốn và cập nhật mặt hàng đã tồn tại. [POS365 — Import hàng hóa](https://www.pos365.vn/docs/import-hang-hoa-2253.html)

**Pain/friction supported by evidence**

- Khởi tạo không chỉ là “thêm tên và giá”; người dùng phải hiểu cấu trúc dữ liệu và hậu quả của từng lựa chọn.
- Chuyển từ sổ/Excel/phần mềm cũ đòi hỏi làm sạch, ánh xạ và xử lý dòng lỗi.
- Sai tồn đầu kỳ, đơn vị tính, mã hoặc giá vốn có thể lan sang kiểm kho và báo cáo lợi nhuận. Đây là **inference mạnh từ cấu trúc workflow**, chưa phải đo lường lỗi thực tế.

**Recurring decision**

- Dữ liệu nào cần nhập ngay để bắt đầu bán, dữ liệu nào có thể bổ sung sau?

### 5.2 Bán hàng tại quầy: tìm/quét hàng → tính tiền → thanh toán → in/xuất hóa đơn

**Typical flow**

1. Tìm theo tên/mã hoặc quét barcode.
2. Điều chỉnh số lượng/giá; áp dụng giảm giá/khuyến mại.
3. Chọn hoặc tạo khách hàng nếu cần tích điểm/ghi nợ/xuất hóa đơn.
4. Nhận thanh toán bằng tiền mặt, thẻ, chuyển khoản hoặc QR.
5. In/gửi chứng từ; hệ thống ghi nhận doanh thu và giảm tồn.

**Fact / Evidence**

- KiotViet hỗ trợ bán nhanh/bán thường/bán giao hàng, tìm theo tên/mã/hình ảnh, quét mã vạch, nhiều phương thức thanh toán, khuyến mại, tích điểm và tra cứu tồn ngay trên màn hình bán. [KiotViet — Bán hàng](https://www.kiotviet.vn/huong-dan-su-dung-kiotviet/retail-ban-hang/ban-hang/)
- MISA eShop mô tả chuỗi chọn hàng/quét mã, nhập số lượng, sửa giá, áp dụng khuyến mại, chọn khách và in phiếu trên máy POS. [MISA eShop — Bán hàng trên máy POS](https://helpeshop.misa.vn/kb/huong-dan-su-dung-misa-eshop-tren-may-pos)
- Sapo xác định POS tại quầy bao gồm giao dịch, kho tại chỗ, doanh số theo ca, công nợ và chăm sóc khách hàng. [Sapo — Tổng quan kênh POS](https://help.sapo.vn/tong-quan-kenh-pos-ban-tai-quay)

**Pain/friction supported by evidence**

- Review công khai của Nhanh ghi nhận giao diện tìm sản phẩm mới khiến người dùng phải gõ nhiều và mất thời gian; các review khác ghi nhận lag/chậm. [App Store — Nhanh](https://apps.apple.com/vn/app/1275801299?platform=iphone&see-all=reviews)
- Review MISA eShop ghi nhận thao tác lag/chậm và thiếu/khó truy cập một số thông tin tại app bán hàng, trong đó có số lượng tồn và vị trí hàng. [App Store — MISA eShop Sale Phone](https://apps.apple.com/vn/app/misa-eshop-sale-phone/id1444979592?platform=iphone&see-all=reviews)
- Vấn đề in là friction thực: một review MISA nêu sự không khớp khổ 58 mm/80 mm; review hPOS nêu kết nối được nhưng in ngắt quãng/sai khổ. [MISA review được tổng hợp kèm nguồn Store](https://duyday.com/phan-mem-ban-hang-misa/), [App Store — hPOS](https://apps.apple.com/vn/app/hpos-qu%E1%BA%A3n-l%C3%BD-b%C3%A1n-h%C3%A0ng/id1617422347)

**Recurring decisions**

- Có cho phép bán khi tồn hệ thống bằng 0/âm không?
- Giao dịch nào cần gắn khách hàng hoặc ghi nợ?
- Khi nào cần in phiếu bán hàng, phiếu tạm tính hay phát hành HĐĐT?

### 5.3 Nhập hàng: chọn nhà cung cấp → nhận hàng → ghi giá/phụ phí → thanh toán/ghi nợ

**Typical flow**

1. Chọn/tạo nhà cung cấp và có thể tạo đơn đặt hàng nhập.
2. Nhận hàng; ghi số lượng, giá nhập, chiết khấu/phụ phí, lô/hạn dùng.
3. Cập nhật tồn và giá vốn.
4. Thanh toán ngay hoặc ghi nhận công nợ phải trả.
5. Xử lý trả hàng nhà cung cấp khi cần.

**Fact / Evidence**

- Sapo tách các nhóm nhà cung cấp, đặt hàng nhập, nhận hàng, phân bổ phụ phí, giá vốn, thanh toán công nợ và hoàn trả. [Sapo — Tổng quan sản phẩm & kho](https://help.sapo.vn/tong-quan-cac-tinh-nang-quan-ly-san-pham-kho-hang), [Sapo — Nhập hàng](https://help.sapo.vn/thao-tac-nhap-hang-tren-phan-mem-quan-ly-ban-hang-sapo)
- KiotViet mô tả nhập hàng tự động cập nhật tồn và công nợ phải trả; trả hàng nhập có thể giảm tồn, tính lại giá vốn và cấn trừ công nợ. [KiotViet — Trung tâm hỗ trợ](https://www.kiotviet.vn/ho-tro/), [KiotViet — Trả hàng nhập](https://www.kiotviet.vn/huong-dan-su-dung-kiotviet/retail-giao-dich/tra-hang-nhap/)
- Video POS365 cho mô hình tạp hóa đặt “nhập hàng” giữa tạo/in mã vạch và cảnh báo hạn dùng; video nhập hàng riêng nhấn mạnh phiếu nhập, lịch sử và công nợ. [POS365 — Video hướng dẫn tạp hóa](https://www.youtube.com/watch?v=Zv96DDuqOMw), [POS365 — Hướng dẫn nhập hàng](https://www.youtube.com/watch?v=wUKseQ3-LoA)

**Pain/friction supported by evidence**

- Một thao tác nhập tác động ít nhất ba sổ logic: tồn, giá vốn và công nợ/tiền. Sai hoặc bỏ sót một thành phần làm báo cáo sau đó sai. Đây là **inference từ mô hình dữ liệu**.
- Nhiều nhà cung cấp/đơn vị đóng gói và giá nhập thay đổi là hypothesis hợp lý cho tạp hóa nhưng chưa có bằng chứng trực tiếp trong tập nguồn này.

**Recurring decisions**

- Nhập mặt hàng nào, bao nhiêu, từ nhà cung cấp nào và trả ngay hay ghi nợ?
- Ưu tiên bán/xả hàng nào vì chậm bán hoặc cận hạn?

### 5.4 Quản lý tồn và kiểm kho

**Typical flow**

1. Theo dõi tồn theo giao dịch.
2. Kiểm kê toàn bộ hoặc một phần bằng tìm kiếm, barcode hoặc file.
3. So sánh số thực tế với số hệ thống.
4. Xác minh nguyên nhân lệch, ghi nhận giá trị lệch và cân bằng kho.
5. Dùng tồn tối thiểu, tốc độ bán và hạn dùng để quyết định nhập/xả.

**Fact / Evidence**

- KiotViet định nghĩa kiểm kho là hoạt động thường xuyên theo tuần/tháng/quý/năm; phiếu kiểm tách Khớp/Lệch/Chưa kiểm, tính giá trị lệch = số lượng lệch × giá vốn và cập nhật lại tồn khi hoàn tất. [KiotViet — Kiểm kho](https://www.kiotviet.vn/huong-dan-su-dung-kiotviet/retail-hang-hoa/kiem-kho/)
- MISA eShop hỗ trợ kiểm kê toàn bộ/một phần, quét barcode, tự tính chênh lệch và sinh phiếu nhập/xuất để xử lý lệch. [MISA eShop — Kiểm kê trên điện thoại](https://helpeshop.misa.vn/kb/kiem-ke-kho-hang-hoa-tren-dien-thoai)
- Sapo mô tả tồn tự giảm khi xuất bán, tăng khi hoàn hàng và hỗ trợ kiểm hàng/cân bằng kho. [Sapo — Quản lý tồn kho](https://support.sapo.vn/huong-dan-quan-ly-kho)

**Pain/friction supported by evidence**

- Sự tồn tại của workflow kiểm kê và “cân bằng kho” ở nhiều nền tảng là bằng chứng mạnh rằng tồn hệ thống và tồn thực tế có thể lệch; nguyên nhân cụ thể chưa được định lượng.
- Một review POS365 ngoài store ghi nhận tồn trên màn hình thu ngân không khớp kỳ vọng khi phần mềm xử lý bán offline. Nguồn có độ tin cậy thấp hơn và cần kiểm chứng chéo. [Review POS365](https://www.kiotviet.net/2025/10/review-dung-phan-mem-pos365-duoc-2-thang.html?m=1)
- Sapo cảnh báo rằng tồn đầu kỳ chỉ tạo một lần và không sửa trực tiếp; điều này làm tăng chi phí sửa sai ban đầu. [Sapo — Thêm sản phẩm](https://help.sapo.vn/thao-tac-them-moi-san-pham-thuong-tren-phan-mem-quan-ly-ban-hang-sapo)

**Recurring decisions**

- Chênh lệch nào cần điều tra và chênh lệch nào chấp nhận điều chỉnh?
- Mặt hàng nào sắp hết, đang dư, chậm bán hoặc gần hết hạn?

### 5.5 Đổi/trả hàng bán

**Typical flow**

1. Tìm hóa đơn gốc hoặc chọn trả nhanh không theo hóa đơn.
2. Chọn hàng và số lượng trả; xác định giá/số tiền hoàn.
3. Chọn phương thức hoàn hoặc cấn trừ công nợ.
4. Cập nhật tồn, doanh thu/công nợ và lưu phiếu đối soát.

**Fact / Evidence**

- KiotViet hỗ trợ trả theo hóa đơn và trả nhanh, đồng thời cập nhật tồn và công nợ; flow theo hóa đơn gồm tìm chứng từ, nhập số lượng/giá trả và hoàn tiền. [KiotViet — Trả hàng](https://www.kiotviet.vn/huong-dan-su-dung-kiotviet/retail-tra-hang/tra-hang/)
- MISA eShop đặt đổi trả trong workflow POS và có luồng phát hành hóa đơn điều chỉnh. [MISA eShop — Bán hàng trên máy POS](https://helpeshop.misa.vn/kb/huong-dan-su-dung-misa-eshop-tren-may-pos), [MISA eShop — Hóa đơn bán hàng](https://helpeshop.misa.vn/kb/hoa-don-ban-hang)

**Pain/friction supported by evidence**

- Trả hàng là giao dịch đảo nhiều hệ quả cùng lúc; nếu không bám hóa đơn gốc, giá hoàn, tồn và công nợ có thể cần quyết định thủ công. Đây là **inference từ workflow**, chưa có dữ liệu tần suất/sai sót.

### 5.6 Thu–chi, công nợ và đối soát cuối ngày

**Typical flow**

1. Ghi nhận thu bán hàng và các khoản thu khác.
2. Ghi nhận chi nhập hàng, vận hành hoặc hoàn tiền.
3. Theo dõi khách còn nợ và khoản phải trả nhà cung cấp.
4. Cuối ngày đối soát bán hàng, phương thức thanh toán, thu–chi và tiền thực tế.

**Fact / Evidence**

- Báo cáo cuối ngày của KiotViet gom ba mối quan tâm: bán hàng, thu–chi và hàng hóa; có bộ lọc theo phương thức thanh toán, loại thu–chi và nhân viên. [KiotViet — Báo cáo cuối ngày](https://www.kiotviet.vn/huong-dan-su-dung-kiotviet/huong-dan-bao-cao/bao-cao-cuoi-ngay)
- POS365 có riêng nhóm tài liệu thu–chi, chứng từ, công nợ khách và nhà cung cấp. [POS365 — Tài liệu hướng dẫn](https://www.pos365.vn/docs)
- MISA eShop định nghĩa báo cáo công nợ bằng nợ đầu kỳ, tăng, giảm và nợ cuối kỳ; cho phép drill-down theo chứng từ. [MISA eShop — Báo cáo công nợ](https://helpeshop.misa.vn/kb/bao-cao-chuoi-bao-cao-cong-no-khach-hang)
- Sapo 365 mô tả đơn chưa trả tiền tự tạo khoản nợ và cho phép ghi khoản nợ thủ công. [App Store — Sapo 365](https://apps.apple.com/vn/app/sapo-365/id1591419584)

**Pain/friction supported by evidence**

- Người dùng phải phân biệt doanh thu, tiền đã thu, công nợ và dòng tiền; các khái niệm này không đồng nhất. Đây là fact về cấu trúc nghiệp vụ, nhưng mức độ người dùng hiểu sai chưa được đo.
- Nhiều phương thức thanh toán làm đối soát cuối ngày phức tạp hơn; chưa có bằng chứng định lượng cho tạp hóa nhỏ trong tập nguồn.

**Recurring decisions**

- Hôm nay tiền thực tế có khớp giao dịch không?
- Khoản nợ nào cần thu/trả trước; khách nào có rủi ro nợ lâu?

### 5.7 Lợi nhuận và báo cáo quản trị

**Typical flow**

1. Chọn kỳ, chi nhánh, nhóm hàng hoặc mối quan tâm.
2. Xem doanh thu, doanh thu thuần, giá vốn, chi phí, trả hàng và lợi nhuận.
3. Drill-down đến mặt hàng/chứng từ.
4. So sánh theo thời gian hoặc xếp hạng bán chạy/chậm.
5. Ra quyết định nhập/xả, giá/khuyến mại và kiểm tra bất thường.

**Fact / Evidence**

- KiotViet có báo cáo bán hàng với 6 “mối quan tâm” và báo cáo hàng hóa với 7 “mối quan tâm”, nhiều chiều hiển thị và bộ lọc. [KiotViet — Báo cáo bán hàng](https://www.kiotviet.vn/huong-dan-su-dung-kiotviet/huong-dan-bao-cao/bao-cao-ban-hang/), [KiotViet — Báo cáo hàng hóa](https://www.kiotviet.vn/huong-dan-su-dung-kiotviet/huong-dan-bao-cao/bao-cao-hang-hoa/)
- MISA định nghĩa lợi nhuận qua nhiều thành phần: doanh thu, khuyến mại, giá vốn, chi phí khác và ảnh hưởng của trả/kiểm kê. [MISA eShop — Tình hình lợi nhuận](https://helpeshop.misa.vn/v1/kb/170107_tinh_hinh_loi_nhuan)
- POS365 có các báo cáo bán, nhập, kho và công nợ, mỗi báo cáo dùng nhiều bộ lọc. [POS365 — Báo cáo](https://www.pos365.vn/docs/bao-cao-2389.html)
- KiotViet đã bổ sung phân tích công nợ theo tỷ lệ nợ/doanh thu, tuổi nợ và nhóm khách nợ nhiều; điều này thể hiện xu hướng category đi từ số liệu thô sang phân tích. [KiotViet — Phân tích](https://www.kiotviet.vn/huong-dan-su-dung-kiotviet/retail-phan-tich/phan-tich/)

**Pain/friction supported by evidence**

- Review Sapo nêu rõ giao diện rườm rà/rối mắt và báo cáo tồn kho hoặc so sánh số lượng giữa các kho “rất khó xem”. Đây là bằng chứng trực tiếp, nhưng chỉ từ một review và không riêng tạp hóa. [App Store — Sapo](https://apps.apple.com/vn/app/sapo-sales-management/id979090133)
- Review KiotViet yêu cầu bổ sung tuổi nợ, báo cáo theo lô/hạn dùng và phản ánh việc mất tổng tiền tồn kho khỏi menu quản lý; đây là tín hiệu rằng người dùng cần chỉ số phù hợp với quyết định, không chỉ dữ liệu tổng quát. [App Store — KiotViet](https://apps.apple.com/vn/app/kiotviet/id1146695068?platform=watch)

**Recurring decisions**

- Mặt hàng nào nên nhập thêm, giảm nhập, đổi giá hoặc xả?
- Doanh thu tăng/giảm do đâu; lợi nhuận thật có tốt không?
- Có thất thoát, chi phí hoặc nợ nào cần xử lý ngay?

### 5.8 Hóa đơn điện tử và tuân thủ

**Typical flow**

1. Xác định cửa hàng có thuộc diện áp dụng và loại hóa đơn cần dùng.
2. Đăng ký/kết nối nhà cung cấp HĐĐT, thông tin thuế và phương thức ký.
3. Phát hành tại lúc bán hoặc từ danh sách chứng từ.
4. Theo dõi hóa đơn chưa/đã phát hành, hóa đơn bị từ chối, thay thế/điều chỉnh khi đổi trả.

**Fact / Evidence**

- Nghị định 70/2025/NĐ-CP có hiệu lực từ 2025-06-01, bổ sung nhóm hộ/cá nhân kinh doanh doanh thu từ 1 tỷ đồng/năm và doanh nghiệp bán trực tiếp cho người tiêu dùng thuộc các ngành nêu trong quy định vào đối tượng sử dụng HĐĐT khởi tạo từ máy tính tiền kết nối cơ quan thuế. [Cổng Thông tin Chính phủ — nội dung mới của Nghị định 70](https://xaydungchinhsach.chinhphu.vn/mot-so-noi-dung-moi-cua-nghi-dinh-so-70-2025-nd-cp-ve-hoa-don-chung-tu-119250403074719995.htm)
- MISA eShop có nhiều luồng riêng cho kết nối nhà cung cấp, ký số, phát hành, điều chỉnh, hóa đơn bị cơ quan thuế từ chối và cập nhật địa điểm kinh doanh. [MISA eShop — Hóa đơn bán hàng](https://helpeshop.misa.vn/kb/hoa-don-ban-hang)
- Review Sapo 6870 ghi nhận trường hợp thông tin tên/địa chỉ hộ kinh doanh trên báo cáo không khớp giấy phép, khiến người dùng coi báo cáo không dùng được. Đây là một case định tính có liên quan trực tiếp đến compliance. [App Store — Sapo 6870](https://apps.apple.com/vn/app/sapo-6870/id6747013574?platform=iphone&see-all=reviews)

**Pain/friction supported by evidence**

- Compliance không phải một nút “xuất hóa đơn”; nó phụ thuộc đăng ký, danh tính/địa điểm, nhà cung cấp, trạng thái cơ quan thuế và xử lý sai sót.
- Sai metadata có thể làm chứng từ/báo cáo mất giá trị sử dụng đối với người dùng, như case Sapo 6870.

## 6. Cross-Cutting Pain Points and Friction

| Pain / friction | Classification | Evidence strength | What the evidence supports |
|---|---|---:|---|
| Khởi tạo danh mục/tồn đầu kỳ nặng và dễ sai | Evidence + hypothesis | Medium | Nhiều bước, quy tắc Excel/SKU/barcode/tồn; chưa có tỷ lệ bỏ cuộc |
| Tồn hệ thống lệch tồn thực tế | Fact at workflow level | High | Tất cả nền tảng lớn có kiểm kê/cân bằng; nguyên nhân và tần suất chưa biết |
| Một giao dịch ảnh hưởng đồng thời tồn, tiền, nợ, lợi nhuận | Fact | High | Thể hiện rõ trong tài liệu nhập/trả/thu-chi |
| Tìm hàng và thao tác tại quầy phải nhanh | Evidence + hypothesis | Medium | Thiết kế “bán nhanh” và review than tìm kiếm chậm/lag; chưa có time-on-task |
| Máy in/khổ giấy/kết nối gây gián đoạn | Evidence | Medium | Review Store và FAQ lỗi máy in; cần định lượng theo hardware |
| Offline/sync làm người dùng nghi ngờ số tồn | Evidence | Low–Medium | Có workflow đồng bộ offline và một review POS365; chưa đủ đại diện |
| Giao diện nhiều chức năng có thể rối | Evidence | Medium | Review Sapo trực tiếp; độ rộng help center; chưa có usability test |
| Báo cáo/chỉ số khó đọc | Evidence | Low–Medium | Một review trực tiếp về báo cáo tồn; tài liệu cho thấy nhiều khái niệm/bộ lọc |
| Dữ liệu khó chuyển thành quyết định | Hypothesis | Low | Vendor marketing liên kết báo cáo với quyết định; chưa có lời kể người dùng đủ mạnh |
| Chi phí, kỹ năng số, năng lực quản lý, lo ngại công nghệ cản adoption | Contextual fact for SMEs | Medium | Nghiên cứu SME Việt Nam; không riêng chủ tạp hóa |
| Hỗ trợ chậm làm tăng rủi ro vận hành | Evidence | Low–Medium | Một số review App Store; không có dữ liệu SLA/volume |

Nguồn bối cảnh: World Bank ghi nhận khoảng cách áp dụng công nghệ ở các chức năng bán hàng và sourcing/procurement của doanh nghiệp Việt Nam; một nghiên cứu gần đây về SME Việt Nam tổng hợp các rào cản gồm nguồn lực tài chính, kỹ năng số, năng lực quản lý, hạn chế công nghệ, an ninh mạng và kháng cự thay đổi. Các kết quả này chỉ nên dùng để định hướng câu hỏi, không gán trực tiếp cho mọi chủ tạp hóa. [World Bank — Firm-Level Technology Adoption in Vietnam](https://documents1.worldbank.org/curated/en/498501615216149075/pdf/Firm-Level-Technology-Adoption-in-Vietnam.pdf), [Tạp chí Công Thương — Digital transformation in SMEs](https://nckh.tapchicongthuong.vn/j/nckh/articles/3553-digital-transformation-in-small-and-medium-sized-enterprises-in-vietnam-current-status-barriers-and-solutions?lang=en-US)

## 7. Evidence-Supported Jobs To Be Done (Candidates, Not Approved)

JTBD dưới đây được viết theo outcome người dùng muốn đạt, không theo tên module.

### JTBD-1 — Hoàn tất giao dịch nhanh và đúng khi khách đang chờ

> Khi khách mang hàng ra quầy, tôi muốn nhận diện đúng sản phẩm, tính đúng giá/khuyến mại, nhận đúng phương thức thanh toán và đưa chứng từ phù hợp, để phục vụ nhanh mà không tạo sai lệch về sau.

- **Evidence strength: High.** Cả KiotViet, Sapo, MISA và POS365 đều tập trung workflow POS, barcode, thanh toán và in.
- **Risks/frictions:** tìm hàng chậm, lag, barcode/danh mục thiếu, thiết bị in không tương thích.

### JTBD-2 — Biết số hàng thực sự có thể bán và sửa chênh lệch đáng tin cậy

> Khi cần tư vấn khách hoặc chuẩn bị nhập hàng, tôi muốn biết tồn thực tế đủ tin cậy và nhanh chóng xử lý chênh lệch, để không bán hụt, nhập dư hoặc mất hàng mà không biết.

- **Evidence strength: High.** Kiểm kho/cân bằng và tự động cập nhật tồn là pattern xuyên sản phẩm.
- **Risks/frictions:** dữ liệu đầu kỳ sai, giao dịch offline/chưa sync, trả/hủy/điều chỉnh chưa phản ánh đúng.

### JTBD-3 — Nhập đúng mặt hàng và đúng lượng mà không khóa vốn hoặc hết hàng

> Khi chuẩn bị nhập hàng, tôi muốn biết mặt hàng nào sắp hết, bán nhanh, bán chậm hoặc cận hạn và hiểu nghĩa vụ với nhà cung cấp, để quyết định nhập/xả phù hợp.

- **Evidence strength: Medium–High.** Báo cáo bán chạy/chậm, cảnh báo tồn/hạn dùng, nhập hàng và công nợ nhà cung cấp xuất hiện nhất quán.
- **Gap:** chưa có nghiên cứu trực tiếp về cách chủ tạp hóa hiện ra quyết định (theo kinh nghiệm, lời chào hàng của NCC hay báo cáo).

### JTBD-4 — Giữ tiền, doanh thu và công nợ khớp nhau

> Cuối ngày hoặc khi cần thanh toán, tôi muốn đối soát tiền đã nhận/đã chi với giao dịch và biết ai còn nợ mình/mình còn nợ ai, để không mất tiền và ưu tiên thu/trả đúng lúc.

- **Evidence strength: High.** Thu–chi, công nợ, phương thức thanh toán và báo cáo cuối ngày là workflow chuẩn.
- **Gap:** chưa biết tần suất bán nợ trong từng phân khúc tạp hóa.

### JTBD-5 — Xử lý ngoại lệ mà không làm hỏng sổ liệu

> Khi có trả/đổi, hủy nhầm, mất mạng, nhập sai hoặc lệch kho, tôi muốn sửa tình huống đúng và có dấu vết, để tồn, tiền, nợ và lợi nhuận vẫn đáng tin.

- **Evidence strength: High for workflow, Medium for pain severity.** Nhiều luồng riêng và FAQ cho ngoại lệ; review cho thấy lỗi thiết bị/sync/tồn có thể xảy ra.

### JTBD-6 — Hiểu cửa hàng đang lời/lỗ ở đâu

> Khi xem kết quả ngày/tuần/tháng, tôi muốn phân biệt doanh thu, tiền thu, giá vốn, chi phí, trả hàng và lợi nhuận, để biết điều gì đang làm kết quả tốt lên hoặc xấu đi.

- **Evidence strength: Medium.** Hệ thống báo cáo và định nghĩa chỉ số rất phong phú; có phản hồi rằng báo cáo tồn khó xem.
- **Gap:** chưa chứng minh người dùng ưu tiên “lợi nhuận chuẩn” hơn các proxy đơn giản như tiền còn lại hoặc doanh thu.

### JTBD-7 — Biết việc cần chú ý tiếp theo, không phải tự phân tích toàn bộ báo cáo

> Khi thời gian quản lý có hạn, tôi muốn nhận ra vấn đề hoặc cơ hội đáng xử lý trước, để hành động mà không phải tự ghép nhiều báo cáo và thuật ngữ.

- **Evidence strength: Low–Medium; hypothesis priority.** KiotViet đã thêm phân tích như tuổi nợ/nhóm hàng; Knote định vị “hỏi bằng tiếng Việt, không cần mở báo cáo”; một review Sapo nói báo cáo khó xem. [App Store — Knote](https://apps.apple.com/vn/app/knote-pos-b%C3%A1n-h%C3%A0ng-ai/id6738162895)
- **Do not treat as validated willingness-to-pay.** Chưa có bằng chứng khách sẵn sàng trả tiền riêng cho outcome này.

### JTBD-8 — Đáp ứng nghĩa vụ hóa đơn/thuế mà không làm gián đoạn bán hàng

> Khi giao dịch thuộc diện phải hoặc cần xuất HĐĐT, tôi muốn phát hành đúng thông tin, đúng trạng thái và sửa sai đúng quy trình, để tuân thủ mà quầy bán vẫn vận hành.

- **Evidence strength: High for affected users; variable relevance.** Quy định và workflow chính thức rõ, nhưng không phải mọi cửa hàng đều cùng phạm vi áp dụng.

## 8. Table Stakes vs. Differentiator Opportunities

### 8.1 Table Stakes — category capabilities

Các khả năng sau xuất hiện nhất quán ở nhiều sản phẩm và gắn trực tiếp với workflow cốt lõi. Đây là **evidence về expectation của category**, không phải quyết định scope hoặc phiên bản cho SimpleStore.

| Capability area | Why it is table stakes |
|---|---|
| Màn hình bán hàng nhanh | Core flow của mọi POS được khảo sát |
| Tìm theo tên/mã, barcode | Xuất hiện ở KiotViet, Sapo, MISA, POS365 |
| Thanh toán đa phương thức | Tiền mặt/chuyển khoản/QR là phần của checkout |
| In/gửi chứng từ | Workflow bán hàng thực tế và có hardware friction |
| Danh mục, đơn vị tính, giá và tồn đầu kỳ | Điều kiện để các module sau hoạt động |
| Nhập hàng, nhà cung cấp, giá vốn | Liên kết trực tiếp với tồn và lợi nhuận |
| Tồn kho, kiểm kê, cân bằng chênh lệch | Pattern nhất quán xuyên sản phẩm |
| Đổi/trả bán và trả hàng nhập | Cần đảo đúng tồn, tiền và công nợ |
| Thu–chi và đối soát cuối ngày | Cần phân biệt giao dịch với dòng tiền |
| Công nợ khách/NCC | Có mặt trên nhiều sản phẩm và báo cáo |
| Báo cáo doanh thu, hàng hóa, lợi nhuận | Baseline của category |
| HĐĐT/tích hợp thuế cho nhóm áp dụng | Nhu cầu compliance có căn cứ pháp lý |
| Phân quyền, lịch sử thao tác, offline/sync phù hợp | Cần cho vận hành an toàn; mức độ có thể tùy phân khúc |

### 8.2 Differentiator opportunities — hypotheses, not solutions

Các cơ hội dưới đây mô tả **outcome có thể khác biệt**, không đề xuất UI hay product feature cụ thể.

| Opportunity | Evidence | Confidence |
|---|---|---:|
| Giảm effort khởi tạo và phục hồi lỗi dữ liệu | Nhiều quy tắc import/tồn/SKU; lỗi đầu vào có hậu quả dây chuyền | Medium |
| Giữ workflow tại quầy nhanh và ổn định trên thiết bị phổ thông | Review về lag, tìm kiếm và in | Medium |
| Làm cho trạng thái tồn “đáng tin và giải thích được” | Kiểm kho phổ biến; offline/sync và điều chỉnh tạo nghi ngờ | Medium |
| Diễn giải chỉ số bằng ngôn ngữ nghiệp vụ đời thường | Báo cáo nhiều thuật ngữ; một review nói khó xem | Low–Medium |
| Ưu tiên vấn đề/cơ hội và gắn số liệu với hành động | Các sản phẩm bắt đầu thêm phân tích; marketing AI chuyển sang hỏi đáp | Low–Medium |
| Xử lý ngoại lệ an toàn mà không cần nhớ nghiệp vụ kế toán | Nhiều workflow trả/hủy/điều chỉnh/HĐĐT | Medium |
| Compliance ít gây gián đoạn | Quy trình HĐĐT nhiều bước và case sai thông tin | Medium for affected segment |

Không nên coi “AI”, dashboard, chatbot hoặc cảnh báo là differentiator tự thân. Differentiator chỉ tồn tại nếu outcome tốt hơn được người dùng nhận biết và coi trọng.

## 9. Special Hypothesis Test — Reports, Metrics and Actionability

### 9.1 What is supported

**Fact**

- Báo cáo của các nền tảng có nhiều lớp: kỳ, chi nhánh, mối quan tâm, kiểu hiển thị, bộ lọc, drill-down và các khái niệm như doanh thu thuần, giá vốn, lợi nhuận, nợ đầu/tăng/giảm/cuối kỳ.
- Một số chỉ số phụ thuộc dữ liệu vận hành trước đó: giá vốn phụ thuộc nhập hàng; lợi nhuận bị ảnh hưởng bởi trả hàng, khuyến mại, kiểm kê và chi phí.

**Evidence**

- Một review Sapo mô tả giao diện rườm rà/rối mắt và báo cáo tồn kho/so sánh kho khó xem.
- Một review KiotViet yêu cầu tuổi nợ, báo cáo theo lô/hạn và tổng giá trị tồn ở vị trí dễ thấy, cho thấy “có báo cáo” chưa chắc đồng nghĩa “đúng góc nhìn quyết định”.
- KiotViet bổ sung phân tích tuổi nợ, tỷ lệ nợ/doanh thu và phân nhóm hàng; POS365 xuất bản nội dung nối báo cáo bán chạy/chậm với quyết định nhập/xả. Đây là tín hiệu các vendor nhận thấy nhu cầu hỗ trợ quyết định, nhưng vẫn là bằng chứng từ phía cung.
- Knote định vị hỏi đáp ngôn ngữ tự nhiên “không cần mở báo cáo”; đây là tín hiệu cạnh tranh, không phải chứng minh người dùng trả tiền.

### 9.2 What is not yet supported

Chưa đủ bằng chứng để nói:

- Đa số chủ tạp hóa không hiểu các khái niệm doanh thu/lợi nhuận/giá vốn.
- “Không biết phải làm gì” là pain lớn hơn bán nhanh, tồn chính xác hoặc compliance.
- Người dùng sẽ tin hoặc làm theo khuyến nghị tự động.
- Người dùng sẵn sàng trả thêm cho insight/actionability.
- Một giao diện đơn giản hơn sẽ giải quyết vấn đề nếu dữ liệu gốc vẫn sai.

### 9.3 Current judgment

> **Hypothesis status: Promising but unproven.** Có bằng chứng về complexity và một số direct complaints về khả năng xem báo cáo. Bằng chứng về “action gap” và willingness-to-pay vẫn yếu.

### 9.4 Cheapest ethical validation without in-person interviews

Không cần đi tận cửa hàng, nhưng nên kiểm chứng qua hành vi thật sau MVP/demo:

- Demo từ xa cho 5–10 chủ cửa hàng và yêu cầu họ trả lời các câu hỏi quyết định bằng dữ liệu mẫu: “mặt hàng nào cần nhập?”, “vì sao lợi nhuận giảm?”, “khoản nợ nào cần xử lý?”.
- Không hướng dẫn đường đi; đo họ chọn báo cáo nào, mất bao lâu, trả lời đúng không và mức tự tin.
- Sau đó cho xem một cách trình bày thiên về outcome/action; so sánh độ chính xác, thời gian và mức tin tưởng.
- Trong pilot, theo dõi liệu họ có thực hiện hành động được gợi ý và quay lại dùng hay không; không dùng lời khen demo làm proxy cho willingness-to-pay.

Phần này là recommendation cho discovery, không phải product requirement.

## 10. Decision Inventory for the Persona

### Daily / per transaction

- Bán mặt hàng nào, giá/khuyến mại nào, có ghi khách/công nợ không?
- Thanh toán đã nhận đủ và đúng phương thức chưa?
- Có cần in/gửi chứng từ hoặc phát hành HĐĐT không?
- Trả/đổi/hủy thế nào để tồn và tiền vẫn đúng?
- Có giao dịch hoặc tiền cuối ngày nào không khớp?

### Weekly / replenishment cycle

- SKU nào cần nhập, nhập bao nhiêu, từ nhà cung cấp nào?
- SKU nào bán chậm, cận hạn hoặc đang giữ quá nhiều vốn?
- Chênh lệch tồn nào cần kiểm tra?
- Công nợ khách/NCC nào cần thu/trả?

### Monthly / periodic

- Cửa hàng thực sự lời/lỗ thế nào sau giá vốn, trả hàng và chi phí?
- Nhóm hàng nào đóng góp doanh thu/lợi nhuận; nhóm nào nên giảm?
- Có dấu hiệu thất thoát hoặc thao tác nhân viên bất thường không?
- Có nghĩa vụ thuế/HĐĐT hoặc thay đổi cấu hình nào cần xử lý?

## 11. Evidence Summary

### Strong evidence

- Category workflow hội tụ quanh bán hàng, barcode/in, nhập–tồn–kiểm, đổi trả, thu–chi, công nợ, báo cáo và HĐĐT.
- Dữ liệu vận hành có tính liên kết: một giao dịch có thể đồng thời thay đổi tồn, tiền, công nợ và lợi nhuận.
- Kiểm kho/cân bằng chênh lệch là workflow chuẩn, cho thấy độ tin cậy của tồn là bài toán thường trực.
- HĐĐT khởi tạo từ máy tính tiền có căn cứ pháp lý rõ cho nhóm đối tượng thuộc phạm vi.

### Medium evidence

- Khởi tạo dữ liệu là friction đáng kể do số trường, quy tắc import và tính khó đảo của một số lựa chọn.
- Tốc độ, tìm kiếm, độ ổn định và tương thích máy in ảnh hưởng trực tiếp đến quầy bán.
- Người dùng cần góc nhìn báo cáo khớp quyết định cụ thể hơn là chỉ nhiều chỉ số.

### Weak evidence

- Mức phổ biến của từng pain trong riêng nhóm tạp hóa nhỏ.
- Mức độ hiểu sai chỉ số và khoảng cách từ dữ liệu đến hành động.
- Willingness-to-pay cho insight, cảnh báo hoặc khuyến nghị.

## 12. Strongest Hypotheses

1. **Reliability before intelligence:** chủ cửa hàng chỉ tin báo cáo/khuyến nghị khi giao dịch, tồn và giá vốn được ghi nhận đúng và có thể giải thích.
2. **Time-to-complete matters at the counter:** tốc độ thao tác, tìm hàng và in là outcome cốt lõi, không chỉ UX polish.
3. **Inventory confidence is central:** biết “có bao nhiêu hàng thật” và xử lý chênh lệch có giá trị cao vì liên quan trực tiếp tới doanh thu, vốn và thất thoát.
4. **Owner-operator needs cross-domain visibility:** persona chính có khả năng phải nối quầy bán, kho, tiền/nợ và kết quả kinh doanh thay vì chỉ làm một vai.
5. **Actionability may differentiate after table stakes work:** nếu hệ thống trả lời đúng câu hỏi quyết định bằng ngôn ngữ dễ hiểu và có căn cứ, nó có thể khác biệt so với báo cáo nhiều lớp.
6. **Compliance can accelerate adoption for a segment:** HĐĐT/thuế tạo trigger mua rõ hơn “quản lý tốt hơn” đối với cửa hàng thuộc diện áp dụng.

## 13. Weak / Unproven Assumptions

- Chủ tạp hóa nhỏ nói chung “ít kinh nghiệm phần mềm”.
- Chủ cửa hàng trực tiếp làm mọi thao tác thay vì giao cho vợ/chồng/con/nhân viên.
- Bán chịu/công nợ khách là phổ biến ở mọi khu vực và quy mô.
- Lô/hạn dùng cần ở mức chi tiết giống nhau cho mọi cửa hàng tạp hóa.
- Họ không hiểu lợi nhuận hoặc báo cáo; bằng chứng hiện chỉ đủ nói một số giao diện/chỉ số có thể khó dùng.
- Họ muốn AI đưa lời khuyên và sẽ tin lời khuyên đó.
- Họ sẵn sàng trả tiền cho “hiểu cửa hàng tốt hơn” thay vì chỉ cho POS, kho hoặc HĐĐT.
- Mobile là thiết bị chính duy nhất; nhiều workflow vẫn phụ thuộc PC/POS/máy in/máy quét.
- Một persona duy nhất đủ bao phủ tạp hóa đô thị, nông thôn, cửa hàng gia đình và minimart nhỏ.

## 14. Open Questions

1. Quy mô SKU, số giao dịch/ngày và số người vận hành nào là boundary của “cửa hàng tạp hóa nhỏ”?
2. Ai thực sự bán tại quầy, ai nhập hàng, ai xem báo cáo và ai trả tiền mua phần mềm?
3. Chủ đang dùng sổ, Excel, app đơn giản hay POS đầy đủ; dữ liệu hiện nằm ở đâu?
4. Bao nhiêu giao dịch là tiền mặt, QR/chuyển khoản, và bán nợ?
5. Mất mạng hoặc thiết bị hỏng xảy ra bao nhiêu; tolerance cho downtime là gì?
6. Họ kiểm kho theo lịch hay chỉ khi phát hiện thiếu hàng; mất bao lâu và nguyên nhân lệch phổ biến là gì?
7. Quyết định nhập hàng hiện dựa trên nhớ/kinh nghiệm, đơn chào của NCC, quan sát kệ hay dữ liệu?
8. “Lợi nhuận” trong ngôn ngữ của họ là gì: tiền mặt còn lại, chênh giá bán–mua, hay lợi nhuận sau chi phí?
9. Báo cáo nào đang được xem thật; báo cáo nào được mua nhưng bỏ không?
10. Khi thấy một cảnh báo/khuyến nghị, bằng chứng nào khiến họ tin và hành động?
11. HĐĐT là trigger mua bắt buộc hay chỉ là chức năng phụ đối với phân khúc mục tiêu?
12. Ngưỡng giá và mô hình trả phí nào cạnh tranh với chi phí tiếp tục dùng sổ/Excel?

## 15. Recommended Next BA Questions

Nên dùng trong cuộc gọi/Zalo/demo từ xa với khách đầu tiên; ưu tiên câu hỏi về hành vi gần đây thay vì ý kiến chung.

1. “Lần gần nhất anh/chị nhập hàng là khi nào? Anh/chị đã quyết định nhập mặt hàng và số lượng bằng cách nào?”
2. “Lần gần nhất tồn trên sổ/phần mềm không khớp hàng trên kệ, anh/chị phát hiện ra sao và xử lý thế nào?”
3. “Cuối ngày anh/chị kiểm tra những con số nào? Có con số nào phải tính lại ngoài phần mềm không?”
4. “Khi muốn biết tháng này lời bao nhiêu, anh/chị làm từng bước thế nào? Có khoản nào thường bị bỏ sót?”
5. “Cho mình xem cách anh/chị tìm một mặt hàng, bán, nhận QR/tiền mặt và in/gửi phiếu.”
6. “Lần gần nhất khách trả hàng hoặc anh/chị hủy nhầm đơn, chuyện gì xảy ra với tiền và tồn?”
7. “Báo cáo nào anh/chị mở nhiều nhất? Sau khi xem, anh/chị đã thay đổi quyết định gì gần đây?”
8. “Có báo cáo nào nhiều số nhưng anh/chị vẫn không biết nên làm gì tiếp theo không? Cho một ví dụ gần nhất.”
9. “Nếu hệ thống nói ‘nên nhập thêm mặt hàng X’, anh/chị cần thấy dữ liệu nào để tin?”
10. “Điều gì khiến anh/chị dừng dùng một phần mềm sau khi thử: nhập liệu ban đầu, chậm/lỗi, không khớp tồn, khó hiểu hay chi phí?”
11. “HĐĐT hiện được làm ở đâu, ai làm, và phần khó nhất/lỗi gần nhất là gì?”
12. “Nếu chỉ trả tiền cho ba kết quả, anh/chị sẽ chọn ba kết quả nào?”

## 16. Recommendation to Product Owner

Không APPROVE persona/JTBD chỉ từ desk research này. Đề nghị Product Owner review theo ba quyết định:

1. **Chấp nhận hay thu hẹp primary persona candidate** theo quy mô, mức trưởng thành số và vai trò trực tiếp vận hành.
2. **Xếp hạng 8 JTBD candidate** theo importance và frequency; không xếp bằng số lượng feature của đối thủ.
3. **Giữ JTBD-7 (actionability) ở trạng thái hypothesis ưu tiên**, rồi kiểm chứng trong 5–10 demo/pilot đầu bằng task thực và willingness-to-pay, thay vì survey ý định.

## 17. Selected Source List

### Official / primary

- [Chính phủ — Nội dung mới của Nghị định 70/2025/NĐ-CP](https://xaydungchinhsach.chinhphu.vn/mot-so-noi-dung-moi-cua-nghi-dinh-so-70-2025-nd-cp-ve-hoa-don-chung-tu-119250403074719995.htm)
- [KiotViet Help Center](https://www.kiotviet.vn/ho-tro/)
- [KiotViet — Bán hàng](https://www.kiotviet.vn/huong-dan-su-dung-kiotviet/retail-ban-hang/ban-hang/)
- [KiotViet — Kiểm kho](https://www.kiotviet.vn/huong-dan-su-dung-kiotviet/retail-hang-hoa/kiem-kho/)
- [KiotViet — Trả hàng](https://www.kiotviet.vn/huong-dan-su-dung-kiotviet/retail-tra-hang/tra-hang/)
- [KiotViet — Báo cáo cuối ngày](https://www.kiotviet.vn/huong-dan-su-dung-kiotviet/huong-dan-bao-cao/bao-cao-cuoi-ngay)
- [KiotViet — Báo cáo hàng hóa](https://www.kiotviet.vn/huong-dan-su-dung-kiotviet/huong-dan-bao-cao/bao-cao-hang-hoa/)
- [Sapo Help Center](https://help.sapo.vn/)
- [Sapo — Tổng quan POS](https://help.sapo.vn/tong-quan-kenh-pos-ban-tai-quay)
- [Sapo — Quản lý sản phẩm và kho](https://help.sapo.vn/tong-quan-cac-tinh-nang-quan-ly-san-pham-kho-hang)
- [Sapo — Nhập file sản phẩm](https://help.sapo.vn/them-moi-san-pham-bang-file-excel-tren-phan-mem-quan-ly-ban-hang-sapo)
- [MISA eShop Help Center](https://helpeshop.misa.vn/)
- [MISA eShop — Khai báo hàng hóa](https://helpeshop.misa.vn/v1/kb/170100_khai_bao_hang_hoa)
- [MISA eShop — Bán hàng trên POS](https://helpeshop.misa.vn/kb/huong-dan-su-dung-misa-eshop-tren-may-pos)
- [MISA eShop — Tình hình lợi nhuận](https://helpeshop.misa.vn/v1/kb/170107_tinh_hinh_loi_nhuan)
- [POS365 Help Center](https://www.pos365.vn/docs)
- [POS365 — Hướng dẫn tạp hóa](https://www.youtube.com/watch?v=Zv96DDuqOMw)

### User feedback / secondary evidence

- [App Store — Sapo](https://apps.apple.com/vn/app/sapo-sales-management/id979090133)
- [App Store — KiotViet](https://apps.apple.com/vn/app/kiotviet/id1146695068?platform=watch)
- [App Store — MISA eShop Sale Phone](https://apps.apple.com/vn/app/misa-eshop-sale-phone/id1444979592?platform=iphone&see-all=reviews)
- [App Store — Sapo 6870](https://apps.apple.com/vn/app/sapo-6870/id6747013574?platform=iphone&see-all=reviews)
- [App Store — Nhanh](https://apps.apple.com/vn/app/1275801299?platform=iphone&see-all=reviews)
- [World Bank — Firm-Level Technology Adoption in Vietnam](https://documents1.worldbank.org/curated/en/498501615216149075/pdf/Firm-Level-Technology-Adoption-in-Vietnam.pdf)
