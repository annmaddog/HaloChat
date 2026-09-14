import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { Avatar } from './Avatar';

describe('Avatar', () => {
  it('hien chu cai dau viet hoa cua ten', () => {
    render(<Avatar id="abc" ten="nguyen" />);
    expect(screen.getByText('N')).toBeInTheDocument();
  });

  it('hien dau hoi neu ten rong', () => {
    render(<Avatar id="abc" ten="" />);
    expect(screen.getByText('?')).toBeInTheDocument();
  });

  it('cung mot id luon ra cung mau nen', () => {
    const { container: c1 } = render(<Avatar id="user-1" ten="A" />);
    const { container: c2 } = render(<Avatar id="user-1" ten="B" />);
    const mau1 = (c1.querySelector('.avatar') as HTMLElement).style.background;
    const mau2 = (c2.querySelector('.avatar') as HTMLElement).style.background;
    expect(mau1).toBe(mau2);
  });

  it('co duongDanAnh: render the img thay vi chu cai', () => {
    const { container } = render(<Avatar id="n1" ten="Nhóm CNTT" duongDanAnh="/uploads/anh.png" />);
    const anh = container.querySelector('img.avatar');
    expect(anh).toBeInTheDocument();
    expect(anh).toHaveAttribute('alt', 'Nhóm CNTT');
  });

  it('duongDanAnh la null: van hien chu cai nhu cu', () => {
    render(<Avatar id="n1" ten="Nhóm CNTT" duongDanAnh={null} />);
    expect(screen.getByText('N')).toBeInTheDocument();
  });
});
