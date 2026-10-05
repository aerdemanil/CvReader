import { useState, type ClipboardEvent, type KeyboardEvent } from 'react'
import { Icon } from '../../components/Icon'

export const MAX_KEYWORDS = 30
export const MAX_KEYWORD_LENGTH = 100

interface KeywordInputProps {
  value: string[]
  onChange: (keywords: string[]) => void
}

// Enter veya virgül ile anahtar kelime ekler; Backspace boş alanda son kelimeyi siler.
export function KeywordInput({ value, onChange }: KeywordInputProps) {
  const [draft, setDraft] = useState('')

  function add(raw: string) {
    const known = new Set(value.map((k) => k.toLocaleLowerCase('tr-TR')))
    const next = [...value]
    for (const part of raw.split(',')) {
      const keyword = part.trim().slice(0, MAX_KEYWORD_LENGTH)
      const key = keyword.toLocaleLowerCase('tr-TR')
      if (keyword && !known.has(key) && next.length < MAX_KEYWORDS) {
        known.add(key)
        next.push(keyword)
      }
    }
    onChange(next)
    setDraft('')
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === 'Enter' || event.key === ',') {
      event.preventDefault()
      add(draft)
    } else if (event.key === 'Backspace' && draft === '' && value.length > 0) {
      onChange(value.slice(0, -1))
    }
  }

  function handlePaste(event: ClipboardEvent<HTMLInputElement>) {
    const text = event.clipboardData.getData('text')
    if (text.includes(',')) {
      event.preventDefault()
      add(text)
    }
  }

  return (
    <div className="keyword-input">
      {value.map((keyword) => (
        <span key={keyword} className="chip">
          {keyword}
          <button
            type="button"
            aria-label={`${keyword} kelimesini kaldır`}
            onClick={() => onChange(value.filter((k) => k !== keyword))}
          >
            <Icon name="close" size={12} />
          </button>
        </span>
      ))}
      <input
        value={draft}
        onChange={(e) => setDraft(e.target.value)}
        onKeyDown={handleKeyDown}
        onPaste={handlePaste}
        onBlur={() => draft.trim() && add(draft)}
        maxLength={MAX_KEYWORD_LENGTH}
        placeholder={value.length === 0 ? 'ör. Java, Spring Boot, PostgreSQL' : ''}
        disabled={value.length >= MAX_KEYWORDS}
        aria-label="Anahtar kelime ekle"
      />
    </div>
  )
}
