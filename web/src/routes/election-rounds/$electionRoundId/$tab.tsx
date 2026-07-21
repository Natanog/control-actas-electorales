import ElectionRoundDetails from '@/features/election-rounds/components/ElectionRoundDetails/ElectionRoundDetails';
import { ElectionRoundDetailsTab } from '@/features/election-rounds/components/ElectionRoundDetails/tabs';
import { electionRoundDetailsQueryOptions } from '@/features/election-rounds/queries';
import { redirectIfNotAuth, redirectIfNotPlatformAdmin } from '@/lib/utils';
import { createFileRoute, redirect } from '@tanstack/react-router';

const coerceTabSlug = (slug: string) => {
  const allowed = Object.values(ElectionRoundDetailsTab);
  return allowed.includes(slug as ElectionRoundDetailsTab)
    ? (slug as ElectionRoundDetailsTab)
    : ElectionRoundDetailsTab.EventDetails;
};

export const Route = createFileRoute('/election-rounds/$electionRoundId/$tab')({
  component: ElectionRoundDetails,
  loader: ({ context: { queryClient }, params: { electionRoundId } }) =>
    queryClient.ensureQueryData(electionRoundDetailsQueryOptions(electionRoundId)),
  beforeLoad: ({ params: { tab, electionRoundId } }) => {
    redirectIfNotAuth();
    redirectIfNotPlatformAdmin();
    const coercedTab = coerceTabSlug(tab);
    if (tab !== coercedTab) {
      throw redirect({ to: `/election-rounds/$electionRoundId/$tab`, params: { tab: coercedTab, electionRoundId }, replace: true });
    }
  },
});
