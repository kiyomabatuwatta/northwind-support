import type { TicketDetail } from '../../types/api';

/** These limits match the API's validation, so invalid input never has to reach it. */
export const REASON_MIN = 5;
export const REASON_MAX = 500;

export interface EscalationFormErrors {
  reason?: string;
}

export function validateEscalation(reason: string): EscalationFormErrors {
  const errors: EscalationFormErrors = {};
  const trimmedReason = reason.trim();

  if (trimmedReason.length < REASON_MIN || trimmedReason.length > REASON_MAX) {
    errors.reason = `Give a reason between ${REASON_MIN} and ${REASON_MAX} characters.`;
  }

  return errors;
}

/** Why a ticket cannot be escalated, or undefined when it can. */
export function escalationBlockedReason(ticket: TicketDetail): string | undefined {
  if (ticket.status === 'Resolved' || ticket.status === 'Closed') {
    return `This ticket is ${ticket.status.toLowerCase()} and cannot be escalated.`;
  }

  if (ticket.priority === 'Critical') {
    return 'This ticket is already Critical and cannot be escalated further.';
  }

  return undefined;
}
