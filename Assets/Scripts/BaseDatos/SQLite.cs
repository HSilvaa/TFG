using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.VisualScripting.Dependencies.Sqlite;
using System.Linq;
using UnityEngine.UI;
using TMPro;
using static Unity.Collections.AllocatorManager;
using Unity.VisualScripting;
using System;

public class SQLite : MonoBehaviour
{
    //"C:\Users\hugop\Desktop\TFG\ProyectoUnity\AIGERIM-TFG\BasesDeDatos"
    private SQLiteConnection db;
    private string dbPath;
    private static SQLite _instance;
    public static SQLite Instance
    {
        get
        {
            if (_instance == null)
            {
                // Buscar si existe un objeto de tipo DatabaseManager
                _instance = FindObjectOfType<SQLite>();

                // Si no existe, crear uno nuevo
                if (_instance == null)
                {
                    GameObject go = new GameObject("SQLite");
                    _instance = go.AddComponent<SQLite>();
                }
            }
            return _instance;
        }
    }


    private static readonly object dbLock = new object();

    void Awake()
    {
        //ResetDatabase();
        // Ruta de la base de datos SQLite
        string unityProjectPath = Application.dataPath; // .../AIGERIM-TFG 3D/Assets
        string projectRoot = Directory.GetParent(unityProjectPath).FullName; // .../AIGERIM-TFG 3D
        string dbFolder = Path.Combine(projectRoot, "BasesDeDatos");

        if (!Directory.Exists(dbFolder))
        {
            Directory.CreateDirectory(dbFolder);
        }

        dbPath = Path.Combine(dbFolder, "TFG_DataBase.db");

        Debug.Log("Ruta DB: " + dbPath);

        // Crear base de datos y tablas si no existen
        using (var db = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create))
        {
            db.CreateTable<Character>();
            db.CreateTable<Conversation>();
            db.CreateTable<Resumen>();
            db.CreateTable<Folder>();
        }
    }

    // --- Clases con atributos del plugin ---
    [Table("Character")]
    public class Character
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Age { get; set; }
        public string Description { get; set; }
        public string Epoca { get; set; }
    }

    [Table("Resumen")]
    public class Resumen
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int CharacterId { get; set; }
        public string ResumenText { get; set; }

        public DateTime Hora { get; set; }
    }

    [Table("Conversation")]
    public class Conversation
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int CharacterId { get; set; }
        public string Historial { get; set; }
        public DateTime Hora { get; set; }
    }


    [Table("Folder")]
    public class Folder
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Route { get; set; }
        public int ParentFolder { get; set; }  // FK a otra Folder --> if null --> isRootFolder
    }

    [Table("FileItem")]
    public class FileItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int SubFolderId { get; set; }  // FK a Folder
        public string FileName { get; set; }
        public string Route { get; set; }
    }

    // --- Métodos para manejar archivos ---
    public int AddFolder(string name, string route, int parentFolder = 0) //Default folder has no parent
    {
        lock (dbLock)
        {
            using (var db = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite))
            {
                var newFolder = new Folder
                {
                    Name = name,
                    Route = route,
                    ParentFolder = parentFolder,
                };

                db.Insert(newFolder);
                return newFolder.Id;
            }
        }
    }

    public void UpdateFolder(int folderToUpdate, string name, string route, int parentFolder = 0)
    {
        lock (dbLock)
        {
            using (var db = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite))
            {
                var existingFolder = db.Table<Folder>().FirstOrDefault(c => c.Id == 1); //GUARRADA PERO SOLO DEBE HABER UNO

                if (existingFolder != null)
                {
                    existingFolder.Name = name;
                    existingFolder.Route = route;
                    existingFolder.ParentFolder = parentFolder;

                    db.Update(existingFolder);
                }
            }
        }
    }

    public Folder GetFolder(int folderId)
    {
        lock (dbLock)
        {
            using (var db = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite))
            { 
                Folder folder = db.Table<Folder>().Where(c => c.Id == folderId).FirstOrDefault();

                if (folder != null)
                {
                    return folder;
                }
                return null;
            }
        }
    }

    public int AddFile(string fileName, string route, int subFolderId)
    {
        lock (dbLock)
        {
            using (var db = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite))
            {
                var existingFolder = db.Table<Folder>().FirstOrDefault(c => c.Id == subFolderId);

                if (existingFolder != null) //Si existe el folder, lo añado
                {
                    var newItem = new FileItem
                    {
                        FileName = fileName,
                        Route = route,
                        SubFolderId = subFolderId,
                    };

                    db.Insert(newItem);
                    return newItem.Id;
                }
                return -1;
            }
        }
    }

    public void UpdateFile(int itemToUpdate, string fileName, string route, int subFolderId = 0)
    {
        lock (dbLock)
        {
            using (var db = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite))
            {
                var existingFile = db.Table<FileItem>().FirstOrDefault(c => c.Id == itemToUpdate);

                if (existingFile != null)
                {
                    existingFile.FileName = fileName;
                    existingFile.Route = route;
                    existingFile.SubFolderId = subFolderId;

                    db.Update(existingFile);
                }
            }
        }
    }

    // --- Métodos para manejar personajes ---
    public int AddCharacter(string name, string age, string description, string epoca)
    {
        lock (dbLock)
        {
            using (var db = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite))
            {
                var newCharacter = new Character
                {
                    Name = name,
                    Age = age,
                    Description = description,
                    Epoca = epoca,
                };

                db.Insert(newCharacter);

                Debug.Log($"Personaje '{name}' añadido correctamente con ID {newCharacter.Id}.");

                return newCharacter.Id;
            }
        }
    }

    public List<Character> GetCharacters()
    {
        lock (dbLock)
        {
            using (var db = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite))
            {
                return db.Table<Character>().ToList();
            }
        }
    }

    public List<Character> DeleteCharById(int id)
    {
        lock (dbLock)
        {
            using (var db = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite))
            {
                // Eliminar conversaciones asociadas
                var conversationsToDelete = db.Table<Conversation>().Where(c => c.CharacterId == id).ToList();
                foreach (var convo in conversationsToDelete)
                {
                    db.Delete(convo);
                }

                // Eliminar personaje
                var characterToDelete = db.Table<Character>().FirstOrDefault(c => c.Id == id);
                if (characterToDelete != null)
                {
                    db.Delete(characterToDelete);
                    Debug.Log($"Personaje con ID {id} y sus conversaciones han sido eliminados.");
                }
                else
                {
                    Debug.LogWarning($"No se encontró personaje con ID {id}.");
                }

                return db.Table<Character>().ToList();
            }
        }
    }


    // --- Métodos para conversaciones ---
    public void AddConversation(int characterId, string historial)
    {
        lock (dbLock)
        {
            using (var db = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite))
            {
                db.Insert(new Conversation
                {
                    CharacterId = characterId, //Personaje al que pertenecen
                    Historial = historial,     //Par de mensajes User-Assistant
                    Hora = DateTime.Now,       //Fecha del mensaje
                });
                Debug.Log("Conversación añadida correctamente.");
            }
        }
    }

    public List<Conversation> GetConversations(int characterId)
    {
        lock (dbLock)
        {
            using (var db = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite))
            {
                return db.Table<Conversation>().Where(c => c.CharacterId == characterId).ToList();
            }
        }
    }

    public Conversation? GetLastConversation(int characterId)
    {
        lock (dbLock)
        {
            using var db = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite);
            return db.Table<Conversation>()
                     .Where(c => c.CharacterId == characterId)
                     .OrderByDescending(c => c.Hora)
                     .FirstOrDefault(); // puede devolver null si no hay ninguna
        }
    }

    public void AddResumen(int characterId, string resumenText)
    {
        lock (dbLock)
        {
            using (var db = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite))
            {
                db.Insert(new Resumen
                {
                    CharacterId = characterId, //Personaje al que pertenecen
                    ResumenText = resumenText, //Resumen
                    Hora = DateTime.Now        //Hora de la creación
                });
                Debug.Log("Resumen añadida correctamente.");
            }
        }
    }

    public List<Resumen> GetResumenById(int characterId)
    {
        lock (dbLock)
        {
            using (var db = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite))
            {
                return db.Table<Resumen>().Where(c => c.CharacterId == characterId).ToList();
            }
        }
    }

    public void UpdateResumen(int characterId, string newResumen)
    {
        lock (dbLock)
        {
            using (var db = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite))
            {
                var conversation = db.Table<Resumen>().FirstOrDefault(c => c.CharacterId == characterId);

                if (conversation != null)
                {
                    conversation.ResumenText = newResumen;
                    db.Update(conversation);
                    Debug.Log($"Resumen para el personaje con ID {characterId} actualizada.");
                }
                else
                {
                    Debug.LogWarning($"No se encontró resumen para el personaje con ID {characterId}.");
                }
            }
        }
    }


    //Nunca voy a updatear algo que ya se ha escrito
    //public void UpdateConversation(int characterId, string newHistorial, string newResumen)
    //{
    //    lock (dbLock)
    //    {
    //        using (var db = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite))
    //        {
    //            var conversation = db.Table<Conversation>().FirstOrDefault(c => c.CharacterId == characterId);

    //            if (conversation != null)
    //            {
    //                conversation.Historial = newHistorial;
    //                conversation.Resumen = newResumen;
    //                db.Update(conversation);
    //                Debug.Log($"Conversación con ID {characterId} actualizada.");
    //            }
    //            else
    //            {
    //                Debug.LogWarning($"No se encontró la conversación con ID {characterId}.");
    //            }
    //        }
    //    }
    //}

    public void ResetDatabase()
    {
        lock (dbLock)
        {
            using (var db = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite))
            {
                // Eliminar tablas
                db.Execute("DROP TABLE IF EXISTS Conversation;");
                db.Execute("DROP TABLE IF EXISTS Character;");
                db.Execute("DROP TABLE IF EXISTS Folder;");
                db.Execute("DROP TABLE IF EXISTS Resumen;");

                // Borrar secuencia de autoincremento
                db.Execute("DELETE FROM sqlite_sequence;");

                // Recrear las tablas
                db.CreateTable<Character>();
                db.CreateTable<Folder>();
                db.CreateTable<Conversation>();
                db.CreateTable<Resumen>();

                Debug.Log("Base de datos reseteada desde cero, incluyendo IDs.");
            }
        }
    }
}
