import { formatDateTime } from '../../../lib/format';
import type { Escalation } from '../../../types/api';

interface EscalationHistoryProps {
  escalations: Escalation[];
  isLoading: boolean;
  error?: string;
}

/** What changed on each escalation, newest first. */
export function EscalationHistory({ escalations, isLoading, error }: EscalationHistoryProps) {
  return (
    <div className="card">
      <h2>Escalation history</h2>

      {isLoading && escalations.length === 0 && <p>Loading history...</p>}

      {error && (
        <p className="field__error" role="alert">
          {error}
        </p>
      )}

      {!isLoading && !error && escalations.length === 0 && <p>This ticket has not been escalated.</p>}

      {escalations.length > 0 && (
        <ol className="history">
          {escalations.map((item) => (
            <li key={item.id}>
              <p>
                <strong>
                  {item.fromPriority} → {item.toPriority}
                </strong>{' '}
                by {item.escalatedBy} on {formatDateTime(item.escalatedAtUtc)}
              </p>
              <p className="summary__note">Reason: {item.reason}</p>
              <p className="summary__note">
                Agent: {item.fromAgent?.fullName ?? 'Unassigned'} → {item.toAgent?.fullName ?? 'Unassigned'}
              </p>
              <p className="summary__note">
                Due: {formatDateTime(item.fromDueAtUtc)} → {formatDateTime(item.toDueAtUtc)}
              </p>
            </li>
          ))}
        </ol>
      )}
    </div>
  );
}
