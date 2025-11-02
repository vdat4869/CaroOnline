import React, { useEffect, useState } from 'react'
import { api } from '../services/api'

export default function History() {
  const [games, setGames] = useState<any[]>([])
  useEffect(() => {
    (async () => {
      const res = await api.get('/api/games')
      setGames(res.data)
    })()
  }, [])

  return (
    <div>
      <div className="text-lg mb-2">Recent Games</div>
      <table className="w-full bg-white shadow">
        <thead>
          <tr className="text-left">
            <th className="p-2">Id</th>
            <th className="p-2">Mode</th>
            <th className="p-2">Result</th>
            <th className="p-2">Created</th>
          </tr>
        </thead>
        <tbody>
          {games.map(g => (
            <tr key={g.id} className="border-t">
              <td className="p-2">{g.id}</td>
              <td className="p-2">{g.mode}</td>
              <td className="p-2">{g.result || '-'}</td>
              <td className="p-2">{new Date(g.createdAt).toLocaleString()}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}


