import { authApi } from '@/common/auth-api';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { useQuery } from '@tanstack/react-query';
import { AlertTriangle, CheckCircle2, FileImage, ScanText } from 'lucide-react';

interface Props { electionRoundId: string; }
interface Summary {
  total: number;
  archived: number;
  ocrProcessing: number;
  ocrSuccess: number;
  manualReview: number;
  validated: number;
  observed: number;
  duplicates: number;
  conflicts: number;
  validationRate: number;
}
interface ActaItem {
  id: string;
  code: string;
  pollingStationNumber: string;
  contestCode: string;
  status: string;
  hash: string;
  ocrConfidence?: number;
  capturedAt: string;
}

export default function ActasDashboard({ electionRoundId }: Props) {
  const summary = useQuery({
    queryKey: ['actas', electionRoundId, 'dashboard'],
    queryFn: async () => (await authApi.get<Summary>(`/api/election-rounds/${electionRoundId}/actas:dashboard`)).data,
  });
  const list = useQuery({
    queryKey: ['actas', electionRoundId, 'list'],
    queryFn: async () => (await authApi.get<{ total: number; items: ActaItem[] }>(`/api/election-rounds/${electionRoundId}/actas`, { params: { pageSize: 100 } })).data,
  });

  const cards = [
    ['Recibidas', summary.data?.total ?? 0, FileImage],
    ['Validadas', summary.data?.validated ?? 0, CheckCircle2],
    ['Revisión manual', summary.data?.manualReview ?? 0, ScanText],
    ['Observadas o conflicto', (summary.data?.observed ?? 0) + (summary.data?.conflicts ?? 0), AlertTriangle],
  ] as const;

  return (
    <div className='space-y-6'>
      <div className='rounded-lg border border-amber-300 bg-amber-50 px-4 py-3 text-sm text-amber-950'>
        Sistema interno de control y contraste documental. No sustituye los resultados oficiales de la autoridad electoral.
      </div>
      <div className='grid gap-4 md:grid-cols-2 xl:grid-cols-4'>
        {cards.map(([label, value, Icon]) => (
          <Card key={label}>
            <CardHeader className='flex flex-row items-center justify-between pb-2'>
              <CardTitle className='text-sm font-medium'>{label}</CardTitle><Icon className='h-5 w-5 text-muted-foreground' />
            </CardHeader>
            <CardContent><div className='text-3xl font-semibold'>{value}</div></CardContent>
          </Card>
        ))}
      </div>
      <Card>
        <CardHeader><CardTitle>Actas recibidas</CardTitle></CardHeader>
        <CardContent>
          <div className='overflow-x-auto'>
            <table className='w-full text-sm'>
              <thead><tr className='border-b text-left text-muted-foreground'>
                <th className='py-3 pr-4'>Código</th><th className='pr-4'>Junta</th><th className='pr-4'>Dignidad</th>
                <th className='pr-4'>Estado</th><th className='pr-4'>OCR</th><th>Hash</th>
              </tr></thead>
              <tbody>
                {(list.data?.items ?? []).map((acta) => (
                  <tr key={acta.id} className='border-b last:border-0'>
                    <td className='py-3 pr-4 font-medium'>{acta.code}</td><td className='pr-4'>{acta.pollingStationNumber}</td>
                    <td className='pr-4'>{acta.contestCode}</td><td className='pr-4'>{acta.status}</td>
                    <td className='pr-4'>{acta.ocrConfidence == null ? '—' : `${Math.round(acta.ocrConfidence * 100)}%`}</td>
                    <td className='font-mono text-xs'>{acta.hash.slice(0, 12)}…</td>
                  </tr>
                ))}
                {!list.isLoading && (list.data?.items.length ?? 0) === 0 && (
                  <tr><td colSpan={6} className='py-10 text-center text-muted-foreground'>Aún no se han recibido actas.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
