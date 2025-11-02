import React, { useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../services/api'

export default function Login() {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)

  async function handleLogin(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    try {
      const res = await api.post('/api/auth/login', { username, password })
      localStorage.setItem('token', res.data.token)
      window.location.href = '/lobby'
    } catch (err: any) {
      setError(err?.response?.data || 'Login failed')
    }
  }

  return (
    <form onSubmit={handleLogin} className="max-w-sm mx-auto bg-white p-4 shadow rounded space-y-3">
      <h1 className="text-lg font-semibold">Login</h1>
      {error && <div className="text-red-600 text-sm">{error}</div>}
      <input className="w-full border p-2 rounded" placeholder="Username" value={username} onChange={e => setUsername(e.target.value)} required />
      <input className="w-full border p-2 rounded" placeholder="Password" type="password" value={password} onChange={e => setPassword(e.target.value)} required />
      <button className="w-full bg-slate-800 text-white py-2 rounded" type="submit">Sign in</button>
      <div className="text-center text-sm">
        <Link to="/register" className="text-blue-600 hover:underline">Don't have an account? Register</Link>
      </div>
    </form>
  )
}


