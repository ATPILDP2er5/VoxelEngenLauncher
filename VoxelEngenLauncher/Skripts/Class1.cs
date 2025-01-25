using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace VoxelEngenLauncher.Skripts
{
    public class PlayerInfo
    {
        public List<AccountPlayer> AccountPlayer { get; set; }
    }

    public class AccountPlayer
    {
        public LoginInfo Login { get; set; }
        public SkinInfo Skin { get; set; }
        public List<WorldInfo> PlayersWorldCreate { get; set; }
    }

    public class LoginInfo
    {
        public string GameName { get; set; }
        public string HashCode { get; set; }
    }

    public class SkinInfo
    {
        public string SkinPath { get; set; }
    }

    public class WorldInfo
    {
        public string WorldName { get; set; }
        public string WorldPath { get; set; }
    }

    public class GetAccounts
    {
        public static PlayerInfo playerInfo = new PlayerInfo();

        public static void GetAccoutsFromLocal()
        {
            // Путь до JSON-файла
            string jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Player", "PlayersInfo.json");

            // Проверяем, существует ли файл
            if (!File.Exists(jsonFilePath))
            {
                // Создаём файл с базовой структурой
                CreateDefaultJsonFile(jsonFilePath);
            }

            try
            {
                // Читаем содержимое JSON-файла
                string jsonContent = File.ReadAllText(jsonFilePath);

                // Десериализуем JSON в объект PlayerInfo
                playerInfo = JsonSerializer.Deserialize<PlayerInfo>(jsonContent);

                // Если объект пустой, создаём базовую структуру
                if (playerInfo?.AccountPlayer == null)
                {
                    playerInfo = CreateDefaultPlayerInfo();
                    SavePlayerInfoToFile(jsonFilePath, playerInfo);
                }
            }
            catch
            {
                // При ошибке создаём базовую структуру и сохраняем
                playerInfo = CreateDefaultPlayerInfo();
                SavePlayerInfoToFile(jsonFilePath, playerInfo);
            }
        }

        /// <summary>
        /// Создаёт новый JSON-файл с базовой структурой.
        /// </summary>
        /// <param name="filePath">Путь к создаваемому файлу.</param>
        private static void CreateDefaultJsonFile(string filePath)
        {
            var defaultData = CreateDefaultPlayerInfo();

            // Сохраняем объект в JSON
            SavePlayerInfoToFile(filePath, defaultData);
        }

        /// <summary>
        /// Создаёт объект с базовой структурой PlayerInfo.
        /// </summary>
        private static PlayerInfo CreateDefaultPlayerInfo()
        {
            return new PlayerInfo
            {
                AccountPlayer = new List<AccountPlayer>
                {
                    new AccountPlayer
                    {
                        Login = new LoginInfo
                        {
                            GameName = "DefaultNickName",
                            HashCode = "NullCode"
                        },
                        Skin = new SkinInfo
                        {
                            SkinPath = "Player/skin/DefaultSkin.png"
                        },
                        PlayersWorldCreate = new List<WorldInfo>
                        {
                            new WorldInfo
                            {
                                WorldName = "NewWorld",
                                WorldPath = "none"
                            }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Сохраняет объект PlayerInfo в JSON-файл.
        /// </summary>
        /// <param name="filePath">Путь к файлу.</param>
        /// <param name="playerInfo">Объект PlayerInfo для сохранения.</param>
        private static void SavePlayerInfoToFile(string filePath, PlayerInfo playerInfo)
        {
            try
            {
                // Создаём папку, если её нет
                string directoryPath = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // Сериализация объекта в JSON
                string jsonContent = JsonSerializer.Serialize(playerInfo, new JsonSerializerOptions
                {
                    WriteIndented = true // Форматирование для читаемости
                });

                // Запись JSON в файл
                File.WriteAllText(filePath, jsonContent);
            }
            catch (Exception ex)
            {
                // Лог ошибок (если требуется)
                throw new Exception($"Ошибка при сохранении JSON: {ex.Message}");
            }
        }
    }
}

