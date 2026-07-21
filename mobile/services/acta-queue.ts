import AsyncStorage from "@react-native-async-storage/async-storage";
import API from "./api";

const STORAGE_KEY = "control_actas.pending_uploads.v1";

export type PendingActa = {
  localId: string;
  electionRoundId: string;
  pollingStationId: string;
  contestCode: string;
  imageUri: string;
  fileName: string;
  mimeType: string;
  capturedAt: string;
  notes?: string;
  latitude?: number;
  longitude?: number;
  attempts: number;
  lastError?: string;
};

export async function getPendingActas(): Promise<PendingActa[]> {
  const raw = await AsyncStorage.getItem(STORAGE_KEY);
  if (!raw) return [];
  try {
    return JSON.parse(raw) as PendingActa[];
  } catch {
    return [];
  }
}

async function savePendingActas(items: PendingActa[]): Promise<void> {
  await AsyncStorage.setItem(STORAGE_KEY, JSON.stringify(items));
}

export async function enqueueActa(item: Omit<PendingActa, "attempts">): Promise<void> {
  const items = await getPendingActas();
  if (items.some((existing) => existing.localId === item.localId)) return;
  await savePendingActas([...items, { ...item, attempts: 0 }]);
}

export async function removePendingActa(localId: string): Promise<void> {
  const items = await getPendingActas();
  await savePendingActas(items.filter((item) => item.localId !== localId));
}

export async function uploadActa(item: PendingActa): Promise<{ id: string; code: string; hash: string; status: string }> {
  const form = new FormData();
  form.append("electionRoundId", item.electionRoundId);
  form.append("pollingStationId", item.pollingStationId);
  form.append("contestCode", item.contestCode);
  form.append("capturedAt", item.capturedAt);
  if (item.notes) form.append("notes", item.notes);
  if (item.latitude !== undefined) form.append("latitude", String(item.latitude));
  if (item.longitude !== undefined) form.append("longitude", String(item.longitude));
  form.append("file", {
    uri: item.imageUri,
    name: item.fileName,
    type: item.mimeType || "image/jpeg",
  } as unknown as Blob);

  const response = await API.post(
    `election-rounds/${item.electionRoundId}/actas`,
    form,
    { headers: { "Content-Type": "multipart/form-data" } },
  );
  return response.data;
}

export async function syncPendingActas(): Promise<{ uploaded: number; failed: number }> {
  const items = await getPendingActas();
  const remaining: PendingActa[] = [];
  let uploaded = 0;

  for (const item of items) {
    try {
      await uploadActa(item);
      uploaded += 1;
    } catch (error: any) {
      remaining.push({
        ...item,
        attempts: item.attempts + 1,
        lastError: error?.response?.data?.detail ?? error?.message ?? "Upload failed",
      });
    }
  }

  await savePendingActas(remaining);
  return { uploaded, failed: remaining.length };
}
