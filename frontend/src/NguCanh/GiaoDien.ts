const KHOA_LUU = 'halochat-giao-dien';

export type GiaoDien = 'sang' | 'toi';

export function layGiaoDienDaLuu(): GiaoDien {
  try {
    return localStorage.getItem(KHOA_LUU) === 'toi' ? 'toi' : 'sang';
  } catch {
    return 'sang';
  }
}

export function apDungGiaoDien(giaoDien: GiaoDien): void {
  document.documentElement.dataset.theme = giaoDien;
  try {
    localStorage.setItem(KHOA_LUU, giaoDien);
  } catch {
    // localStorage có thể bị chặn (chế độ ẩn danh) — chấp nhận mất khả năng nhớ lựa chọn.
  }
}
