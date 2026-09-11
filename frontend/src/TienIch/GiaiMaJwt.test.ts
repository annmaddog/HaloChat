import { describe, it, expect } from 'vitest';
import { GiaiMaJwt } from './GiaiMaJwt';

function maHoaBase64Utf8(vanBan: string): string {
  const byteMang = new TextEncoder().encode(vanBan);
  let nhiPhan = '';
  byteMang.forEach((byte) => {
    nhiPhan += String.fromCharCode(byte);
  });
  return btoa(nhiPhan);
}

function taoJwtGiaLap(payload: object): string {
  const header = maHoaBase64Utf8(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const than = maHoaBase64Utf8(JSON.stringify(payload));
  return `${header}.${than}.chu-ky-gia`;
}

describe('GiaiMaJwt', () => {
  it('giải mã đúng payload từ token hợp lệ', () => {
    const token = taoJwtGiaLap({ sub: '123', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com' });

    const ketQua = GiaiMaJwt(token);

    expect(ketQua).toEqual({ sub: '123', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com' });
  });

  it('giải mã đúng ký tự có dấu tiếng Việt', () => {
    const token = taoJwtGiaLap({ sub: '1', tenTaiKhoan: 'Nguyễn Ăn', email: 'a@gmail.com' });

    expect(GiaiMaJwt(token)?.tenTaiKhoan).toBe('Nguyễn Ăn');
  });

  it('trả về null với token không hợp lệ', () => {
    expect(GiaiMaJwt('khong-phai-jwt')).toBeNull();
  });
});
