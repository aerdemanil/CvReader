import type { ReactNode } from 'react'
import { Icon } from './Icon'

interface AlertProps {
  tone: 'error' | 'info'
  children: ReactNode
}

export function Alert({ tone, children }: AlertProps) {
  return (
    <div className={`alert alert-${tone}`} role={tone === 'error' ? 'alert' : 'status'}>
      <Icon name={tone === 'error' ? 'alert' : 'info'} />
      <div>{children}</div>
    </div>
  )
}
