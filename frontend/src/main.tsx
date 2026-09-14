import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { apDungGiaoDien, layGiaoDienDaLuu } from './NguCanh/GiaoDien'

apDungGiaoDien(layGiaoDienDaLuu());

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
