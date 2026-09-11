import { BrowserRouter } from 'react-router-dom';
import { NhaCungCapXacThuc } from './NguCanh/NguCanhXacThuc';
import { DinhTuyen } from './DinhTuyen';

function App() {
  return (
    <BrowserRouter>
      <NhaCungCapXacThuc>
        <DinhTuyen />
      </NhaCungCapXacThuc>
    </BrowserRouter>
  );
}

export default App;
