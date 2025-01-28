using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IO;
using Tomlyn;
using Tomlyn.Model;


namespace VoxelEngenLauncher.Resurces.Skripts
{

    public class SettingsManager
    {
        public static void UpdateGlobalSettings(string PathSettingsGame)
        {
            // Формируем пути к файлам
            string globalSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resurces\\Data\\GlobalSettings.toml");

            // Проверяем существование файлов
            if (!File.Exists(PathSettingsGame))
            {
                return;
            }
            if (!File.Exists(globalSettingsPath))
            {
                return;
            }

            try
            {
                // Загружаем файлы TOML
                string settingsContent = File.ReadAllText(PathSettingsGame);
                string globalContent = File.ReadAllText(globalSettingsPath);

                // Преобразуем их в модели
                var settingsData = Toml.ToModel<TomlTable>(settingsContent);
                var globalData = Toml.ToModel<TomlTable>(globalContent);

                // Обновляем глобальные настройки из settings.toml
                MergeTomlTables(globalData, settingsData);

                // Сохраняем обновлённый файл GlobalSettings.toml
                string updatedGlobalContent = Toml.FromModel(globalData);
                File.WriteAllText(globalSettingsPath, updatedGlobalContent);

                Console.WriteLine($"Глобальные настройки успешно обновлены: {globalSettingsPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки TOML файлов: {ex.Message}");
            }
        }

        /// <summary>
        /// Рекурсивно обновляет данные в целевой таблице из источника.
        /// </summary>
        private static void MergeTomlTables(TomlTable target, TomlTable source)
        {
            foreach (var key in source.Keys)
            {
                if (source[key] is TomlTable sourceSubTable && target[key] is TomlTable targetSubTable)
                {
                    // Рекурсивное объединение для вложенных таблиц
                    MergeTomlTables(targetSubTable, sourceSubTable);
                }
                else
                {
                    // Перезаписываем значение из settings.toml
                    target[key] = source[key];
                }
            }
        }
    }

}
