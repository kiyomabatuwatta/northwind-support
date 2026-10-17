
import { useCallback, useEffect, useMemo, useState } from 'react';
import { ApiError } from '../../../api/client';
import { ticketsApi } from '../../../api/tickets';
import type { PagedResult, TicketFilters, TicketListItem } from '../../../types/api';

const DEBOUNCE_MS = 300;

export const emptyFilters: TicketFilters = {
  search: '',
  status: undefined,
  priority: undefined,
  categoryId: undefined,
  customerId: undefined,
  assignedAgentId: undefined,
  unassignedOnly: false,
  page: 1,
  pageSize: 20,
  sortBy: 'createdAtUtc',
  sortDirection: 'desc',
};

interface Settled {
  /** The request this outcome belongs to. */
  request: object;
  error?: string;
}

/** Returns `value` only after it has stopped changing for `delay` ms. */
function useDebouncedValue<T>(value: T, delay: number): T {
  const [debounced, setDebounced] = useState(value);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(value), delay);

    return () => window.clearTimeout(handle);
  }, [value, delay]);

  return debounced;
}

/** Owns the ticket list's filters and the data they produce. */
export function useTicketList() {
  const [filters, setFilters] = useState<TicketFilters>(emptyFilters);
  const [data, setData] = useState<PagedResult<TicketListItem>>();
  const [settled, setSettled] = useState<Settled>();
  const [reloadCount, setReloadCount] = useState(0);

  // Only the free-text search is debounced; every other filter applies immediately.
  const search = useDebouncedValue(filters.search, DEBOUNCE_MS);

  const {
    status,
    priority,
    categoryId,
    customerId,
    assignedAgentId,
    unassignedOnly,
    page,
    pageSize,
    sortBy,
    sortDirection,
  } = filters;

  // The request that is actually sent. It lists every filter field, so any change
  // (including filters added later) produces a new query and therefore a new request.
  const query = useMemo<TicketFilters>(
    () => ({
      search,
      status,
      priority,
      categoryId,
      customerId,
      assignedAgentId,
      unassignedOnly,
      page,
      pageSize,
      sortBy,
      sortDirection,
    }),
    [
      search,
      status,
      priority,
      categoryId,
      customerId,
      assignedAgentId,
      unassignedOnly,
      page,
      pageSize,
      sortBy,
      sortDirection,
    ],
  );

  // One object per request: loading is "the latest request has not settled yet".
  const request = useMemo(() => ({ query, reloadCount }), [query, reloadCount]);

  useEffect(() => {
    let ignore = false;

    ticketsApi
      .getTickets(request.query)
      .then((result) => {
        if (!ignore) {
          setData(result);
          setSettled({ request });
        }
      })
      .catch((error: unknown) => {
        if (!ignore) {
          const message = error instanceof ApiError ? error.message : 'Unable to load tickets.';
          setSettled({ request, error: message });
        }
      });

    // Ignore a response that arrives after the query has changed (stale result).
    return () => {
      ignore = true;
    };
  }, [request]);

  const isLoading = settled?.request !== request;
  const error = isLoading ? undefined : settled.error;

  const updateFilters = (patch: Partial<TicketFilters>) =>
    setFilters((current) => ({ ...current, ...patch, page: patch.page ?? 1 }));

  const resetFilters = () => setFilters(emptyFilters);

  const reload = useCallback(() => setReloadCount((count) => count + 1), []);

  return { filters, updateFilters, resetFilters, reload, isLoading, data, error };
}
