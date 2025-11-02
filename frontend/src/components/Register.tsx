import React, { useState, FormEvent } from 'react'
import { api } from '../services/api'
import { Link } from 'react-router-dom'

export default function Register() {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [error, setError] = useState<string | null>(null)

  async function handleRegister(e: FormEvent) {
    e.preventDefault()
    setError(null)
    try {
      const res = await api.post('/api/auth/register', { 
        username, 
        password, 
        displayName: displayName || undefined 
      })
      localStorage.setItem('token', res.data.token)
      window.location.href = '/lobby'
    } catch (err: any) {
      setError(err?.response?.data?.message || err?.response?.data || 'Registration failed')
    }
  }

  return (
    <form onSubmit={handleRegister} className="max-w-sm mx-auto bg-white p-4 shadow rounded space-y-3">
      <h1 className="text-lg font-semibold">Register</h1>
      {error && <div className="text-red-600 text-sm">{error}</div>}
      <input 
        className="w-full border p-2 rounded" 
        placeholder="Username" 
        value={username} 
        onChange={e => setUsername(e.target.value)} 
        required
      />
      <input 
        className="w-full border p-2 rounded" 
        placeholder="Password" 
        type="password" 
        value={password} 
        onChange={e => setPassword(e.target.value)} 
        required
      />
      <input 
        className="w-full border p-2 rounded" 
        placeholder="Display Name (optional)" 
        value={displayName} 
        onChange={e => setDisplayName(e.target.value)} 
      />
      <button className="w-full bg-slate-800 text-white py-2 rounded" type="submit">Sign up</button>
      <div className="text-center text-sm">
        <Link to="/login" className="text-blue-600 hover:underline">Already have an account? Login</Link>
      </div>
    </form>
  )
}

