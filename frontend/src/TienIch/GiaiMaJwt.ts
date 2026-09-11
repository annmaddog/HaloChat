export interface PayloadJwt {
  sub: string;
  tenTaiKhoan: string;
  email: string;
}

/** Giải mã phần payload của JWT — chỉ đọc, không xác minh chữ ký (server đã xác minh). */
export function GiaiMaJwt(token: string): PayloadJwt | null {
  try {
    const phanThan = token.split('.')[1];
    if (!phanThan) return null;

    const base64 = phanThan.replace(/-/g, '+').replace(/_/g, '/');
    const vanBan = decodeURIComponent(
      atob(base64)
        .split('')
        .map((ky) => '%' + ky.charCodeAt(0).toString(16).padStart(2, '0'))
        .join(''),
    );
    return JSON.parse(vanBan) as PayloadJwt;
  } catch {
    return null;
  }
}
