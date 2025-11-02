import { Link, Routes, Route, useNavigate } from 'react-router-dom'
import Login from './components/Login'
import Register from './components/Register'
import Lobby from './components/Lobby'
import GameBoard from './components/GameBoard'
import History from './components/History'
import DifficultySelect from './components/DifficultySelect'
import React from 'react'

export default function App() {
  const navigate = useNavigate();
  const token = localStorage.getItem('token');

  return (
    <div className="min-h-full">
      <nav className="bg-white shadow p-3 flex gap-4">
        <Link to="/">Home</Link>
        <Link to="/lobby">Lobby</Link>
        <Link to="/history">History</Link>
        <Link to="/pve">PvE</Link>
        <div className="ml-auto flex gap-2">
          {token ? (
            <button className="px-3 py-1 bg-slate-800 text-white rounded" onClick={() => { localStorage.removeItem('token'); navigate('/'); }}>Logout</button>
          ) : (
            <>
              <Link className="px-3 py-1 bg-slate-800 text-white rounded" to="/register">Register</Link>
              <Link className="px-3 py-1 bg-slate-800 text-white rounded" to="/login">Login</Link>
            </>
          )}
        </div>
      </nav>
      <main className="p-4">
        <Routes>
          <Route path="/" element={<div className="text-xl">Caro Online</div>} />
          <Route path="/login" element={<Login />} />
          <Route path="/register" element={<Register />} />
          <Route path="/lobby" element={<Lobby />} />
          <Route path="/game/:id" element={<GameBoard />} />
          <Route path="/history" element={<History />} />
          <Route path="/pve" element={<DifficultySelect />} />
        </Routes>
      </main>
    </div>
  )
}


