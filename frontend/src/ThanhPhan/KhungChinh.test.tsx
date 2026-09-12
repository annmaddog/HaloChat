import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect } from 'vitest';
import { KhungChinh } from './KhungChinh';

describe('KhungChinh', () => {
  it('hiển thị đủ 4 mục điều hướng và nội dung con', () => {
    render(
      <MemoryRouter initialEntries={['/nguoi-dung']}>
        <KhungChinh>
          <p>Nội dung test</p>
        </KhungChinh>
      </MemoryRouter>,
    );

    expect(screen.getByRole('link', { name: /Tin nhắn/ })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /Bạn bè/ })).toBeInTheDocument();
    expect(screen.getByText('Nhóm')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /Cài đặt/ })).toBeInTheDocument();
    expect(screen.getByText('Nội dung test')).toBeInTheDocument();
  });

  it('mục Tin nhắn được đánh dấu đang chọn khi ở route /nguoi-dung', () => {
    render(
      <MemoryRouter initialEntries={['/nguoi-dung']}>
        <KhungChinh>
          <p>Nội dung</p>
        </KhungChinh>
      </MemoryRouter>,
    );

    expect(screen.getByRole('link', { name: /Tin nhắn/ })).toHaveClass('khung-chinh__muc--dang-chon');
  });
});
