import { render, screen } from '@testing-library/react'
import { expect, test } from 'vitest'
import NotFound from './not-found'

test('renders not found page', () => {
  render(<NotFound />)
  expect(screen.getByText('404 - Not Found')).toBeInTheDocument()
  expect(screen.getByText('Could not find requested resource')).toBeInTheDocument()
})
