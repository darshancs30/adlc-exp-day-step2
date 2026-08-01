export type ProblemDetails = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
};

export type ConversionRequest = {
  amount: number;
  fromCurrency: string;
  toCurrency: string;
};

export type ConversionResponse = {
  auditId: string;
  amount: number;
  fromCurrency: string;
  toCurrency: string;
  rate: number;
  convertedAmount: number;
  providerDateOrMarker: string;
  executedAtUtc: string;
};

export type ConversionAuditRecord = {
  auditId: string;
  amount: number;
  fromCurrency: string;
  toCurrency: string;
  rate: number;
  convertedAmount: number;
  providerDateOrMarker: string;
  executedAtUtc: string;
};

const apiBaseUrl = (() => {
  const fromWindow = typeof window !== 'undefined' ? window.__VITE_API_URL__ : undefined;
  return (fromWindow ?? '').trim();
})();

async function readProblemDetails(res: Response): Promise<ProblemDetails> {
  try {
    const body = (await res.json()) as ProblemDetails;
    return body;
  } catch {
    return {
      title: 'Request failed',
      status: res.status
    };
  }
}

class ApiClient {
  baseUrl: string;

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl;
  }

  private url(path: string) {
    if (!this.baseUrl) return path;
    const trimmed = this.baseUrl.replace(/\/+$/, '');
    return `${trimmed}${path}`;
  }

  async convert(req: ConversionRequest): Promise<ConversionResponse> {
    const res = await fetch(this.url('/api/conversions'), {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(req)
    });

    if (!res.ok) {
      const pd = await readProblemDetails(res);
      throw pd;
    }
    return (await res.json()) as ConversionResponse;
  }

  async getById(auditId: string): Promise<ConversionAuditRecord> {
    const res = await fetch(this.url(`/api/conversions/${encodeURIComponent(auditId)}`));
    if (!res.ok) {
      const pd = await readProblemDetails(res);
      throw pd;
    }
    return (await res.json()) as ConversionAuditRecord;
  }

  async listRecent(params: { limit: number }): Promise<{ records: ConversionAuditRecord[] }> {
    const res = await fetch(this.url(`/api/conversions/recent?limit=${params.limit}`));
    if (!res.ok) {
      const pd = await readProblemDetails(res);
      throw pd;
    }
    return (await res.json()) as { records: ConversionAuditRecord[] };
  }
}

export const apiClient = new ApiClient(apiBaseUrl);
