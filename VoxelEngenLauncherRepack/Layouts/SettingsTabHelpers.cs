using VoxelEngenLauncherRepack.Layouts;
internal static class SettingsTabHelpers
{
    public static int GetIndexLang(string key)
    {
        int index = 0;
        for (int i = 0; i < SettingsTab.Languages.Length; i++)
        {
            if (SettingsTab.Languages[i].Key == key)
            {
                index = i;
                break;
            }
        }
        return index;
    }
}