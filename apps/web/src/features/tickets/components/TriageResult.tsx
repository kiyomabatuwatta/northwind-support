import { formatDateTime } from '../../../lib/format';
import type { TicketDetail } from '../../../types/api';
import { SlaCell } from './SlaCell';
import { PriorityBadge } from './TicketBadges';

/** What the system decided for a ticket that was just raised, and why. */
export function TriageResult({ ticket }: { ticket: TicketDetail }) {
  const triage = ticket.triage;

  return (
    <dl className="summary" aria-label="Triage result">
      <div>
        <dt>Priority</dt>
        <dd>
          <PriorityBadge priority={ticket.priority} />
          {triage && <span className="summary__note">{triage.priorityReason}</span>}
        </dd>
      </div>

      <div>
        <dt>Due</dt>
        <dd>
          <SlaCell dueAtUtc={ticket.dueAtUtc} slaStatus={ticket.slaStatus} />
          {triage && <span className="summary__note">{triage.slaReason}</span>}
        </dd>
      </div>

      <div>
        <dt>Assigned agent</dt>
        <dd>
          {ticket.assignedAgent?.fullName ?? 'Unassigned'}
          {triage && <span className="summary__note">{triage.assignmentReason}</span>}
        </dd>
      </div>

      <div>
        <dt>Created</dt>
        <dd>{formatDateTime(ticket.createdAtUtc)}</dd>
      </div>
    </dl>
  );
}
