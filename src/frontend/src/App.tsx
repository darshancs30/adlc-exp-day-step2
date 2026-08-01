import React, { useEffect, useMemo, useState } from 'react';
import { apiClient, type ProblemDetails, type ConversionRequest, type ConversionResponse, type ConversionAuditRecord } from './api';

export function App() {
  const [amount, setAmount] = useState<string>('');
  const [fromCurrency, setFromCurrency] = useState<string>('USD');
  const [toCurrency, setToCurrency] = useState<string>('EUR');

  const [inFlight, setInFlight] = useState(false);
  const [result, setResult] = useState<ConversionResponse | null>(null);
  const [error, setError] = useState<ProblemDetails | null>(null);

  const [history, setHistory] = useState<ConversionAuditRecord[]>([]);

  const apiBaseDebug = useMemo(() => apiClient.baseUrl, []);
  useEffect(() => {
    // Preload recent conversions.
    apiClient
      .listRecent({ limit: 10 })
      .then((r) => setHistory(r.records))
      .catch(() => {
        // Non-fatal; the user can still convert.
      });
  }, []);

  async function submit() {
    setError(null);
    setResult(null);

    const req: ConversionRequest = {
      amount: Number(amount),
      fromCurrency,
      toCurrency
    };

    setInFlight(true);
    try {
      const r = await apiClient.convert(req);
      setResult(r);
      const refreshed = await apiClient.listRecent({ limit: 10 });
      setHistory(refreshed.records);
    } catch (e) {
      const pd = e as ProblemDetails;
      setError(pd);
    } finally {
      setInFlight(false);
    }
  }

  function renderProblem(pd: ProblemDetails) {
    const title = pd.title ?? 'Request failed';
    const detail = pd.detail ?? '';
    return (
      <div className="errorBox" role="alert">
        <div className="errorTitle">{title}</div>
        {detail ? <div className="errorDetail">{detail}</div> : null}
      </div>
    );
  }

  return (
    <div className="page">
      <header className="header">
        <div>
          <h1>Real-Time Currency Conversion</h1>
          <p className="sub">Instant results with an immutable audit trail.</p>
        </div>
        <div className="meta">
          <div className="metaRow">
            <span className="metaLabel">API base</span>
            <span className="metaValue">{apiBaseDebug || '(relative)'}</span>
          </div>
        </div>
      </header>

      <main className="grid">
        <section className="card">
          <h2>Convert</h2>
          <div className="form">
            <label>
              Amount
              <input
                inputMode="decimal"
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                placeholder="100.00"
                aria-label="amount"
              />
            </label>

            <label>
              From currency
              <input
                value={fromCurrency}
                onChange={(e) => setFromCurrency(e.target.value)}
                placeholder="USD"
                aria-label="fromCurrency"
              />
            </label>

            <label>
              To currency
              <input
                value={toCurrency}
                onChange={(e) => setToCurrency(e.target.value)}
                placeholder="EUR"
                aria-label="toCurrency"
              />
            </label>

            <button className="primary" onClick={submit} disabled={inFlight}>
              {inFlight ? 'Converting…' : 'Convert'}
            </button>
          </div>

          {error ? renderProblem(error) : null}

          {result ? (
            <div className="result">
              <h3>Conversion Result</h3>
              <dl>
                <div className="kv">
                  <dt>Audit ID</dt>
                  <dd className="mono">{result.auditId}</dd>
                </div>
                <div className="kv">
                  <dt>Converted amount</dt>
                  <dd>{result.convertedAmount} {result.toCurrency}</dd>
                </div>
                <div className="kv">
                  <dt>Rate</dt>
                  <dd>{result.rate} ({result.fromCurrency} → {result.toCurrency})</dd>
                </div>
                <div className="kv">
                  <dt>Provider marker</dt>
                  <dd>{result.providerDateOrMarker}</dd>
                </div>
                <div className="kv">
                  <dt>Executed at (UTC)</dt>
                  <dd>{result.executedAtUtc}</dd>
                </div>
              </dl>
            </div>
          ) : null}
        </section>

        <section className="card">
          <h2>Recent Audit Trail</h2>
          <div className="history">
            {history.length === 0 ? (
              <div className="muted">No conversions yet.</div>
            ) : (
              <ul>
                {history.map((r) => (
                  <li key={r.auditId} className="historyItem">
                    <div className="historyTop">
                      <div className="mono">{r.auditId}</div>
                      <div className="muted">{r.executedAtUtc}</div>
                    </div>
                    <div className="historyMid">
                      <span>
                        {r.amount} {r.fromCurrency} → {r.convertedAmount} {r.toCurrency}
                      </span>
                    </div>
                    <div className="historyBottom muted">Rate {r.rate} · Provider {r.providerDateOrMarker}</div>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </section>
      </main>
    </div>
  );
}
