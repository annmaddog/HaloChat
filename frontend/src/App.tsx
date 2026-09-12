import { BrowserRouter } from 'react-router-dom';
import { NhaCungCapXacThuc } from './NguCanh/NguCanhXacThuc';
import { NhaCungCapChat } from './NguCanh/NguCanhChat';
import { DinhTuyen } from './DinhTuyen';

function App() {
  return (
    <BrowserRouter>
      <NhaCungCapXacThuc>
        <NhaCungCapChat>
          <DinhTuyen />
        </NhaCungCapChat>
      </NhaCungCapXacThuc>
    </BrowserRouter>
  );
}

export default App;
