import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '../../../api/client';
import { ticketsApi } from '../../../api/tickets';
import type { TicketDetail } from '../../../types/api';
import { EscalationPanel } from './EscalationPanel';

vi.mock('../../../api/tickets', () => ({
  ticketsApi: { escalate: vi.fn() },
}));

const escalate = vi.mocked(ticketsApi.escalate);

function ticket(overrides: Partial<TicketDetail> = {}): TicketDetail {
  return {
    id: 12,
    reference: 'TCK-0012',
    title: 'Invoice total does not match the order',
    description: 'The total is wrong.',
    status: 'InProgress',
    priority: 'High',
    customer: { id: 2, name: 'Fabrikam Inc', tier: 'Standard', email: 'a@b.test', phone: null },
    category: { id: 2, name: 'Billing', requiresSpecialist: true },
    assignedAgent: { id: 4, fullName: 'Sara Lindqvist' },
    createdAtUtc: '2026-09-24T08:12:00Z',
    updatedAtUtc: '2026-09-25T10:03:00Z',
    dueAtUtc: '2026-09-24T16:12:00Z',
    resolvedAtUtc: null,
    slaStatus: 'Breached',
    ...overrides,
  };
}

describe('EscalationPanel', () => {
  afterEach(() => vi.clearAllMocks());

  it('blocks a too-short reason and never calls the API', async () => {
    const user = userEvent.setup();
    const onEscalated = vi.fn();
    render(<EscalationPanel ticket={ticket()} onEscalated={onEscalated} />);

    await user.type(screen.getByLabelText('Reason'), 'no');
    await user.click(screen.getByRole('button', { name: 'Escalate ticket' }));

    expect(screen.getByText(/between 5 and 500 characters/)).toBeInTheDocument();
    expect(escalate).not.toHaveBeenCalled();
    expect(onEscalated).not.toHaveBeenCalled();
  });

  it('shows a 409 from the server and leaves the ticket unchanged', async () => {
    const user = userEvent.setup();
    const onEscalated = vi.fn();
    escalate.mockRejectedValue(new ApiError(409, 'Ticket TCK-0012 is already Critical and cannot be escalated.'));
    render(<EscalationPanel ticket={ticket()} onEscalated={onEscalated} />);

    await user.type(screen.getByLabelText('Reason'), 'Customer is waiting');
    await user.click(screen.getByRole('button', { name: 'Escalate ticket' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('already Critical');
    expect(onEscalated).not.toHaveBeenCalled();
  });

  it('sends a trimmed, valid escalation', async () => {
    const user = userEvent.setup();
    escalate.mockResolvedValue({ ticket: ticket({ priority: 'Critical' }), escalation: {} as never });
    const onEscalated = vi.fn();
    render(<EscalationPanel ticket={ticket()} onEscalated={onEscalated} />);

    await user.type(screen.getByLabelText('Reason'), '  Customer is waiting  ');
    await user.click(screen.getByRole('button', { name: 'Escalate ticket' }));

    expect(escalate).toHaveBeenCalledWith(12, { reason: 'Customer is waiting' });
    expect(onEscalated).toHaveBeenCalledTimes(1);
  });

  it('disables the control for a Critical ticket and says why', () => {
    render(<EscalationPanel ticket={ticket({ priority: 'Critical' })} onEscalated={vi.fn()} />);

    expect(screen.getByRole('button', { name: 'Escalate ticket' })).toBeDisabled();
    expect(screen.getByText(/already Critical/)).toBeInTheDocument();
  });
});
