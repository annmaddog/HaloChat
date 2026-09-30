# Chữ ký số + Bảng "Thông tin kỹ thuật" — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thêm chữ ký số RSA-PSS/SHA-256 xác thực người gửi cho mọi tin nhắn Text, và 1 modal "Thông tin kỹ thuật" trên mỗi tin nhắn hiển thị trạng thái mã hóa, thời gian mã hóa/giải mã đo trực tiếp, và trạng thái chữ ký — phục vụ đúng 3 mục "Đánh giá" của đề bài.

**Architecture:** Backend thêm 1 service độc lập `DichVuChuKySo` (RSA-PSS, dùng lại khóa RSA hiện có) trong `HaloChat.Security`, tích hợp vào luồng gửi/đọc tin nhắn đã có sẵn ở `DichVuTinNhan` (không đổi giao thức SignalR). Endpoint REST mới `GET /api/tinnhan/{id}/thong-tin-ky-thuat` trả về số liệu đo trực tiếp (không lưu DB). Frontend thêm 1 icon + badge trên mỗi tin nhắn và 1 modal mới.

**Tech Stack:** ASP.NET Core .NET 9 (`backend/HaloChat.Api`, `backend/HaloChat.Security`), MongoDB Driver, xUnit; React 19 + TypeScript + Vite (`frontend/`), Vitest + Testing Library.

**Spec:** `docs/superpowers/specs/2026-09-30-halochat-chu-ky-so-design.md`

## Global Constraints

- Chỉ áp dụng chữ ký cho tin nhắn loại `Text` — Ảnh/File không ký (spec §2).
- Điều kiện ký: CHỈ cần người gửi có `KhoaBiMat` — khác điều kiện mã hóa (không cần mọi người tham gia có khóa) (spec §2).
- Dùng lại đúng cặp khóa RSA hiện có (`KhoaCongKhai`/`KhoaBiMat`) — không sinh khóa ký riêng (spec §2).
- Thuật toán ký: RSA-PSS (`RSASignaturePadding.Pss`) + SHA-256 (spec §3).
- Ký/xác minh trên đúng giá trị ĐANG LƯU ở `NoiDungTinNhan` (plaintext hoặc bản mã, tùy tin có mã hóa hay không) — KHÔNG phải trên plaintext sau khi giải mã (spec §4).
- Tin nhắn cũ không có chữ ký — không hồi tố, không lỗi (spec §2).
- Không thêm phương thức/sự kiện SignalR mới — chữ ký gắn tự động vào luồng gửi/đọc có sẵn (spec §2).
- "Thông tin kỹ thuật": thời gian mã hóa/giải mã đo LẠI ngay lúc bấm xem, KHÔNG lưu vào MongoDB, KHÔNG thêm field lưu trữ (spec §8).
- Tin đã thu hồi hoặc không phải Text → bảng "Thông tin kỹ thuật" trả `ApDungDuoc = false`, không phân tích nội dung (spec §8).

## Review Focus

- **Người gửi bị xóa tài khoản sau khi đã ký tin, người khác đọc lại tin đó** — `_khoNguoiDung.TimTheoIdAsync(t.NguoiGuiId)` trả `null`; hàm xác minh chữ ký phải trả `false` (không throw `NullReferenceException`), không phải trả `null`/coi như "không có chữ ký" (đã CÓ chữ ký, chỉ là không xác minh được) — Task 3 test riêng trường hợp này.
- **Chữ ký hợp lệ nhưng nội dung đã lưu bị sửa trực tiếp trong MongoDB (không qua API)** — `XacMinhChuKy` phải trả `false`, không throw ra ngoài làm sập request đọc tin nhắn — Task 3 test bằng cách sửa `NoiDungTinNhan` sau khi đã gọi `GuiTinNhanAsync`.
- **Gọi "Thông tin kỹ thuật" cho tin nhắn đã mã hóa nhưng người xem KHÔNG có bản khóa phiên của mình** (ví dụ thành viên nhóm vào sau, giống hành vi `GiaiMaNoiDungThucTe` trả "[Không thể giải mã]") — `LayThongTinKyThuatAsync` phải trả `ApDungDuoc = true, DaMaHoa = true` nhưng các field thời gian/kích thước/bản mã đều `null` thay vì throw — Task 5 test riêng.
- **`ChuKySo` là chuỗi rác/không đúng định dạng Base64** (dữ liệu hỏng, hiếm nhưng có thể xảy ra nếu ai đó sửa tay MongoDB) — `XacMinhChuKy` phải bắt được `FormatException` từ `Convert.FromBase64String` và trả `false`, không throw — Task 1 test riêng (đã có trong spec §3 nhưng dễ bị bỏ sót phần `FormatException` cụ thể, không chỉ `CryptographicException`).
- **Gọi endpoint `/thong-tin-ky-thuat` cho tin nhắn của người khác (không phải người gửi/người nhận/thành viên nhóm)** — phải trả lỗi quyền hạn (403/404 tùy loại), không được lộ thông tin kỹ thuật (kích thước, thời gian, trạng thái chữ ký) của tin nhắn người khác — Task 5 + Task 6 test riêng, tái dùng đúng `KiemTraQuyenTrenTinNhanAsync` đã có.

---

## Task 1: `DichVuChuKySo` — service ký/xác minh độc lập

**Files:**
- Create: `backend/HaloChat.Security/DichVuChuKySo/IDichVuChuKySo.cs`
- Create: `backend/HaloChat.Security/DichVuChuKySo/DichVuChuKySo.cs`
- Create: `backend/HaloChat.Api.Tests/Services/DichVuChuKySoTests.cs`
- Modify: `backend/HaloChat.Api/Program.cs` (đăng ký DI)

**Interfaces:**
- Produces: `IDichVuChuKySo.KyDuLieu(string noiDung, string khoaBiMatNguoiKy) : string`, `IDichVuChuKySo.XacMinhChuKy(string noiDung, string chuKy, string khoaCongKhaiNguoiKy) : bool` — dùng bởi Task 2, 3, 5.

- [ ] **Step 1: Viết interface**

`backend/HaloChat.Security/DichVuChuKySo/IDichVuChuKySo.cs`:
```csharp
namespace HaloChat.Security;

/// <summary>
/// [Chữ ký số] Xác thực NGƯỜI GỬI + TOÀN VẸN nội dung tin nhắn — bổ sung cho
/// DichVuMaHoa (giấu nội dung), không thay thế. Dùng lại đúng cặp khóa RSA hiện
/// có (KhoaCongKhai/KhoaBiMat) — xem docs/superpowers/specs/2026-09-30-halochat-chu-ky-so-design.md §3.
/// </summary>
public interface IDichVuChuKySo
{
    /// <summary>Ký noiDung bằng khóa bí mật RSA (Base64) của người gửi — trả chữ ký Base64.</summary>
    string KyDuLieu(string noiDung, string khoaBiMatNguoiKy);

    /// <summary>Xác minh chuKy (Base64) trên đúng noiDung bằng khóa công khai RSA (Base64) của người ký.</summary>
    bool XacMinhChuKy(string noiDung, string chuKy, string khoaCongKhaiNguoiKy);
}
```

- [ ] **Step 2: Viết test round-trip (RED trước khi có cài đặt)**

`backend/HaloChat.Api.Tests/Services/DichVuChuKySoTests.cs`:
```csharp
using HaloChat.Security;
using Xunit;

namespace HaloChat.Api.Tests.Services;

public class DichVuChuKySoTests
{
    private static DichVuChuKySo TaoDichVu() => new();
    private static DichVuMaHoa TaoDichVuMaHoa() => new();

    [Fact]
    public void KyRoiXacMinh_DungCapKhoa_TraVeTrue()
    {
        var dichVu = TaoDichVu();
        var (khoaCongKhai, khoaBiMat) = TaoDichVuMaHoa().SinhCapKhoaRsa();
        const string noiDung = "Chào Bình, tối nay học mật mã nhé!";

        var chuKy = dichVu.KyDuLieu(noiDung, khoaBiMat);
        var hopLe = dichVu.XacMinhChuKy(noiDung, chuKy, khoaCongKhai);

        Assert.True(hopLe);
    }

    [Fact]
    public void KyRoiXacMinh_DoiMotKyTuNoiDung_TraVeFalse()
    {
        var dichVu = TaoDichVu();
        var (khoaCongKhai, khoaBiMat) = TaoDichVuMaHoa().SinhCapKhoaRsa();
        var chuKy = dichVu.KyDuLieu("Nội dung gốc", khoaBiMat);

        var hopLe = dichVu.XacMinhChuKy("Nội dung gốc bị sửa", chuKy, khoaCongKhai);

        Assert.False(hopLe);
    }

    [Fact]
    public void XacMinh_SaiKhoaCongKhai_TraVeFalse()
    {
        var dichVu = TaoDichVu();
        var maHoa = TaoDichVuMaHoa();
        var (_, khoaBiMatDung) = maHoa.SinhCapKhoaRsa();
        var (khoaCongKhaiSai, _) = maHoa.SinhCapKhoaRsa();
        const string noiDung = "Xin chào";
        var chuKy = dichVu.KyDuLieu(noiDung, khoaBiMatDung);

        var hopLe = dichVu.XacMinhChuKy(noiDung, chuKy, khoaCongKhaiSai);

        Assert.False(hopLe);
    }

    [Fact]
    public void XacMinh_ChuKyLaChuoiRacKhongPhaiBase64_TraVeFalseKhongNemLoi()
    {
        var dichVu = TaoDichVu();
        var (khoaCongKhai, _) = TaoDichVuMaHoa().SinhCapKhoaRsa();

        var hopLe = dichVu.XacMinhChuKy("Xin chào", "***không phải base64***", khoaCongKhai);

        Assert.False(hopLe);
    }

    [Fact]
    public void XacMinh_ChuKyLaBase64HopLeNhungKhongPhaiChuKyThat_TraVeFalseKhongNemLoi()
    {
        var dichVu = TaoDichVu();
        var (khoaCongKhai, _) = TaoDichVuMaHoa().SinhCapKhoaRsa();
        var chuKyRac = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 });

        var hopLe = dichVu.XacMinhChuKy("Xin chào", chuKyRac, khoaCongKhai);

        Assert.False(hopLe);
    }

    [Fact]
    public void KyHaiLanCungNoiDung_ChuKyKhacNhau()
    {
        // RSA-PSS dùng "salt" ngẫu nhiên nên cùng nội dung + cùng khóa vẫn ra 2 chữ ký khác nhau.
        var dichVu = TaoDichVu();
        var (_, khoaBiMat) = TaoDichVuMaHoa().SinhCapKhoaRsa();
        const string noiDung = "Xin chào";

        var chuKy1 = dichVu.KyDuLieu(noiDung, khoaBiMat);
        var chuKy2 = dichVu.KyDuLieu(noiDung, khoaBiMat);

        Assert.NotEqual(chuKy1, chuKy2);
    }
}
```

- [ ] **Step 3: Chạy test, xác nhận FAIL (không biên dịch được vì chưa có `DichVuChuKySo`)**

Run: `cd backend && dotnet test --filter "DichVuChuKySoTests"`
Expected: FAIL biên dịch — `The type or namespace name 'DichVuChuKySo' could not be found`.

- [ ] **Step 4: Viết cài đặt**

`backend/HaloChat.Security/DichVuChuKySo/DichVuChuKySo.cs`:
```csharp
using System.Security.Cryptography;
using System.Text;

namespace HaloChat.Security;

/// <summary>Cài đặt thật cho IDichVuChuKySo — RSA-PSS + SHA-256, dùng lại định dạng khóa PKCS#1 (Base64) giống DichVuMaHoa.</summary>
public class DichVuChuKySo : IDichVuChuKySo
{
    public string KyDuLieu(string noiDung, string khoaBiMatNguoiKy)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(Convert.FromBase64String(khoaBiMatNguoiKy), out _);
        var duLieu = Encoding.UTF8.GetBytes(noiDung);
        var chuKy = rsa.SignData(duLieu, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        return Convert.ToBase64String(chuKy);
    }

    public bool XacMinhChuKy(string noiDung, string chuKy, string khoaCongKhaiNguoiKy)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportRSAPublicKey(Convert.FromBase64String(khoaCongKhaiNguoiKy), out _);
            var duLieu = Encoding.UTF8.GetBytes(noiDung);
            var chuKyBytes = Convert.FromBase64String(chuKy);
            return rsa.VerifyData(duLieu, chuKyBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            // Chữ ký/khóa hỏng định dạng (Base64 sai, độ dài khóa sai...) → coi như
            // không hợp lệ thay vì làm sập request đọc tin nhắn.
            return false;
        }
    }
}
```

- [ ] **Step 5: Chạy test, xác nhận PASS**

Run: `cd backend && dotnet test --filter "DichVuChuKySoTests"`
Expected: `Passed! - Failed: 0, Passed: 6`

- [ ] **Step 6: Đăng ký DI**

`backend/HaloChat.Api/Program.cs` — thêm ngay sau dòng `builder.Services.AddScoped<IDichVuMaHoa, DichVuMaHoa>();`:
```csharp
builder.Services.AddScoped<IDichVuChuKySo, DichVuChuKySo>();
```

- [ ] **Step 7: Build toàn solution để chắc chắn Program.cs vẫn biên dịch**

Run: `cd backend && dotnet build`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 8: Commit**

```bash
git add backend/HaloChat.Security/DichVuChuKySo backend/HaloChat.Api.Tests/Services/DichVuChuKySoTests.cs backend/HaloChat.Api/Program.cs
git commit -m "feat(backend): them DichVuChuKySo (RSA-PSS/SHA-256) doc lap voi DichVuMaHoa"
```

---

## Task 2: Ký tin nhắn lúc gửi (`DichVuTinNhan.GuiTinNhanAsync`)

**Files:**
- Modify: `backend/HaloChat.Api/Models/TinNhan.cs`
- Modify: `backend/HaloChat.Api/Services/DichVuTinNhan.cs`
- Modify: `backend/HaloChat.Api.Tests/Services/DichVuTinNhanTests.cs`

**Interfaces:**
- Consumes: `IDichVuChuKySo.KyDuLieu(string, string) : string` (Task 1).
- Produces: `TinNhan.ChuKySo : string?` — dùng bởi Task 3, 5.

- [ ] **Step 1: Thêm field vào Model**

`backend/HaloChat.Api/Models/TinNhan.cs` — thêm vào cuối class `TinNhan` (sau `AuthTag`):
```csharp
    // [Chữ ký số] Base64 chữ ký RSA-PSS/SHA-256 của NGƯỜI GỬI trên chính nội dung đang lưu ở
    // NoiDungTinNhan (plaintext hoặc bản mã, tùy tin có mã hóa hay không) — null khi người gửi
    // chưa có khóa RSA lúc gửi (tài khoản cũ, dữ liệu test) hoặc tin không phải loại Text.
    public string? ChuKySo { get; set; }
```

- [ ] **Step 2: Viết test RED — gửi tin bởi người gửi CÓ khóa thì ChuKySo khác null**

Thêm vào `backend/HaloChat.Api.Tests/Services/DichVuTinNhanTests.cs`, cuối class (trước dấu `}` đóng cuối file — tìm đúng vị trí bằng cách đọc file trước khi sửa, đặt cạnh nhóm test "Mã hóa lai RSA-AES (GĐ6)" đã có):
```csharp
    // --- Chữ ký số ---

    [Fact]
    public async Task GuiTinNhanAsync_NguoiGuiCoKhoaRsa_ChuKySoKhacNull()
    {
        var (dichVu, khoTinNhan, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiDungCoKhoaRsa(IdNguoiGui, "NguoiGui"));
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());

        await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Xin chào", null, null, null, null, null);

        var tinNhanTrongKho = Assert.Single(khoTinNhan.DanhSach);
        Assert.False(string.IsNullOrEmpty(tinNhanTrongKho.ChuKySo));
    }

    [Fact]
    public async Task GuiTinNhanAsync_NguoiGuiChuaCoKhoaRsa_ChuKySoRongDuKhongMaHoa()
    {
        // Người nhận CÓ khóa (đủ điều kiện nếu logic ký lỡ dùng nhầm điều kiện mã hóa),
        // nhưng người gửi thì KHÔNG — ChuKySo phải rỗng vì ký chỉ phụ thuộc người GỬI.
        var (dichVu, khoTinNhan, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiDungCoKhoaRsa(IdNguoiNhan, "NguoiNhan"));

        await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Xin chào", null, null, null, null, null);

        var tinNhanTrongKho = Assert.Single(khoTinNhan.DanhSach);
        Assert.Null(tinNhanTrongKho.ChuKySo);
    }

    [Fact]
    public async Task GuiTinNhanAsync_LoaiAnh_KhongKy()
    {
        var (dichVu, khoTinNhan, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiDungCoKhoaRsa(IdNguoiGui, "NguoiGui"));
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());

        await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Anh", "", "/api/tinnhan/file/507f1f77bcf86cd799439003", "a.png", 1024, "image/png", null);

        var tinNhanTrongKho = Assert.Single(khoTinNhan.DanhSach);
        Assert.Null(tinNhanTrongKho.ChuKySo);
    }
```

- [ ] **Step 3: Chạy test, xác nhận 2 test đầu FAIL (chưa ký), test loại Ảnh PASS (vốn đã null theo mặc định)**

Run: `cd backend && dotnet test --filter "GuiTinNhanAsync_NguoiGuiCoKhoaRsa_ChuKySoKhacNull|GuiTinNhanAsync_NguoiGuiChuaCoKhoaRsa_ChuKySoRongDuKhongMaHoa|GuiTinNhanAsync_LoaiAnh_KhongKy" -v d`
Expected: `GuiTinNhanAsync_NguoiGuiCoKhoaRsa_ChuKySoKhacNull` FAIL (`Assert.False` nhận `true` — chuỗi rỗng/null), 2 test còn lại PASS.

- [ ] **Step 4: Thêm dependency + hàm ký vào DichVuTinNhan**

`backend/HaloChat.Api/Services/DichVuTinNhan.cs` — thêm `using HaloChat.Security;` đã có sẵn (không cần thêm). Sửa field/constructor:
```csharp
    private readonly IDichVuMaHoa _dichVuMaHoa;
    private readonly IDichVuChuKySo _dichVuChuKySo;

    public DichVuTinNhan(
        ITinNhanRepository khoTinNhan, INguoiDungRepository khoNguoiDung, ILoiMoiKetBanRepository khoLoiMoiKetBan,
        INhomRepository khoNhom, IDocNhomRepository khoDocNhom, IQuanLyKetNoiChat quanLyKetNoi,
        ITinNhanAnRepository khoTinNhanAn, IDichVuMaHoa dichVuMaHoa, IDichVuChuKySo dichVuChuKySo)
    {
        _khoTinNhan = khoTinNhan;
        _khoNguoiDung = khoNguoiDung;
        _khoLoiMoiKetBan = khoLoiMoiKetBan;
        _khoNhom = khoNhom;
        _khoDocNhom = khoDocNhom;
        _quanLyKetNoi = quanLyKetNoi;
        _khoTinNhanAn = khoTinNhanAn;
        _dichVuMaHoa = dichVuMaHoa;
        _dichVuChuKySo = dichVuChuKySo;
    }
```

Trong `GuiTinNhanAsync`, ngay sau dòng `MaHoaNoiDungNeuCoThe(tinNhan, tinNhan.NoiDungTinNhan, nguoiThamGia, nguoiGui, soNguoiThamGiaDuKien);` và TRƯỚC `await _khoTinNhan.ThemMoiAsync(tinNhan);`, thêm:
```csharp
        KyTinNhanNeuCoThe(tinNhan, nguoiGui);
```

Thêm hàm mới ngay sau `MaHoaNoiDungNeuCoThe` (trước `GiaiMaNoiDungThucTe`):
```csharp
    /// <summary>
    /// [Chữ ký số] Ký NỘI DUNG ĐANG LƯU (tinNhan.NoiDungTinNhan — đã ở giá trị cuối cùng sau
    /// MaHoaNoiDungNeuCoThe ở trên, có thể là plaintext hoặc bản mã) bằng khóa bí mật của NGƯỜI
    /// GỬI. Điều kiện ký khác điều kiện mã hóa: chỉ cần NGƯỜI GỬI có khóa (không cần biết khóa
    /// người nhận, vì ký là hành động 1 chiều — ai cũng xác minh được bằng khóa công khai người gửi).
    /// </summary>
    private void KyTinNhanNeuCoThe(TinNhan tinNhan, NguoiDung? nguoiGui)
    {
        if (tinNhan.LoaiTinNhan != LoaiTinNhan.Text || nguoiGui is null || string.IsNullOrWhiteSpace(nguoiGui.KhoaBiMat))
        {
            return;
        }

        tinNhan.ChuKySo = _dichVuChuKySo.KyDuLieu(tinNhan.NoiDungTinNhan, nguoiGui.KhoaBiMat);
    }
```

- [ ] **Step 5: Cập nhật nơi khởi tạo `DichVuTinNhan` trong test (thêm tham số thứ 9)**

`backend/HaloChat.Api.Tests/Services/DichVuTinNhanTests.cs` — sửa `TaoDichVu()`:
```csharp
        var dichVu = new DichVuTinNhan(khoTinNhan, khoNguoiDung, khoLoiMoiKetBan, khoNhom, khoDocNhom, quanLyKetNoi, khoTinNhanAn, new DichVuMaHoa(), new DichVuChuKySo());
```

- [ ] **Step 6: Chạy lại đúng 3 test, xác nhận PASS**

Run: `cd backend && dotnet test --filter "GuiTinNhanAsync_NguoiGuiCoKhoaRsa_ChuKySoKhacNull|GuiTinNhanAsync_NguoiGuiChuaCoKhoaRsa_ChuKySoRongDuKhongMaHoa|GuiTinNhanAsync_LoaiAnh_KhongKy"`
Expected: `Passed! - Failed: 0, Passed: 3`

- [ ] **Step 7: Build + chạy TOÀN BỘ test backend để chắc chắn không có nơi nào khác khởi tạo `DichVuTinNhan` thiếu tham số mới**

Run: `cd backend && dotnet build && dotnet test`
Expected: build sạch; nếu còn lỗi biên dịch ở chỗ khởi tạo `DichVuTinNhan` khác (ví dụ test tích hợp `ThietLapKiemThuTichHop.cs` hoặc `ChatHubTests.cs` dùng DI thật thì không cần sửa gì — DI tự inject qua `Program.cs` đã đăng ký ở Task 1; chỉ sửa nếu có chỗ `new DichVuTinNhan(...)` thủ công khác ngoài file test đã sửa).

- [ ] **Step 8: Commit**

```bash
git add backend/HaloChat.Api/Models/TinNhan.cs backend/HaloChat.Api/Services/DichVuTinNhan.cs backend/HaloChat.Api.Tests/Services/DichVuTinNhanTests.cs
git commit -m "feat(backend): ky tin nhan Text luc gui bang khoa bi mat nguoi gui"
```

---

## Task 3: Xác minh chữ ký lúc đọc tin (`AnhXaDto` + `TinNhanDto`)

**Files:**
- Modify: `backend/HaloChat.Api/Dto/TinNhanDto.cs`
- Modify: `backend/HaloChat.Api/Services/DichVuTinNhan.cs`
- Modify: `backend/HaloChat.Api.Tests/Services/DichVuTinNhanTests.cs`

**Interfaces:**
- Consumes: `TinNhan.ChuKySo` (Task 2), `IDichVuChuKySo.XacMinhChuKy` (Task 1).
- Produces: `TinNhanDto.DaXacThucChuKy : bool?` (field cuối cùng của record) — dùng bởi Task 4 (frontend), Task 5.
- **Thay đổi chữ ký hàm quan trọng cho các task sau:** `AnhXaDto` đổi từ `private TinNhanDto AnhXaDto(...)` thành `private async Task<TinNhanDto> AnhXaDto(...)` — mọi lệnh gọi phải thêm `await`. Thêm helper mới `private async Task<List<TinNhanDto>> AnhXaDanhSachDto(IEnumerable<TinNhan> danhSach, string idHienTai, string? khoaBiMatHienTai) : Task<List<TinNhanDto>>` thay cho pattern `.Select(t => AnhXaDto(...)).ToList()` cũ (không thể `.Select` với hàm async rồi `.ToList()` trực tiếp).

- [ ] **Step 1: Thêm field vào DTO**

`backend/HaloChat.Api/Dto/TinNhanDto.cs` — thay toàn bộ nội dung:
```csharp
namespace HaloChat.Api.Dto;

public record TinNhanDto(
    string Id,
    string NguoiGuiId,
    string? NguoiNhanId,
    string? NhomId,
    string LoaiTinNhan,
    string NoiDungTinNhan,
    string? DuongDanFile,
    string? TenFileGoc,
    long? KichThuocFile,
    string? LoaiFile,
    bool DaDoc,
    bool DaNhan,
    DateTime ThoiGianTao,
    TraLoiThongTinDto? TraLoi,
    bool DaThuHoi,
    bool DaGhim,
    DateTime? ThoiGianGhim,
    List<CamXucDto> DanhSachCamXuc,
    bool? DaXacThucChuKy);
```

- [ ] **Step 2: Viết test RED — xác minh chữ ký đúng/sai/không có**

Thêm vào `DichVuTinNhanTests.cs`, cạnh nhóm test chữ ký số vừa thêm ở Task 2:
```csharp
    [Fact]
    public async Task GuiTinNhanAsync_CoChuKy_DocLaiTraVeDaXacThucChuKyTrue()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiDungCoKhoaRsa(IdNguoiGui, "NguoiGui"));
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());

        var ketQua = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Xin chào", null, null, null, null, null);

        Assert.True(ketQua.DaXacThucChuKy);
    }

    [Fact]
    public async Task GuiTinNhanAsync_KhongCoChuKy_DocLaiTraVeDaXacThucChuKyNull()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());

        var ketQua = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Xin chào", null, null, null, null, null);

        Assert.Null(ketQua.DaXacThucChuKy);
    }

    [Fact]
    public async Task LayLichSuAsync_TinBiSuaNoiDungSauKhiKy_TraVeDaXacThucChuKyFalse()
    {
        var (dichVu, khoTinNhan, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiDungCoKhoaRsa(IdNguoiGui, "NguoiGui"));
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Xin chào", null, null, null, null, null);

        // Giả lập ai đó sửa thẳng dữ liệu trong Mongo sau khi tin đã được ký.
        khoTinNhan.DanhSach[0].NoiDungTinNhan = "Xin chào (đã bị sửa)";

        var lichSu = await dichVu.LayLichSuAsync(IdNguoiGui, IdNguoiNhan, null, 30);

        Assert.False(Assert.Single(lichSu).DaXacThucChuKy);
    }

    [Fact]
    public async Task LayLichSuAsync_NguoiGuiBiXoaSauKhiDaKy_TraVeDaXacThucChuKyFalseKhongNemLoi()
    {
        var (dichVu, khoTinNhan, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiDungCoKhoaRsa(IdNguoiGui, "NguoiGui"));
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Xin chào", null, null, null, null, null);

        // Giả lập tài khoản người gửi bị xóa hẳn khỏi hệ thống sau khi đã ký.
        khoNguoiDung.DanhSach.RemoveAll(nd => nd.Id == IdNguoiGui);

        var lichSu = await dichVu.LayLichSuAsync(IdNguoiNhan, IdNguoiGui, null, 30);

        Assert.False(Assert.Single(lichSu).DaXacThucChuKy);
    }
```

- [ ] **Step 3: Chạy test, xác nhận FAIL biên dịch (thiếu tham số `DaXacThucChuKy` trong mọi `new TinNhanDto(...)` positional hiện có ở `AnhXaDto`)**

Run: `cd backend && dotnet build`
Expected: FAIL — `CS7036: There is no argument given that corresponds to the required formal parameter 'DaXacThucChuKy'`.

- [ ] **Step 4: Viết hàm xác minh + sửa `AnhXaDto` thành async**

Trong `backend/HaloChat.Api/Services/DichVuTinNhan.cs`, thêm hàm mới ngay trước `AnhXaDto`:
```csharp
    /// <summary>
    /// [Chữ ký số] Xác minh chữ ký của NGƯỜI GỬI trên nội dung ĐANG LƯU (t.NoiDungTinNhan —
    /// plaintext hoặc bản mã tùy tin có mã hóa hay không), KHÔNG phải trên nội dung đã giải mã
    /// — vì chữ ký được tính lúc gửi TRÊN GIÁ TRỊ CUỐI CÙNG sẽ lưu (xem KyTinNhanNeuCoThe).
    /// Trả null khi tin không có chữ ký (tin cũ / không phải Text) — không hiển thị badge gì
    /// trên UI; true/false khi có chữ ký, kết quả xác minh đúng/sai.
    /// </summary>
    private async Task<bool?> XacMinhChuKyNeuCoThe(TinNhan t)
    {
        if (t.LoaiTinNhan != LoaiTinNhan.Text || string.IsNullOrEmpty(t.ChuKySo))
        {
            return null;
        }

        var nguoiGui = await _khoNguoiDung.TimTheoIdAsync(t.NguoiGuiId);
        if (nguoiGui is null || string.IsNullOrWhiteSpace(nguoiGui.KhoaCongKhai))
        {
            return false;
        }

        return _dichVuChuKySo.XacMinhChuKy(t.NoiDungTinNhan, t.ChuKySo, nguoiGui.KhoaCongKhai);
    }
```

Sửa toàn bộ `AnhXaDto` thành:
```csharp
    private async Task<TinNhanDto> AnhXaDto(TinNhan t, string idHienTai, string? khoaBiMatHienTai)
    {
        var traLoi = t.TraLoi is null ? null : new TraLoiThongTinDto(t.TraLoi.Id, t.TraLoi.TenNguoiGui, t.TraLoi.NoiDungTomTat, t.TraLoi.LoaiTinNhan.ToString());
        var danhSachCamXuc = t.DaThuHoi
            ? new List<CamXucDto>()
            : t.DanhSachCamXuc.Select(cx => new CamXucDto(cx.NguoiDungId, cx.LoaiCamXuc.ToString())).ToList();

        if (t.DaThuHoi)
        {
            return new(
                t.Id, t.NguoiGuiId, t.NguoiNhanId, t.NhomId, t.LoaiTinNhan.ToString(),
                "Tin nhắn đã được thu hồi.", null, null, null, null,
                t.DaDoc, t.DaNhan, t.ThoiGianTao, traLoi, t.DaThuHoi, t.DaGhim, t.ThoiGianGhim, danhSachCamXuc,
                null); // [Chữ ký số] Tin đã thu hồi không hiện trạng thái chữ ký.
        }

        // [GĐ6] Giải mã (nếu tin này có mã hóa) dưới góc nhìn của idHienTai
        // trước khi trả DTO — client không bao giờ thấy Ciphertext/khóa RSA,
        // chỉ thấy đúng NoiDungTinNhan như thể chưa từng mã hóa.
        var noiDungThucTe = GiaiMaNoiDungThucTe(t, idHienTai, khoaBiMatHienTai);
        var daXacThucChuKy = await XacMinhChuKyNeuCoThe(t);

        return new(
            t.Id, t.NguoiGuiId, t.NguoiNhanId, t.NhomId, t.LoaiTinNhan.ToString(), noiDungThucTe,
            t.DuongDanFile, t.TenFileGoc, t.KichThuocFile, t.LoaiFile, t.DaDoc, t.DaNhan, t.ThoiGianTao,
            traLoi, t.DaThuHoi, t.DaGhim, t.ThoiGianGhim, danhSachCamXuc, daXacThucChuKy);
    }

    /// <summary>Áp dụng AnhXaDto cho cả 1 danh sách — thay cho .Select(...).ToList() vì AnhXaDto giờ là async.</summary>
    private async Task<List<TinNhanDto>> AnhXaDanhSachDto(IEnumerable<TinNhan> danhSach, string idHienTai, string? khoaBiMatHienTai)
    {
        var ketQua = new List<TinNhanDto>();
        foreach (var t in danhSach)
        {
            ketQua.Add(await AnhXaDto(t, idHienTai, khoaBiMatHienTai));
        }
        return ketQua;
    }
```

- [ ] **Step 5: Cập nhật MỌI nơi gọi `AnhXaDto` trong `DichVuTinNhan.cs`**

Đọc lại toàn bộ file trước khi sửa (đã bị nhiều task khác chỉnh sửa) — tìm CHÍNH XÁC 2 kiểu lệnh gọi và sửa TỪNG chỗ như sau (không bỏ sót — có đúng 13 lệnh gọi `AnhXaDto` ngoài `AnhXaDto`/`AnhXaDanhSachDto` tự thân):

**Kiểu A — trả 1 tin (`return AnhXaDto(...)` → `return await AnhXaDto(...)`)**, áp dụng cho: `GuiTinNhanAsync`, `ThuHoiAsync`, `GhimAsync`, `BoGhimAsync`, `ThaCamXucAsync`, `BoCamXucAsync`. Ví dụ `GuiTinNhanAsync` (dòng cuối hàm):
```csharp
        await _khoTinNhan.ThemMoiAsync(tinNhan);
        return await AnhXaDto(tinNhan, nguoiGuiId, nguoiGui?.KhoaBiMat);
```
Và tương tự `ThuHoiAsync`:
```csharp
        await _khoTinNhan.DanhDauThuHoiAsync(tinNhanId);
        tinNhan.DaThuHoi = true;
        return await AnhXaDto(tinNhan, idHienTai, await LayKhoaBiMatAsync(idHienTai));
```
(Áp dụng đúng cùng 1 kiểu sửa — thêm `await` trước `AnhXaDto(` — cho `GhimAsync`, `BoGhimAsync`, `ThaCamXucAsync`, `BoCamXucAsync`.)

**Kiểu B — trả danh sách (`....Select(t => AnhXaDto(t, idHienTai, khoaBiMat)).ToList()` → `await AnhXaDanhSachDto(...)`)**, áp dụng cho 7 hàm: `LayLichSuAsync`, `LayLichSuNhomAsync`, `LayTinDaGhimTheoNguoiDungAsync`, `LayTinDaGhimTheoNhomAsync`, `LayMediaTheoNguoiDungAsync`, `LayMediaTheoNhomAsync`, `TimKiemTheoNguoiDungAsync`, `TimKiemTheoNhomAsync`. Ví dụ `LayLichSuAsync`:
```csharp
    public async Task<List<TinNhanDto>> LayLichSuAsync(string nguoiHienTaiId, string nguoiKiaId, string? truocId, int soLuong)
    {
        var lichSu = await _khoTinNhan.LayLichSuTheoNguoiDungAsync(nguoiHienTaiId, nguoiKiaId, truocId, soLuong);
        var idDaAn = await _khoTinNhanAn.LayDanhSachIdDaAnAsync(nguoiHienTaiId, lichSu.Select(t => t.Id));
        var khoaBiMat = await LayKhoaBiMatAsync(nguoiHienTaiId);
        return await AnhXaDanhSachDto(lichSu.Where(t => !idDaAn.Contains(t.Id)), nguoiHienTaiId, khoaBiMat);
    }
```
Áp dụng ĐÚNG CÙNG KIỂU (`return await AnhXaDanhSachDto(<biểu thức Where cũ, bỏ .Select(...).ToList()>, idHienTai, khoaBiMat);`) cho 6 hàm còn lại — mỗi hàm chỉ khác tên biến nguồn (`ghim`, `media`, `ketQua`) và tham số id, giữ nguyên logic `Where` đã có của từng hàm.

- [ ] **Step 6: Build, xác nhận không còn lỗi biên dịch**

Run: `cd backend && dotnet build`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 7: Chạy đúng các test mới, xác nhận PASS**

Run: `cd backend && dotnet test --filter "GuiTinNhanAsync_CoChuKy_DocLaiTraVeDaXacThucChuKyTrue|GuiTinNhanAsync_KhongCoChuKy_DocLaiTraVeDaXacThucChuKyNull|LayLichSuAsync_TinBiSuaNoiDungSauKhiKy_TraVeDaXacThucChuKyFalse|LayLichSuAsync_NguoiGuiBiXoaSauKhiDaKy_TraVeDaXacThucChuKyFalseKhongNemLoi"`
Expected: `Passed! - Failed: 0, Passed: 4`

- [ ] **Step 8: Chạy TOÀN BỘ test backend (đảm bảo không phá vỡ 225 test đã có do đổi chữ ký `AnhXaDto`)**

Run: `cd backend && dotnet test`
Expected: tất cả PASS, tổng số test = số cũ (225) + số test mới thêm ở Task 1, 2, 3.

- [ ] **Step 9: Commit**

```bash
git add backend/HaloChat.Api/Dto/TinNhanDto.cs backend/HaloChat.Api/Services/DichVuTinNhan.cs backend/HaloChat.Api.Tests/Services/DichVuTinNhanTests.cs
git commit -m "feat(backend): xac minh chu ky so luc doc tin, tra DaXacThucChuKy trong DTO"
```

---

## Task 4: Frontend — icon trạng thái chữ ký trên tin nhắn

**Files:**
- Modify: `frontend/src/KieuDuLieu.ts`
- Modify: `frontend/src/ThanhPhan/BieuTuong.tsx`
- Modify: `frontend/src/ThanhPhan/KhungTinNhan.tsx`
- Modify: `frontend/src/ThanhPhan/KhungTinNhan.css`
- Modify: `frontend/src/ThanhPhan/KhungTinNhan.test.tsx`

**Interfaces:**
- Consumes: `TinNhanDto.DaXacThucChuKy` (Task 3, qua JSON — JS nhận `boolean | null`).
- Produces: `TinNhan.daXacThucChuKy: boolean | null` trong `KieuDuLieu.ts` — dùng bởi Task 7 nếu cần tham chiếu chéo (không bắt buộc).

- [ ] **Step 1: Thêm field vào interface `TinNhan`**

`frontend/src/KieuDuLieu.ts` — thêm vào cuối interface `TinNhan` (sau `danhSachCamXuc: CamXuc[];`):
```typescript
  daXacThucChuKy: boolean | null;
}
```
(đổi dấu `}` đóng interface đang có thành sau dòng mới thêm — tức chỉ chèn thêm 1 dòng trước dấu đóng interface hiện tại).

- [ ] **Step 2: Viết 2 icon SVG mới**

`frontend/src/ThanhPhan/BieuTuong.tsx` — thêm 2 hàm mới, đặt cạnh `BieuTuongChuyenTiep`:
```typescript
export function BieuTuongChuKyHopLe() {
  return (
    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3">
      <polyline points="20 6 9 17 4 12" />
    </svg>
  );
}

export function BieuTuongChuKyKhongHopLe() {
  return (
    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
      <circle cx="12" cy="12" r="9" />
      <line x1="12" y1="8" x2="12" y2="13" />
      <line x1="12" y1="16" x2="12.01" y2="16" />
    </svg>
  );
}
```

- [ ] **Step 3: Viết test RED — hiện đúng icon theo 3 trạng thái**

Đọc `frontend/src/ThanhPhan/KhungTinNhan.test.tsx` trước để biết chính xác cách các test khác dựng `TIN_NHAN_MAU`/`PROPS_MAC_DINH` và cách click để hiện giờ (`tinDangMoId`) nếu icon đặt cạnh giờ — sau đó thêm 3 test theo đúng pattern đó, ví dụ (điều chỉnh tên biến/hàm cho khớp thực tế trong file):
```typescript
  it('tin co chu ky hop le hien icon xac thuc mau xanh', () => {
    const tinCoChuKyHopLe = { ...TIN_NHAN_MAU, id: 'm1', daXacThucChuKy: true };
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinCoChuKyHopLe]} />);

    expect(screen.getByLabelText('Đã xác thực chữ ký người gửi')).toBeInTheDocument();
  });

  it('tin co chu ky khong hop le hien icon canh bao', () => {
    const tinChuKySai = { ...TIN_NHAN_MAU, id: 'm1', daXacThucChuKy: false };
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinChuKySai]} />);

    expect(screen.getByLabelText('Chữ ký không hợp lệ — nội dung có thể đã bị thay đổi')).toBeInTheDocument();
  });

  it('tin khong co chu ky khong hien icon nao', () => {
    const tinKhongKy = { ...TIN_NHAN_MAU, id: 'm1', daXacThucChuKy: null };
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinKhongKy]} />);

    expect(screen.queryByLabelText('Đã xác thực chữ ký người gửi')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Chữ ký không hợp lệ — nội dung có thể đã bị thay đổi')).not.toBeInTheDocument();
  });
```
Cũng cần thêm `daXacThucChuKy: null` vào object `TIN_NHAN_MAU` đã có trong file (nếu nó liệt kê đủ field của interface `TinNhan` theo kiểu tường minh — kiểm tra bằng cách đọc file trước).

- [ ] **Step 4: Chạy test, xác nhận FAIL**

Run: `cd frontend && npx vitest run src/ThanhPhan/KhungTinNhan.test.tsx`
Expected: FAIL biên dịch (thiếu `daXacThucChuKy` trong `TIN_NHAN_MAU` nếu áp dụng) hoặc FAIL vì không tìm thấy label.

- [ ] **Step 5: Thêm icon vào JSX — đặt trong `.khung-tin-nhan__chan`, cạnh `.khung-tin-nhan__thoi-gian-chan`**

`frontend/src/ThanhPhan/KhungTinNhan.tsx` — thêm import:
```typescript
import { BieuTuongGhim, BieuTuongTraLoi, BieuTuongChuyenTiep, BieuTuongChuKyHopLe, BieuTuongChuKyKhongHopLe, BieuTuongTaiLieu, BieuTuongTai, BieuTuongBaCham, BieuTuongMatCuoi, BieuTuongKhoLuuTru, BieuTuongTimKiem } from './BieuTuong';
```
Trong khối `.khung-tin-nhan__chan` (đọc lại file trước để lấy đúng vị trí `.khung-tin-nhan__thoi-gian-chan` hiện tại — nằm ngay sau `{tinDangMoId === tn.id && (...)}` mở `.chan`), thêm ngay sau đoạn thời gian:
```tsx
                  {tn.daXacThucChuKy === true && (
                    <span className="khung-tin-nhan__chu-ky khung-tin-nhan__chu-ky--hop-le" aria-label="Đã xác thực chữ ký người gửi" title="Đã xác thực chữ ký người gửi">
                      <BieuTuongChuKyHopLe />
                    </span>
                  )}
                  {tn.daXacThucChuKy === false && (
                    <span className="khung-tin-nhan__chu-ky khung-tin-nhan__chu-ky--khong-hop-le" aria-label="Chữ ký không hợp lệ — nội dung có thể đã bị thay đổi" title="Chữ ký không hợp lệ — nội dung có thể đã bị thay đổi">
                      <BieuTuongChuKyKhongHopLe />
                    </span>
                  )}
```

- [ ] **Step 6: Thêm CSS**

`frontend/src/ThanhPhan/KhungTinNhan.css` — thêm cạnh `.khung-tin-nhan__thoi-gian-chan`:
```css
.khung-tin-nhan__chu-ky {
  display: inline-flex;
  align-items: center;
  flex-shrink: 0;
}

.khung-tin-nhan__chu-ky--hop-le {
  color: var(--mau-chinh-dam);
}

.khung-tin-nhan__chu-ky--khong-hop-le {
  color: var(--mau-loi);
}
```

- [ ] **Step 7: Chạy test, xác nhận PASS**

Run: `cd frontend && npx vitest run src/ThanhPhan/KhungTinNhan.test.tsx`
Expected: tất cả PASS.

- [ ] **Step 8: Type-check + chạy toàn bộ test frontend**

Run: `cd frontend && npx tsc -b --noEmit && npx vitest run`
Expected: type-check sạch; tất cả test PASS (kể cả các test khác dùng `TinNhan` object literal — nếu FAIL vì thiếu field `daXacThucChuKy`, thêm `daXacThucChuKy: null` vào từng object đó, đọc thông báo lỗi TypeScript để biết chính xác file/dòng).

- [ ] **Step 9: Commit**

```bash
git add frontend/src/KieuDuLieu.ts frontend/src/ThanhPhan/BieuTuong.tsx frontend/src/ThanhPhan/KhungTinNhan.tsx frontend/src/ThanhPhan/KhungTinNhan.css frontend/src/ThanhPhan/KhungTinNhan.test.tsx
git commit -m "feat(frontend): hien icon trang thai chu ky so tren tin nhan"
```

---

## Task 5: `LayThongTinKyThuatAsync` — số liệu cho bảng "Thông tin kỹ thuật"

**Files:**
- Create: `backend/HaloChat.Api/Dto/ThongTinKyThuatDto.cs`
- Modify: `backend/HaloChat.Api/Services/IDichVuTinNhan.cs`
- Modify: `backend/HaloChat.Api/Services/DichVuTinNhan.cs`
- Modify: `backend/HaloChat.Api.Tests/Services/DichVuTinNhanTests.cs`

**Interfaces:**
- Consumes: `TinNhan.DanhSachKhoaPhien/CiphertextTinNhan/Nonce/AuthTag/ChuKySo`, `IDichVuMaHoa.GiaiMaKhoaPhien/GiaiMaTinNhan/SinhKhoaPhienAes/MaHoaTinNhan`, `XacMinhChuKyNeuCoThe` (private, Task 3).
- Produces: `ThongTinKyThuatDto`, `IDichVuTinNhan.LayThongTinKyThuatAsync(string idHienTai, string tinNhanId) : Task<ThongTinKyThuatDto>` — dùng bởi Task 6 (controller).

- [ ] **Step 1: Viết DTO**

`backend/HaloChat.Api/Dto/ThongTinKyThuatDto.cs`:
```csharp
namespace HaloChat.Api.Dto;

/// <summary>
/// [Thông tin kỹ thuật] Số liệu ĐO TRỰC TIẾP lúc bấm xem, không lưu MongoDB — xem
/// docs/superpowers/specs/2026-09-30-halochat-chu-ky-so-design.md §8.
/// </summary>
public record ThongTinKyThuatDto(
    bool ApDungDuoc,
    bool DaMaHoa,
    string? ThuatToanMaHoa,
    int? KichThuocGocByte,
    int? KichThuocMaHoaByte,
    double? TyLePhinh,
    string? CiphertextRutGon,
    string? NonceRutGon,
    string? AuthTagRutGon,
    double? ThoiGianMaHoaMs,
    double? ThoiGianGiaiMaMs,
    bool CoChuKy,
    bool? DaXacThucChuKy,
    string? ThuatToanChuKy);
```

- [ ] **Step 2: Thêm vào interface**

`backend/HaloChat.Api/Services/IDichVuTinNhan.cs` — thêm dòng cuối cùng trong interface:
```csharp
    Task<Dto.ThongTinKyThuatDto> LayThongTinKyThuatAsync(string idHienTai, string tinNhanId);
```

- [ ] **Step 3: Viết test RED**

Thêm vào `DichVuTinNhanTests.cs`, nhóm mới "Thông tin kỹ thuật":
```csharp
    // --- Thông tin kỹ thuật ---

    [Fact]
    public async Task LayThongTinKyThuatAsync_TinDaMaHoaVaCoChuKy_TraVeDuSoLieu()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiDungCoKhoaRsa(IdNguoiGui, "NguoiGui"));
        khoNguoiDung.DanhSach.Add(TaoNguoiDungCoKhoaRsa(IdNguoiNhan, "NguoiNhan"));
        var tinDaGui = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Chào Bình, tối nay học mật mã nhé!", null, null, null, null, null);

        var thongTin = await dichVu.LayThongTinKyThuatAsync(IdNguoiGui, tinDaGui.Id);

        Assert.True(thongTin.ApDungDuoc);
        Assert.True(thongTin.DaMaHoa);
        Assert.Equal("AES-256-GCM", thongTin.ThuatToanMaHoa);
        Assert.True(thongTin.KichThuocMaHoaByte > 0);
        Assert.True(thongTin.KichThuocGocByte > 0);
        Assert.NotNull(thongTin.ThoiGianMaHoaMs);
        Assert.NotNull(thongTin.ThoiGianGiaiMaMs);
        Assert.True(thongTin.CoChuKy);
        Assert.True(thongTin.DaXacThucChuKy);
        Assert.Equal("RSA-PSS / SHA-256", thongTin.ThuatToanChuKy);
    }

    [Fact]
    public async Task LayThongTinKyThuatAsync_TinKhongMaHoa_TraVeDaMaHoaFalseKhongCoThoiGian()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tinDaGui = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Xin chào", null, null, null, null, null);

        var thongTin = await dichVu.LayThongTinKyThuatAsync(IdNguoiGui, tinDaGui.Id);

        Assert.True(thongTin.ApDungDuoc);
        Assert.False(thongTin.DaMaHoa);
        Assert.Null(thongTin.ThuatToanMaHoa);
        Assert.Null(thongTin.ThoiGianMaHoaMs);
        Assert.Null(thongTin.ThoiGianGiaiMaMs);
        Assert.False(thongTin.CoChuKy);
    }

    [Fact]
    public async Task LayThongTinKyThuatAsync_TinDaThuHoi_TraVeApDungDuocFalse()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tinDaGui = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Xin chào", null, null, null, null, null);
        await dichVu.ThuHoiAsync(IdNguoiGui, tinDaGui.Id);

        var thongTin = await dichVu.LayThongTinKyThuatAsync(IdNguoiGui, tinDaGui.Id);

        Assert.False(thongTin.ApDungDuoc);
    }

    [Fact]
    public async Task LayThongTinKyThuatAsync_LoaiAnh_TraVeApDungDuocFalse()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tinDaGui = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Anh", "", "/api/tinnhan/file/507f1f77bcf86cd799439003", "a.png", 1024, "image/png", null);

        var thongTin = await dichVu.LayThongTinKyThuatAsync(IdNguoiGui, tinDaGui.Id);

        Assert.False(thongTin.ApDungDuoc);
    }

    [Fact]
    public async Task LayThongTinKyThuatAsync_NguoiXemKhongCoBanKhoaPhienCuaMinh_TraVeCacFieldMaHoaNull()
    {
        // Mô phỏng: tin đã mã hóa (2 người ban đầu có khóa), sau đó 1 người thứ 3
        // (không nằm trong DanhSachKhoaPhien) cố xem "Thông tin kỹ thuật" — ví dụ do
        // dữ liệu bất thường/lỗi ứng dụng khác gọi nhầm, không phải luồng bình thường
        // (bình thường KiemTraQuyenTrenTinNhanAsync đã chặn người ngoài cuộc trò chuyện,
        // nhưng ở tin 1-1 người GỬI/NHẬN luôn có quyền dù có thể thiếu khóa phiên riêng
        // nếu dữ liệu bị can thiệp tay — test này phòng hờ tình huống đó, không throw).
        var (dichVu, khoTinNhan, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiDungCoKhoaRsa(IdNguoiGui, "NguoiGui"));
        khoNguoiDung.DanhSach.Add(TaoNguoiDungCoKhoaRsa(IdNguoiNhan, "NguoiNhan"));
        var tinDaGui = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Xin chào", null, null, null, null, null);
        khoTinNhan.DanhSach[0].DanhSachKhoaPhien.RemoveAll(k => k.NguoiDungId == IdNguoiGui);

        var thongTin = await dichVu.LayThongTinKyThuatAsync(IdNguoiGui, tinDaGui.Id);

        Assert.True(thongTin.ApDungDuoc);
        Assert.True(thongTin.DaMaHoa);
        Assert.Null(thongTin.ThoiGianMaHoaMs);
        Assert.Null(thongTin.KichThuocGocByte);
    }

    [Fact]
    public async Task LayThongTinKyThuatAsync_NguoiKhongThuocCuocTroChuyen_NemNgoaiLeQuyenHan()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tinDaGui = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Xin chào", null, null, null, null, null);

        await Assert.ThrowsAsync<KhongCoQuyenTrenTinNhanException>(() =>
            dichVu.LayThongTinKyThuatAsync("nguoi-khong-lien-quan", tinDaGui.Id));
    }
```

- [ ] **Step 4: Chạy test, xác nhận FAIL biên dịch (chưa có `LayThongTinKyThuatAsync`)**

Run: `cd backend && dotnet build`
Expected: FAIL — `CS0535` hoặc lỗi tương tự do `IDichVuTinNhan` chưa được `DichVuTinNhan` cài đặt đủ.

- [ ] **Step 5: Viết cài đặt**

`backend/HaloChat.Api/Services/DichVuTinNhan.cs` — thêm `using System.Diagnostics;` và `using System.Text;` vào đầu file (cạnh các `using` hiện có). Thêm hàm mới, đặt sau `BoCamXucAsync` và trước `LayKhoaBiMatAsync`:
```csharp
    public async Task<ThongTinKyThuatDto> LayThongTinKyThuatAsync(string idHienTai, string tinNhanId)
    {
        var tinNhan = await _khoTinNhan.TimTheoIdAsync(tinNhanId) ?? throw new TinNhanKhongTonTaiException();
        await KiemTraQuyenTrenTinNhanAsync(idHienTai, tinNhan);

        if (tinNhan.DaThuHoi || tinNhan.LoaiTinNhan != LoaiTinNhan.Text)
        {
            return new ThongTinKyThuatDto(false, false, null, null, null, null, null, null, null, null, null, false, null, null);
        }

        var daMaHoa = tinNhan.DanhSachKhoaPhien.Count > 0;
        string? thuatToanMaHoa = null;
        int? kichThuocGoc = null;
        int? kichThuocMaHoa = null;
        double? tyLePhinh = null;
        string? ciphertextRutGon = null;
        string? nonceRutGon = null;
        string? authTagRutGon = null;
        double? thoiGianMaHoaMs = null;
        double? thoiGianGiaiMaMs = null;

        if (daMaHoa)
        {
            thuatToanMaHoa = "AES-256-GCM";
            var khoaCuaMinh = tinNhan.DanhSachKhoaPhien.FirstOrDefault(k => k.NguoiDungId == idHienTai);
            var khoaBiMatHienTai = await LayKhoaBiMatAsync(idHienTai);

            if (khoaCuaMinh is not null && !string.IsNullOrEmpty(khoaBiMatHienTai) &&
                !string.IsNullOrEmpty(tinNhan.CiphertextTinNhan) && !string.IsNullOrEmpty(tinNhan.Nonce) && !string.IsNullOrEmpty(tinNhan.AuthTag))
            {
                var khoaPhien = _dichVuMaHoa.GiaiMaKhoaPhien(khoaCuaMinh.KhoaPhienDaMaHoa, khoaBiMatHienTai);

                var dongHoGiaiMa = Stopwatch.StartNew();
                var plaintext = _dichVuMaHoa.GiaiMaTinNhan(tinNhan.CiphertextTinNhan, tinNhan.Nonce, tinNhan.AuthTag, khoaPhien);
                dongHoGiaiMa.Stop();
                thoiGianGiaiMaMs = dongHoGiaiMa.Elapsed.TotalMilliseconds;

                // Mã hóa LẠI bằng 1 khóa phiên MỚI chỉ để đo thời gian — không ghi đè
                // gì vào tinNhan/MongoDB, kết quả bỏ đi ngay sau khi đo (spec §8).
                var khoaPhienMoi = _dichVuMaHoa.SinhKhoaPhienAes();
                var dongHoMaHoa = Stopwatch.StartNew();
                _dichVuMaHoa.MaHoaTinNhan(plaintext, khoaPhienMoi);
                dongHoMaHoa.Stop();
                thoiGianMaHoaMs = dongHoMaHoa.Elapsed.TotalMilliseconds;

                kichThuocGoc = Encoding.UTF8.GetByteCount(plaintext);
                kichThuocMaHoa = Convert.FromBase64String(tinNhan.CiphertextTinNhan).Length;
                tyLePhinh = kichThuocGoc > 0 ? (double)kichThuocMaHoa / kichThuocGoc.Value : null;
                ciphertextRutGon = RutGonChuoi(tinNhan.CiphertextTinNhan);
                nonceRutGon = RutGonChuoi(tinNhan.Nonce);
                authTagRutGon = RutGonChuoi(tinNhan.AuthTag);
            }
        }
        else
        {
            kichThuocGoc = Encoding.UTF8.GetByteCount(tinNhan.NoiDungTinNhan);
        }

        var coChuKy = !string.IsNullOrEmpty(tinNhan.ChuKySo);
        bool? daXacThucChuKy = coChuKy ? await XacMinhChuKyNeuCoThe(tinNhan) : null;
        string? thuatToanChuKy = coChuKy ? "RSA-PSS / SHA-256" : null;

        return new ThongTinKyThuatDto(
            true, daMaHoa, thuatToanMaHoa, kichThuocGoc, kichThuocMaHoa, tyLePhinh,
            ciphertextRutGon, nonceRutGon, authTagRutGon, thoiGianMaHoaMs, thoiGianGiaiMaMs,
            coChuKy, daXacThucChuKy, thuatToanChuKy);
    }

    /// <summary>Rút gọn 1 chuỗi Base64 dài thành "10 ký tự đầu...10 ký tự cuối" để hiện trong "Thông tin kỹ thuật".</summary>
    private static string RutGonChuoi(string chuoi) =>
        chuoi.Length <= 24 ? chuoi : $"{chuoi[..10]}...{chuoi[^10..]}";
```

Thêm `using HaloChat.Api.Dto;` đã có sẵn ở đầu file (không cần thêm) — chỉ cần đảm bảo `ThongTinKyThuatDto` được tham chiếu đúng namespace `HaloChat.Api.Dto` (đã `using` sẵn).

- [ ] **Step 6: Chạy đúng các test mới, xác nhận PASS**

Run: `cd backend && dotnet test --filter "LayThongTinKyThuatAsync"`
Expected: `Passed! - Failed: 0, Passed: 6`

- [ ] **Step 7: Chạy toàn bộ test backend**

Run: `cd backend && dotnet test`
Expected: tất cả PASS.

- [ ] **Step 8: Commit**

```bash
git add backend/HaloChat.Api/Dto/ThongTinKyThuatDto.cs backend/HaloChat.Api/Services/IDichVuTinNhan.cs backend/HaloChat.Api/Services/DichVuTinNhan.cs backend/HaloChat.Api.Tests/Services/DichVuTinNhanTests.cs
git commit -m "feat(backend): LayThongTinKyThuatAsync - do truc tiep thoi gian ma hoa/giai ma + kich thuoc + trang thai chu ky"
```

---

## Task 6: Endpoint REST `GET /api/tinnhan/{id}/thong-tin-ky-thuat`

**Files:**
- Modify: `backend/HaloChat.Api/Controllers/TinNhanController.cs`
- Create: `backend/HaloChat.Api.Tests/TinNhanControllerTests.cs` (SỬA nếu file đã tồn tại — đọc trước để biết cấu trúc thật, file này nằm ở gốc `HaloChat.Api.Tests/` theo cấu trúc đã biết, không phải trong `Services/`)

**Interfaces:**
- Consumes: `IDichVuTinNhan.LayThongTinKyThuatAsync` (Task 5).
- Produces: endpoint HTTP `GET /api/tinnhan/{id}/thong-tin-ky-thuat` — dùng bởi Task 7 (frontend `DichVuApi.ts`).

- [ ] **Step 1: Đọc `TinNhanControllerTests.cs` hiện có để nắm đúng cách test tích hợp controller (WebApplicationFactory, cách đăng nhập lấy token, cách seed dữ liệu) trước khi viết test mới — KHÔNG đoán cấu trúc.**

- [ ] **Step 2: Thêm endpoint vào Controller**

`backend/HaloChat.Api/Controllers/TinNhanController.cs` — thêm method mới, đặt cạnh `An` (theo đúng nhóm route dạng `{id}/...`):
```csharp
    [HttpGet("{id}/thong-tin-ky-thuat")]
    public async Task<IActionResult> LayThongTinKyThuat(string id)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        if (!ObjectId.TryParse(id, out _))
        {
            return BadRequest(new { thongBao = "Id tin nhắn không hợp lệ." });
        }

        try
        {
            return Ok(await _dichVuTinNhan.LayThongTinKyThuatAsync(IdHienTai, id));
        }
        catch (TinNhanKhongTonTaiException loi) { return NotFound(new { thongBao = loi.Message }); }
        catch (KhongCoQuyenTrenTinNhanException loi) { return StatusCode(403, new { thongBao = loi.Message }); }
        catch (NhomKhongTonTaiException loi) { return NotFound(new { thongBao = loi.Message }); }
        catch (KhongPhaiThanhVienNhomException loi) { return StatusCode(403, new { thongBao = loi.Message }); }
    }
```

- [ ] **Step 3: Viết test tích hợp — theo ĐÚNG khuôn mẫu đã đọc ở Step 1** (ví dụ minh họa, THAY đúng cách khởi tạo client/đăng ký/đăng nhập thật đang dùng trong file đó — không copy nguyên văn nếu khuôn mẫu thật khác):

```csharp
    [Fact]
    public async Task LayThongTinKyThuat_TinCuaMinh_TraVe200()
    {
        // Đăng ký + đăng nhập 2 tài khoản, gửi 1 tin nhắn Text giữa họ (theo đúng
        // cách các test khác trong file này đang seed dữ liệu qua HTTP thật), rồi:
        var phanHoi = await _client.GetAsync($"/api/tinnhan/{{idTinNhanVuaGui}}/thong-tin-ky-thuat");
        Assert.Equal(System.Net.HttpStatusCode.OK, phanHoi.StatusCode);
    }

    [Fact]
    public async Task LayThongTinKyThuat_NguoiKhongLienQuan_TraVe403()
    {
        // Tài khoản thứ 3 không tham gia hội thoại gọi endpoint trên cùng id tin nhắn:
        var phanHoi = await _clientNguoiThu3.GetAsync($"/api/tinnhan/{{idTinNhanCuaNguoiKhac}}/thong-tin-ky-thuat");
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, phanHoi.StatusCode);
    }

    [Fact]
    public async Task LayThongTinKyThuat_IdKhongHopLe_TraVe400()
    {
        var phanHoi = await _client.GetAsync("/api/tinnhan/khong-phai-object-id/thong-tin-ky-thuat");
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, phanHoi.StatusCode);
    }
```

- [ ] **Step 4: Chạy test, xác nhận PASS (điều chỉnh code Step 3 theo đúng thực tế file nếu cần trước khi chạy)**

Run: `cd backend && dotnet test --filter "LayThongTinKyThuat"`
Expected: `Passed!`

- [ ] **Step 5: Chạy toàn bộ test backend**

Run: `cd backend && dotnet test`
Expected: tất cả PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/HaloChat.Api/Controllers/TinNhanController.cs backend/HaloChat.Api.Tests/TinNhanControllerTests.cs
git commit -m "feat(backend): endpoint GET /api/tinnhan/{id}/thong-tin-ky-thuat"
```

---

## Task 7: Frontend — modal "Thông tin kỹ thuật"

**Files:**
- Modify: `frontend/src/KieuDuLieu.ts`
- Modify: `frontend/src/DichVuApi.ts`
- Create: `frontend/src/ThanhPhan/ModalThongTinKyThuat.tsx`
- Create: `frontend/src/ThanhPhan/ModalThongTinKyThuat.css`
- Create: `frontend/src/ThanhPhan/ModalThongTinKyThuat.test.tsx`
- Modify: `frontend/src/ThanhPhan/KhungTinNhan.tsx`
- Modify: `frontend/src/Trang/TrangChat.tsx`
- Modify: `frontend/src/Trang/TrangNhom.tsx`

**Interfaces:**
- Consumes: `GET /api/tinnhan/{id}/thong-tin-ky-thuat` (Task 6).
- Produces: component `ModalThongTinKyThuat`, prop mới `onXemThongTinKyThuat: (id: string) => void` trên `KhungTinNhan`.

- [ ] **Step 1: Thêm interface + hàm gọi API**

`frontend/src/KieuDuLieu.ts` — thêm ở cuối file:
```typescript
export interface ThongTinKyThuat {
  apDungDuoc: boolean;
  daMaHoa: boolean;
  thuatToanMaHoa: string | null;
  kichThuocGocByte: number | null;
  kichThuocMaHoaByte: number | null;
  tyLePhinh: number | null;
  ciphertextRutGon: string | null;
  nonceRutGon: string | null;
  authTagRutGon: string | null;
  thoiGianMaHoaMs: number | null;
  thoiGianGiaiMaMs: number | null;
  coChuKy: boolean;
  daXacThucChuKy: boolean | null;
  thuatToanChuKy: string | null;
}
```

`frontend/src/DichVuApi.ts` — thêm `ThongTinKyThuat` vào import từ `./KieuDuLieu` ở đầu file, và thêm hàm mới (đặt cạnh các hàm `LayTin...` khác):
```typescript
export async function LayThongTinKyThuat(token: string, tinNhanId: string): Promise<ThongTinKyThuat> {
  return goiApi<ThongTinKyThuat>(`/tinnhan/${tinNhanId}/thong-tin-ky-thuat`, {
    headers: { Authorization: `Bearer ${token}` },
  });
}
```

- [ ] **Step 2: Viết component modal**

`frontend/src/ThanhPhan/ModalThongTinKyThuat.tsx`:
```typescript
import { useEffect, useState } from 'react';
import { LayThongTinKyThuat, LoiGoiApi } from '../DichVuApi';
import { BieuTuongDong } from './BieuTuong';
import type { ThongTinKyThuat } from '../KieuDuLieu';
import './ModalThongTinKyThuat.css';

interface PropsModalThongTinKyThuat {
  token: string;
  tinNhanId: string;
  onDong: () => void;
}

export function ModalThongTinKyThuat({ token, tinNhanId, onDong }: PropsModalThongTinKyThuat) {
  const [thongTin, setThongTin] = useState<ThongTinKyThuat | null>(null);
  const [dangTai, setDangTai] = useState(true);
  const [loi, setLoi] = useState<string | null>(null);

  useEffect(() => {
    setDangTai(true);
    setLoi(null);
    LayThongTinKyThuat(token, tinNhanId)
      .then(setThongTin)
      .catch((loiBat) => setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Không tải được thông tin kỹ thuật.'))
      .finally(() => setDangTai(false));
  }, [token, tinNhanId]);

  return (
    <div className="modal-thong-tin-ky-thuat__nen" onClick={onDong}>
      <div className="modal-thong-tin-ky-thuat__hop" onClick={(su) => su.stopPropagation()}>
        <div className="modal-thong-tin-ky-thuat__dau">
          <h2>Thông tin kỹ thuật</h2>
          <button type="button" onClick={onDong} aria-label="Đóng"><BieuTuongDong /></button>
        </div>

        {dangTai && <p className="modal-thong-tin-ky-thuat__trang-thai">Đang tải...</p>}
        {loi && <p className="thong-bao-loi" role="alert">{loi}</p>}

        {thongTin && !thongTin.apDungDuoc && (
          <p className="modal-thong-tin-ky-thuat__trang-thai">Không áp dụng cho loại tin nhắn này.</p>
        )}

        {thongTin && thongTin.apDungDuoc && (
          <>
            <section className="modal-thong-tin-ky-thuat__muc">
              <div className="modal-thong-tin-ky-thuat__muc-dau">
                <span>🔒 Mã hóa</span>
                <span className={`modal-thong-tin-ky-thuat__badge ${thongTin.daMaHoa ? 'modal-thong-tin-ky-thuat__badge--xanh' : ''}`}>
                  {thongTin.daMaHoa ? 'Đã mã hóa' : 'Không mã hóa'}
                </span>
              </div>
              {thongTin.daMaHoa && (
                <dl className="modal-thong-tin-ky-thuat__ds">
                  <dt>Thuật toán</dt><dd>{thongTin.thuatToanMaHoa}</dd>
                  {thongTin.kichThuocGocByte !== null && (<><dt>Kích thước gốc</dt><dd>{thongTin.kichThuocGocByte} byte</dd></>)}
                  {thongTin.kichThuocMaHoaByte !== null && (
                    <>
                      <dt>Kích thước sau mã hóa</dt>
                      <dd>{thongTin.kichThuocMaHoaByte} byte{thongTin.tyLePhinh !== null && ` (~${thongTin.tyLePhinh.toFixed(2)} lần)`}</dd>
                    </>
                  )}
                  {thongTin.ciphertextRutGon && (<><dt>Bản mã</dt><dd className="modal-thong-tin-ky-thuat__ma">{thongTin.ciphertextRutGon}</dd></>)}
                  {thongTin.nonceRutGon && (<><dt>Nonce</dt><dd className="modal-thong-tin-ky-thuat__ma">{thongTin.nonceRutGon}</dd></>)}
                  {thongTin.authTagRutGon && (<><dt>AuthTag</dt><dd className="modal-thong-tin-ky-thuat__ma">{thongTin.authTagRutGon}</dd></>)}
                </dl>
              )}
            </section>

            {thongTin.daMaHoa && (thongTin.thoiGianMaHoaMs !== null || thongTin.thoiGianGiaiMaMs !== null) && (
              <section className="modal-thong-tin-ky-thuat__muc">
                <div className="modal-thong-tin-ky-thuat__muc-dau"><span>⏱ Thời gian (đo lại ngay lúc bấm xem)</span></div>
                <dl className="modal-thong-tin-ky-thuat__ds">
                  {thongTin.thoiGianMaHoaMs !== null && (<><dt>Mã hóa lại để đo</dt><dd>{thongTin.thoiGianMaHoaMs.toFixed(3)} ms</dd></>)}
                  {thongTin.thoiGianGiaiMaMs !== null && (<><dt>Giải mã</dt><dd>{thongTin.thoiGianGiaiMaMs.toFixed(3)} ms</dd></>)}
                </dl>
              </section>
            )}

            <section className="modal-thong-tin-ky-thuat__muc">
              <div className="modal-thong-tin-ky-thuat__muc-dau">
                <span>✒ Chữ ký số</span>
                <span className={`modal-thong-tin-ky-thuat__badge ${thongTin.daXacThucChuKy === true ? 'modal-thong-tin-ky-thuat__badge--xanh' : thongTin.daXacThucChuKy === false ? 'modal-thong-tin-ky-thuat__badge--do' : ''}`}>
                  {thongTin.daXacThucChuKy === true ? '✓ Hợp lệ' : thongTin.daXacThucChuKy === false ? 'Không hợp lệ' : 'Không có chữ ký'}
                </span>
              </div>
              {thongTin.coChuKy && (
                <dl className="modal-thong-tin-ky-thuat__ds">
                  <dt>Thuật toán</dt><dd>{thongTin.thuatToanChuKy}</dd>
                </dl>
              )}
            </section>
          </>
        )}
      </div>
    </div>
  );
}
```

- [ ] **Step 3: Viết CSS**

`frontend/src/ThanhPhan/ModalThongTinKyThuat.css`:
```css
.modal-thong-tin-ky-thuat__nen {
  position: fixed;
  inset: 0;
  background: rgba(16, 27, 51, 0.4);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 30;
}

.modal-thong-tin-ky-thuat__hop {
  background: var(--mau-nen-the);
  border-radius: var(--ban-kinh-the);
  width: 400px;
  max-width: calc(100vw - 32px);
  max-height: min(600px, calc(100vh - 32px));
  overflow-y: auto;
  padding: 20px;
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.modal-thong-tin-ky-thuat__dau {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.modal-thong-tin-ky-thuat__dau h2 {
  margin: 0;
  font-size: 16px;
}

.modal-thong-tin-ky-thuat__dau button {
  border: none;
  background: none;
  cursor: pointer;
  color: var(--mau-chu-phu);
}

.modal-thong-tin-ky-thuat__trang-thai {
  color: var(--mau-chu-phu);
  font-size: 13px;
  text-align: center;
  margin: 12px 0;
}

.modal-thong-tin-ky-thuat__muc {
  border: 1px solid var(--mau-vien);
  border-radius: var(--ban-kinh-o);
  padding: 12px;
}

.modal-thong-tin-ky-thuat__muc-dau {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-weight: 700;
  font-size: 13px;
}

.modal-thong-tin-ky-thuat__badge {
  font-size: 11px;
  font-weight: 600;
  padding: 2px 8px;
  border-radius: 999px;
  background: var(--mau-nen-tren);
  color: var(--mau-chu-phu);
}

.modal-thong-tin-ky-thuat__badge--xanh {
  background: rgba(47, 123, 246, 0.15);
  color: var(--mau-chinh-dam);
}

.modal-thong-tin-ky-thuat__badge--do {
  background: rgba(214, 69, 69, 0.15);
  color: var(--mau-loi);
}

.modal-thong-tin-ky-thuat__ds {
  margin: 10px 0 0;
  display: grid;
  grid-template-columns: auto 1fr;
  gap: 4px 10px;
  font-size: 12px;
}

.modal-thong-tin-ky-thuat__ds dt {
  color: var(--mau-chu-phu);
}

.modal-thong-tin-ky-thuat__ds dd {
  margin: 0;
  text-align: right;
  word-break: break-all;
}

.modal-thong-tin-ky-thuat__ma {
  font-family: monospace;
  font-size: 11px;
}
```

- [ ] **Step 4: Viết test cho modal**

`frontend/src/ThanhPhan/ModalThongTinKyThuat.test.tsx` — đọc trước 1 test file modal đã có (ví dụ `ModalHoanTatHoSo.test.tsx` hoặc `PanelKhoMedia.test.tsx`) để lấy đúng cách mock `vi.spyOn(DichVuApi, ...)`, rồi viết theo đúng khuôn mẫu đó:
```typescript
import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import * as DichVuApi from '../DichVuApi';
import { ModalThongTinKyThuat } from './ModalThongTinKyThuat';

describe('ModalThongTinKyThuat', () => {
  it('hien badge Da ma hoa va Hop le khi tin da ma hoa co chu ky dung', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinKyThuat').mockResolvedValue({
      apDungDuoc: true, daMaHoa: true, thuatToanMaHoa: 'AES-256-GCM',
      kichThuocGocByte: 18, kichThuocMaHoaByte: 34, tyLePhinh: 1.89,
      ciphertextRutGon: 'd3a163f3...bfcc0395', nonceRutGon: '6d0c7f89...c172a', authTagRutGon: '33d5467c...d0152cb',
      thoiGianMaHoaMs: 0.09, thoiGianGiaiMaMs: 0.34,
      coChuKy: true, daXacThucChuKy: true, thuatToanChuKy: 'RSA-PSS / SHA-256',
    });

    render(<ModalThongTinKyThuat token="tok" tinNhanId="m1" onDong={() => {}} />);

    expect(await screen.findByText('Đã mã hóa')).toBeInTheDocument();
    expect(screen.getByText('✓ Hợp lệ')).toBeInTheDocument();
    expect(screen.getByText('AES-256-GCM')).toBeInTheDocument();
  });

  it('hien Khong ap dung khi tin da thu hoi', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinKyThuat').mockResolvedValue({
      apDungDuoc: false, daMaHoa: false, thuatToanMaHoa: null,
      kichThuocGocByte: null, kichThuocMaHoaByte: null, tyLePhinh: null,
      ciphertextRutGon: null, nonceRutGon: null, authTagRutGon: null,
      thoiGianMaHoaMs: null, thoiGianGiaiMaMs: null,
      coChuKy: false, daXacThucChuKy: null, thuatToanChuKy: null,
    });

    render(<ModalThongTinKyThuat token="tok" tinNhanId="m2" onDong={() => {}} />);

    expect(await screen.findByText('Không áp dụng cho loại tin nhắn này.')).toBeInTheDocument();
  });

  it('hien Khong co chu ky khi tin cu chua tung ky', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinKyThuat').mockResolvedValue({
      apDungDuoc: true, daMaHoa: false, thuatToanMaHoa: null,
      kichThuocGocByte: 9, kichThuocMaHoaByte: null, tyLePhinh: null,
      ciphertextRutGon: null, nonceRutGon: null, authTagRutGon: null,
      thoiGianMaHoaMs: null, thoiGianGiaiMaMs: null,
      coChuKy: false, daXacThucChuKy: null, thuatToanChuKy: null,
    });

    render(<ModalThongTinKyThuat token="tok" tinNhanId="m3" onDong={() => {}} />);

    expect(await screen.findByText('Không có chữ ký')).toBeInTheDocument();
    expect(await screen.findByText('Không mã hóa')).toBeInTheDocument();
  });
});
```

- [ ] **Step 5: Chạy test modal, xác nhận PASS**

Run: `cd frontend && npx vitest run src/ThanhPhan/ModalThongTinKyThuat.test.tsx`
Expected: `Test Files 1 passed`, tất cả PASS.

- [ ] **Step 6: Thêm mục menu "Thông tin kỹ thuật" vào `KhungTinNhan.tsx`**

Đọc lại `KhungTinNhan.tsx` trước khi sửa (đã đổi nhiều lần trong các task UI trước) để lấy đúng vị trí `PropsKhungTinNhan` và khối `.khung-tin-nhan__menu`. Thêm prop mới vào interface `PropsKhungTinNhan` (cạnh `onChuyenTiep`):
```typescript
  onXemThongTinKyThuat: (id: string) => void;
```
Thêm vào phần destructure tham số hàm `KhungTinNhan` (cạnh `onChuyenTiep`):
```typescript
  onThuHoi, onGhim, onBoGhim, onAn, onChuyenTiep, onXemThongTinKyThuat, danhSachTinNhanGhim, onMoKhoMedia, onTimKiem, onNhayToiTinNhan,
```
Trong khối `.khung-tin-nhan__menu`, thêm mục mới NGAY SAU nút "Lưu về thiết bị" (áp dụng cho MỌI loại tin, kể cả Text — không đặt trong điều kiện `tn.loaiTinNhan !== 'Text'`) và TRƯỚC nút "Ghim":
```tsx
                          <button onClick={() => { onXemThongTinKyThuat(tn.id); setMenuMoChoTinNhanId(null); }}>Thông tin kỹ thuật</button>
```

- [ ] **Step 7: Cập nhật `KhungTinNhan.test.tsx` — thêm `onXemThongTinKyThuat: () => {}` vào `PROPS_MAC_DINH`**

`frontend/src/ThanhPhan/KhungTinNhan.test.tsx` — thêm vào `PROPS_MAC_DINH` (cạnh `onChuyenTiep: () => {},` đã có từ trước):
```typescript
  onXemThongTinKyThuat: () => {},
```

- [ ] **Step 8: Wire vào `TrangChat.tsx`**

Đọc lại `TrangChat.tsx` trước khi sửa (đã đổi ở các task trước — có sẵn state `tinChuyenTiep`/`dangChuyenTiep`/`loiChuyenTiep` và modal `ModalChuyenTiep` làm khuôn mẫu). Thêm import:
```typescript
import { ModalThongTinKyThuat } from '../ThanhPhan/ModalThongTinKyThuat';
```
Thêm state (cạnh `tinChuyenTiep`):
```typescript
  const [idXemThongTinKyThuat, setIdXemThongTinKyThuat] = useState<string | null>(null);
```
Truyền prop vào `<KhungTinNhan>` (cạnh `onChuyenTiep={moChuyenTiep}`):
```tsx
            onXemThongTinKyThuat={setIdXemThongTinKyThuat}
```
Render modal (cạnh khối render `{tinChuyenTiep && (<ModalChuyenTiep .../>)}`):
```tsx
      {idXemThongTinKyThuat && (
        <ModalThongTinKyThuat
          token={token ?? ''}
          tinNhanId={idXemThongTinKyThuat}
          onDong={() => setIdXemThongTinKyThuat(null)}
        />
      )}
```

- [ ] **Step 9: Wire vào `TrangNhom.tsx` — ĐÚNG CÙNG CÁCH như Step 8**, đọc lại file trước, thêm import + state `idXemThongTinKyThuat` + prop `onXemThongTinKyThuat={setIdXemThongTinKyThuat}` + render `<ModalThongTinKyThuat>` theo đúng cùng khuôn mẫu (dùng `token` đã destructure sẵn từ `useXacThuc()` trong file này).

- [ ] **Step 10: Type-check + chạy toàn bộ test frontend**

Run: `cd frontend && npx tsc -b --noEmit && npx vitest run`
Expected: type-check sạch (0 lỗi); tất cả test PASS.

- [ ] **Step 11: Commit**

```bash
git add frontend/src/KieuDuLieu.ts frontend/src/DichVuApi.ts frontend/src/ThanhPhan/ModalThongTinKyThuat.tsx frontend/src/ThanhPhan/ModalThongTinKyThuat.css frontend/src/ThanhPhan/ModalThongTinKyThuat.test.tsx frontend/src/ThanhPhan/KhungTinNhan.tsx frontend/src/ThanhPhan/KhungTinNhan.test.tsx frontend/src/Trang/TrangChat.tsx frontend/src/Trang/TrangNhom.tsx
git commit -m "feat(frontend): modal Thong tin ky thuat - mo tu menu ... tren moi tin nhan"
```

---

## Task 8: Kiểm tra cuối — build + test toàn bộ, push

**Files:** (không tạo/sửa file mới — chỉ xác minh)

- [ ] **Step 1: Build + test backend toàn bộ**

Run: `cd backend && dotnet build && dotnet test`
Expected: build sạch, tất cả test PASS (đủ số test cũ + mới của Task 1, 2, 3, 5, 6).

- [ ] **Step 2: Type-check + test frontend toàn bộ**

Run: `cd frontend && npx tsc -b --noEmit && npx vitest run`
Expected: type-check sạch, tất cả test PASS (216 cũ + mới của Task 4, 7).

- [ ] **Step 3: Push lên `origin/main`**

```bash
git push
```

- [ ] **Step 4: Báo cáo tổng hợp cho người dùng** (bằng tiếng Việt, theo quy ước dự án) — tóm tắt: 2 tính năng đã xong (chữ ký số + Thông tin kỹ thuật), số test mới/tổng, các giới hạn đã biết cần nêu trong báo cáo đồ án (spec §6: dùng chung khóa RSA cho cả mã hóa lẫn ký, không phải non-repudiation thật vì Private Key ở server), và cách xem thử trên web (bấm "..." trên 1 tin nhắn → "Thông tin kỹ thuật"; icon ✓/cảnh báo nhỏ cạnh giờ gửi khi bấm mở tin nhắn).
