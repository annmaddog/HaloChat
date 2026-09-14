import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { Huy } from './Huy';

describe('Huy', () => {
  it('khong render gi khi soLuong = 0', () => {
    const { container } = render(<Huy soLuong={0} />);
    expect(container).toBeEmptyDOMElement();
  });

  it('hien dung so khi <= 99', () => {
    render(<Huy soLuong={7} />);
    expect(screen.getByText('7')).toBeInTheDocument();
  });

  it('hien "99+" khi > 99', () => {
    render(<Huy soLuong={150} />);
    expect(screen.getByText('99+')).toBeInTheDocument();
  });
});
