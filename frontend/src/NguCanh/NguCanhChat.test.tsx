import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { NhaCungCapChat, useChat } from './NguCanhChat';
import { NhaCungCapXacThuc } from './NguCanhXacThuc';

const ketNoiGiaLap = {
  start: vi.fn().mockResolvedValue(undefined),
  stop: vi.fn().mockResolvedValue(undefined),
  on: vi.fn(),
  off: vi.fn(),
  invoke: vi.fn(),
  onreconnected: vi.fn(),
  onreconnecting: vi.fn(),
  onclose: vi.fn(),
};

vi.mock('@microsoft/signalr', () => ({
  // Dùng `function` (không phải arrow function) vì vitest gọi implementation
  // này qua `Reflect.construct` khi `new HubConnectionBuilder()` được thực thi
  // — arrow function không thể construct được nên sẽ ném "is not a constructor".
  HubConnectionBuilder: vi.fn().mockImplementation(function () {
    return {
      withUrl: vi.fn().mockReturnThis(),
      withAutomaticReconnect: vi.fn().mockReturnThis(),
      configureLogging: vi.fn().mockReturnThis(),
      build: vi.fn().mockReturnValue(ketNoiGiaLap),
    };
  }),
  LogLevel: { Warning: 2 },
}));

function TrangThuNghiem() {
  const { dangKetNoi } = useChat();
  return <p>{dangKetNoi ? 'da-ket-noi' : 'chua-ket-noi'}</p>;
}

describe('NhaCungCapChat', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  it('kết nối ChatHub khi đã có token đăng nhập', async () => {
    localStorage.setItem('haloChatToken', 'token-gia-lap');

    render(
      <NhaCungCapXacThuc>
        <NhaCungCapChat>
          <TrangThuNghiem />
        </NhaCungCapChat>
      </NhaCungCapXacThuc>,
    );

    await waitFor(() => expect(ketNoiGiaLap.start).toHaveBeenCalledTimes(1));
    expect(await screen.findByText('da-ket-noi')).toBeInTheDocument();
  });

  it('không tạo kết nối khi chưa đăng nhập', () => {
    render(
      <NhaCungCapXacThuc>
        <NhaCungCapChat>
          <TrangThuNghiem />
        </NhaCungCapChat>
      </NhaCungCapXacThuc>,
    );

    expect(ketNoiGiaLap.start).not.toHaveBeenCalled();
    expect(screen.getByText('chua-ket-noi')).toBeInTheDocument();
  });
});
