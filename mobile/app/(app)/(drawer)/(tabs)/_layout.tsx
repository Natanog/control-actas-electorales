import React from "react";
import { Tabs } from "expo-router";
import { TextStyle, ViewStyle } from "react-native";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { useTheme } from "tamagui";
import { Icon } from "../../../../components/Icon";
import { useUserData } from "../../../../contexts/user/UserContext.provider";

export default function TabLayout() {
  const { isAssignedToEllectionRound } = useUserData();
  const insets = useSafeAreaInsets();
  const theme = useTheme();

  return (
    <Tabs screenOptions={{
      tabBarActiveTintColor: theme.purple5?.val,
      tabBarHideOnKeyboard: true,
      headerShown: false,
      tabBarStyle: [$tabBar, { height: insets.bottom + 60 }],
      tabBarLabelStyle: $tabBarLabel,
    }}>
      <Tabs.Screen name="index" options={{ title: "Observación", tabBarIcon: ({ color }) => <Icon icon="observation" color={color} /> }} />
      <Tabs.Screen name="quick-report" options={{
        title: "Actas",
        tabBarIcon: ({ color }) => <Icon icon="quickReport" color={color} />,
        href: isAssignedToEllectionRound ? "/quick-report" : null,
      }} />
      <Tabs.Screen name="guides" options={{ title: "Guías", tabBarIcon: ({ color }) => <Icon icon="learning" color={color} />, href: isAssignedToEllectionRound ? "/guides" : null }} />
      <Tabs.Screen name="inbox" options={{ title: "Mensajes", tabBarIcon: ({ color }) => <Icon icon="inbox" color={color} />, href: isAssignedToEllectionRound ? "/inbox" : null }} />
      <Tabs.Screen name="more" options={{ title: "Más", tabBarIcon: ({ color }) => <Icon icon="more" color={color} /> }} />
    </Tabs>
  );
}

const $tabBar: ViewStyle = { backgroundColor: "white" };
const $tabBarLabel: TextStyle = { marginBottom: 4, marginTop: -12, fontFamily: "Roboto", fontSize: 12 };
