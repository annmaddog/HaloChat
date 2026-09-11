import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import type { HubConnection } from '@microsoft/signalr';
import { TaoKetNoiChat } from '../DichVuSignalR';
import { useXacThuc } from './NguCanhXacThuc';

interface TrangThaiChat {
  ketNoi: HubConnection | null;
  dangKetNoi: boolean;
}

// Giá trị mặc định KHÔNG throw (khác NguCanhXacThuc cố ý): nơi nào chưa bọc
// NhaCungCapChat (vd DinhTuyen.test.tsx hiện có) vẫn chạy được, chỉ là chưa
// có realtime — tránh phải sửa lại các test không liên quan tới chat.
const BoiCanhChat = createContext<TrangThaiChat>({ ketNoi: null, dangKetNoi: false });

export function NhaCungCapChat({ children }: { children: ReactNode }) {
  const { token } = useXacThuc();
  const [ketNoi, setKetNoi] = useState<HubConnection | null>(null);
  const [dangKetNoi, setDangKetNoi] = useState(false);

  useEffect(() => {
    if (!token) {
      setKetNoi(null);
      setDangKetNoi(false);
      return;
    }

    let daHuy = false;
    const ketNoiMoi = TaoKetNoiChat(token);
    ketNoiMoi.onreconnected(() => { if (!daHuy) setDangKetNoi(true); });
    ketNoiMoi.onreconnecting(() => { if (!daHuy) setDangKetNoi(false); });
    ketNoiMoi.onclose(() => { if (!daHuy) setDangKetNoi(false); });

    ketNoiMoi
      .start()
      .then(() => {
        if (!daHuy) setDangKetNoi(true);
      })
      .catch((loi) => {
        // Không throw ra ngoài: mất kết nối realtime không được phép làm sập
        // trang — người dùng vẫn dùng được REST (lịch sử, gửi ảnh/file).
        console.error('Không kết nối được ChatHub:', loi);
      });

    setKetNoi(ketNoiMoi);

    return () => {
      daHuy = true;
      setDangKetNoi(false);
      void ketNoiMoi.stop();
    };
  }, [token]);

  return <BoiCanhChat.Provider value={{ ketNoi, dangKetNoi }}>{children}</BoiCanhChat.Provider>;
}

export function useChat(): TrangThaiChat {
  return useContext(BoiCanhChat);
}
