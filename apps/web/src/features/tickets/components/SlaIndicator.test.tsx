import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { SlaStatus } from '../../../types/api';
import { SlaIndicator } from './SlaIndicator';

const states: { status: SlaStatus; text: string }[] = [
  { status: 'WithinSla', text: 'Within SLA' },
  { status: 'AtRisk', text: 'At risk' },
  { status: 'Breached', text: 'Breached' },
  { status: 'Met', text: 'Met' },
  { status: 'NotApplicable', text: 'No SLA' },
];

describe('SlaIndicator', () => {
  it.each(states)('shows "$text" for $status', ({ status, text }) => {
    render(<SlaIndicator status={status} />);

    expect(screen.getByText(text)).toBeInTheDocument();
  });

  it('gives every state its own text and symbol, so colour is not the only signal', () => {
    const rendered = states.map(({ status }) => {
      const { container, unmount } = render(<SlaIndicator status={status} />);
      const content = container.textContent;
      unmount();

      return content;
    });

    expect(new Set(rendered).size).toBe(states.length);
  });
});
