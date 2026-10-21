import { Badge, type BadgeTone } from '../../../components/Badge';
import type { SlaStatus } from '../../../types/api';

interface SlaDisplay {
  text: string;
  /** A symbol, so the state is not told by colour alone. */
  symbol: string;
  tone: BadgeTone;
}

const displays: Record<SlaStatus, SlaDisplay> = {
  WithinSla: { text: 'Within SLA', symbol: '●', tone: 'info' },
  AtRisk: { text: 'At risk', symbol: '▲', tone: 'warning' },
  Breached: { text: 'Breached', symbol: '✕', tone: 'danger' },
  Met: { text: 'Met', symbol: '✓', tone: 'success' },
  NotApplicable: { text: 'No SLA', symbol: '–', tone: 'neutral' },
};

/**
 * The one place an SLA state is drawn, used by the ticket list and the ticket detail page.
 * Each state has its own words and its own symbol as well as its own colour.
 */
export function SlaIndicator({ status }: { status: SlaStatus }) {
  const display = displays[status];

  return (
    <Badge tone={display.tone}>
      <span aria-hidden="true">{display.symbol} </span>
      {display.text}
    </Badge>
  );
}
