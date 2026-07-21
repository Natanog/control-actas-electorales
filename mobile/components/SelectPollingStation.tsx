import React, { useMemo, useState } from "react";
import { Adapt, Select, SelectProps, Sheet, View, YStack } from "tamagui";
import { Icon } from "./Icon";
import { Typography } from "./Typography";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { PollingStationVisitVM } from "../services/definitions.api";
import Button from "../components/Button";

interface SelectPollingStationProps extends SelectProps {
  placeholder?: string;
  options: PollingStationVisitVM[];
}

const SelectPollingStation: React.FC<SelectPollingStationProps> = ({
  options,
  placeholder = "Selecciona la junta",
  value,
  defaultValue,
  onValueChange,
  ...selectProps
}) => {
  const [internalValue, setInternalValue] = useState(defaultValue?.toString() ?? "");
  const currentValue = value?.toString() ?? internalValue;
  const insets = useSafeAreaInsets();

  const changeValue = (next: string) => {
    if (value === undefined) setInternalValue(next);
    onValueChange?.(next);
  };

  return (
    <YStack paddingVertical="$xs" paddingHorizontal="$md" backgroundColor="white">
      <Select {...selectProps} value={currentValue} onValueChange={changeValue} disablePreventBodyScroll>
        <Select.Trigger
          justifyContent="center"
          alignItems="center"
          backgroundColor="$purple1"
          borderRadius="$10"
          iconAfter={<Icon icon="chevronRight" size={24} transform="rotate(90deg)" color="$purple5" />}
        >
          <Select.Value width="90%" color="$purple5" placeholder={placeholder} fontWeight="500" />
        </Select.Trigger>

        <Adapt platform="touch">
          <Sheet native modal snapPoints={[80, 50]}>
            <Sheet.Frame>
              <YStack paddingVertical="$xl" paddingLeft="$lg" paddingRight="$xxxl" borderBottomWidth={1} borderBottomColor="$gray3">
                <Typography preset="body2" color="$gray5">Mis juntas asignadas</Typography>
                <Typography numberOfLines={5} color="$gray5" marginTop="$xxs">
                  Selecciona la junta en la que vas a fotografiar y archivar el acta.
                </Typography>
              </YStack>
              <Sheet.ScrollView padding="$sm"><Adapt.Contents /></Sheet.ScrollView>
              <View paddingVertical="$xl" paddingHorizontal={40} borderTopWidth={1} borderTopColor="$gray3" marginBottom={insets.bottom}>
                <Button preset="outlined">Agregar visita</Button>
              </View>
            </Sheet.Frame>
            <Sheet.Overlay />
          </Sheet>
        </Adapt>

        <Select.Content>
          <Select.Viewport>
            <Select.Group>
              {useMemo(() => options.map((entry, index) => (
                <Select.Item index={index} key={entry.pollingStationId} value={entry.pollingStationId} gap="$3">
                  <Select.ItemText width="90%" numberOfLines={2}>
                    {entry.pollingStationId}{entry.visitedAt ? ` · ${entry.visitedAt}` : ""}
                  </Select.ItemText>
                  <Select.ItemIndicator><Icon icon="chevronLeft" /></Select.ItemIndicator>
                </Select.Item>
              )), [options])}
            </Select.Group>
          </Select.Viewport>
        </Select.Content>
      </Select>
    </YStack>
  );
};

export default SelectPollingStation;
