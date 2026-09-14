import './Huy.css';

export function Huy({ soLuong }: { soLuong: number }) {
  if (soLuong <= 0) return null;
  return <span className="huy">{soLuong > 99 ? '99+' : soLuong}</span>;
}
