import { render, screen, fireEvent } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { CongTac } from './CongTac';

describe('CongTac', () => {
  it('goi onDoi voi gia tri nguoc lai khi bam', () => {
    const onDoi = vi.fn();
    render(<CongTac batTat={false} onDoi={onDoi} nhan="Vi du" />);
    fireEvent.click(screen.getByRole('switch'));
    expect(onDoi).toHaveBeenCalledWith(true);
  });

  it('phan anh dung trang thai bat/tat qua aria-checked', () => {
    render(<CongTac batTat onDoi={() => {}} nhan="Vi du" />);
    expect(screen.getByRole('switch')).toHaveAttribute('aria-checked', 'true');
  });

  it('khong goi onDoi khi disabled', () => {
    const onDoi = vi.fn();
    render(<CongTac batTat={false} onDoi={onDoi} disabled nhan="Vi du" />);
    fireEvent.click(screen.getByRole('switch'));
    expect(onDoi).not.toHaveBeenCalled();
  });
});
