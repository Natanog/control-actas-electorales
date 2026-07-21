import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Alert, Image } from "react-native";
import NetInfo from "@react-native-community/netinfo";
import { Input, XStack, YStack } from "tamagui";
import Button from "../../../../components/Button";
import Card from "../../../../components/Card";
import { Screen } from "../../../../components/Screen";
import SelectPollingStation from "../../../../components/SelectPollingStation";
import { Typography } from "../../../../components/Typography";
import { useUserData } from "../../../../contexts/user/UserContext.provider";
import { useCamera } from "../../../../hooks/useCamera";
import {
  enqueueActa,
  getPendingActas,
  PendingActa,
  syncPendingActas,
  uploadActa,
} from "../../../../services/acta-queue";

const ActaCapture = () => {
  const { electionRounds, visits, selectedPollingStation, setSelectedPollingStation } = useUserData();
  const { uploadCameraOrMedia } = useCamera();
  const [contestCode, setContestCode] = useState("ALCALDE");
  const [notes, setNotes] = useState("");
  const [image, setImage] = useState<{ uri: string; name: string; type: string }>();
  const [pending, setPending] = useState<PendingActa[]>([]);
  const [saving, setSaving] = useState(false);
  const electionRoundId = electionRounds[0]?.id;

  const refreshQueue = useCallback(async () => setPending(await getPendingActas()), []);
  useEffect(() => { refreshQueue(); }, [refreshQueue]);
  useEffect(() => NetInfo.addEventListener(async (state) => {
    if (state.isConnected) {
      await syncPendingActas();
      await refreshQueue();
    }
  }), [refreshQueue]);

  const canSubmit = useMemo(
    () => !!electionRoundId && !!selectedPollingStation && !!contestCode.trim() && !!image,
    [contestCode, electionRoundId, image, selectedPollingStation],
  );

  const takePhoto = async () => {
    const result = await uploadCameraOrMedia("camera");
    if (result) setImage(result);
  };

  const submit = async () => {
    if (!canSubmit || !image || !electionRoundId || !selectedPollingStation) return;
    setSaving(true);
    const item: PendingActa = {
      localId: `${Date.now()}-${selectedPollingStation}`,
      electionRoundId,
      pollingStationId: selectedPollingStation,
      contestCode: contestCode.trim().toUpperCase(),
      imageUri: image.uri,
      fileName: image.name || `acta-${Date.now()}.jpg`,
      mimeType: image.type || "image/jpeg",
      capturedAt: new Date().toISOString(),
      notes: notes.trim() || undefined,
      attempts: 0,
    };

    try {
      const network = await NetInfo.fetch();
      if (network.isConnected) {
        const response = await uploadActa(item);
        Alert.alert("Acta archivada", `${response.code}\nEstado: ${response.status}\nHash: ${response.hash.slice(0, 12)}…`);
      } else {
        await enqueueActa(item);
        Alert.alert("Guardada sin conexión", "El acta se sincronizará automáticamente al recuperar internet.");
      }
      setImage(undefined);
      setNotes("");
    } catch (error: any) {
      await enqueueActa(item);
      Alert.alert("Pendiente de sincronización", error?.response?.data ?? "No fue posible subirla. Se conservó en el dispositivo.");
    } finally {
      setSaving(false);
      await refreshQueue();
    }
  };

  const syncNow = async () => {
    setSaving(true);
    const result = await syncPendingActas();
    setSaving(false);
    await refreshQueue();
    Alert.alert("Sincronización", `${result.uploaded} subidas; ${result.failed} pendientes.`);
  };

  return (
    <Screen preset="scroll" backgroundColor="white" contentContainerStyle={{ gap: 16, paddingBottom: 32 }}>
      <YStack paddingHorizontal="$md" paddingTop="$md" gap="$sm">
        <Typography preset="heading">Captura de actas</Typography>
        <Typography color="$gray7">
          Sistema interno de control documental. Los resultados mostrados no son oficiales.
        </Typography>
      </YStack>

      <SelectPollingStation
        placeholder="Selecciona la junta o recinto"
        options={visits}
        value={selectedPollingStation}
        onValueChange={setSelectedPollingStation}
      />

      <YStack paddingHorizontal="$md" gap="$sm">
        <Typography preset="subheading">Dignidad o elección</Typography>
        <Input value={contestCode} onChangeText={setContestCode} placeholder="Ej. ALCALDE" autoCapitalize="characters" />

        <Typography preset="subheading">Fotografía original</Typography>
        {image ? (
          <Card padding="$sm">
            <Image source={{ uri: image.uri }} style={{ width: "100%", height: 280, borderRadius: 10 }} resizeMode="contain" />
            <XStack gap="$sm" marginTop="$sm">
              <Button flex={1} preset="outlined" onPress={takePhoto}>Repetir foto</Button>
              <Button flex={1} preset="chromeless" onPress={() => setImage(undefined)}>Quitar</Button>
            </XStack>
          </Card>
        ) : (
          <Button onPress={takePhoto}>Tomar foto del acta</Button>
        )}

        <Typography preset="subheading">Observaciones opcionales</Typography>
        <Input value={notes} onChangeText={setNotes} placeholder="Novedades de captura, legibilidad o entrega" multiline />
        <Button disabled={!canSubmit || saving} onPress={submit}>
          {saving ? "Guardando…" : "Archivar acta"}
        </Button>
      </YStack>

      <Card marginHorizontal="$md" padding="$md">
        <Typography preset="subheading">Pendientes de sincronización: {pending.length}</Typography>
        {pending.slice(0, 5).map((item) => (
          <Typography key={item.localId} color="$gray7" marginTop="$xs">
            {item.contestCode} · junta {item.pollingStationId} · intento {item.attempts}
          </Typography>
        ))}
        {pending.length > 0 && <Button marginTop="$sm" preset="outlined" disabled={saving} onPress={syncNow}>Sincronizar ahora</Button>}
      </Card>
    </Screen>
  );
};

export default ActaCapture;
