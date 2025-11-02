
import { api } from '../services/api'
import { useNavigate } from 'react-router-dom'
import React, { useState, FormEvent } from 'react'

export default function DifficultySelect() {
  const [difficulty, setDifficulty] = useState(1)
  const [timeControl, setTimeControl] = useState(60)
  const navigate = useNavigate()

  async function handleCreate() {
    const res = await api.post('/api/games', {
      mode: 'PvE',
      p1UserId: null,
      p2UserId: null,
      pveDifficulty: difficulty,
      timeControlSeconds: timeControl
    })
    navigate(`/game/${res.data.id}`)
  }

  return (
    <div className="max-w-sm mx-auto bg-white p-4 rounded shadow space-y-3">
      <div className="text-lg font-semibold">PvE Options</div>
      <label className="block">Difficulty</label>
      <select className="w-full border p-2 rounded" value={difficulty} onChange={e => setDifficulty(Number(e.target.value))}>
        <option value={1}>Easy</option>
        <option value={2}>Normal</option>
        <option value={3}>Hard</option>
      </select>
      <label className="block">Time control (seconds)</label>
      <input className="w-full border p-2 rounded" type="number" min={10} value={timeControl} onChange={e => setTimeControl(Number(e.target.value))} />
      <button className="w-full bg-emerald-600 text-white py-2 rounded" onClick={handleCreate}>Create PvE Game</button>
    </div>
  )
}


