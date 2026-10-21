import { useState, type FormEvent } from 'react';
import { toErrorMessage } from '../../../api/client';
import { ticketsApi } from '../../../api/tickets';
import { Field } from '../../../components/Field';
import type { EscalationResult, TicketDetail } from '../../../types/api';
import {
  REASON_MAX,
  escalationBlockedReason,
  validateEscalation,
  type EscalationFormErrors,
} from '../escalationRules';

interface EscalationPanelProps {
  ticket: TicketDetail;
  /** Called with the updated ticket and the recorded history row after a successful escalation. */
  onEscalated: (result: EscalationResult) => void;
}

/** Escalates a ticket one priority level, with a reason that is recorded in its history. */
export function EscalationPanel({ ticket, onEscalated }: EscalationPanelProps) {
  const [reason, setReason] = useState('');
  const [errors, setErrors] = useState<EscalationFormErrors>({});
  const [serverError, setServerError] = useState<string>();
  const [isPending, setIsPending] = useState(false);

  const blockedReason = escalationBlockedReason(ticket);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setServerError(undefined);

    const found = validateEscalation(reason);
    setErrors(found);

    if (Object.keys(found).length > 0) {
      return;
    }

    setIsPending(true);

    try {
      const result = await ticketsApi.escalate(ticket.id, {
        reason: reason.trim(),
      });

      setReason('');
      onEscalated(result);
    } catch (caught) {
      // A 409 lands here too: the message says whether the ticket is Critical or finished.
      setServerError(toErrorMessage(caught, 'Could not escalate the ticket.'));
    } finally {
      setIsPending(false);
    }
  };

  return (
    <div className="card">
      <h2>Escalate</h2>

      {blockedReason && <p className="summary__note">{blockedReason}</p>}

      <form onSubmit={(event) => void submit(event)} noValidate>
        <Field id="escalation-reason" label="Reason" error={errors.reason}
          hint={`${REASON_MAX} characters at most. The escalation is recorded under your name.`}>
          {(props) => (
            <textarea
              {...props}
              rows={3}
              maxLength={REASON_MAX}
              value={reason}
              disabled={Boolean(blockedReason)}
              onChange={(event) => setReason(event.target.value)}
            />
          )}
        </Field>

        <button
          type="submit"
          className="button button--primary"
          disabled={Boolean(blockedReason) || isPending}
        >
          {isPending ? 'Escalating...' : 'Escalate ticket'}
        </button>

        {serverError && (
          <p className="field__error" role="alert">
            {serverError}
          </p>
        )}
      </form>
    </div>
  );
}
