import { beforeEach, describe, expect, it } from 'vitest';
import { apDungGiaoDien, layGiaoDienDaLuu } from './GiaoDien';

describe('GiaoDien', () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
  });

  it('mac dinh la sang khi chua luu gi', () => {
    expect(layGiaoDienDaLuu()).toBe('sang');
  });

  it('apDungGiaoDien dat data-theme tren the html', () => {
    apDungGiaoDien('toi');
    expect(document.documentElement.dataset.theme).toBe('toi');
  });

  it('apDungGiaoDien luu lai lua chon, doc lai dung', () => {
    apDungGiaoDien('toi');
    expect(layGiaoDienDaLuu()).toBe('toi');
  });
});
