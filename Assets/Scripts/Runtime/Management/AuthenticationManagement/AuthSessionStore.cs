using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.AuthenticationManagement
{
    /// <summary>
    /// File-based persistence for the authenticated session, so the user does not
    /// have to log in on every launch. Stores a base64-wrapped JSON of the
    /// AuthenticationResponse under Application.persistentDataPath/auth/session.dat.
    /// Cleared on logout, session expiry, and parse failure.
    /// </summary>
    internal static class AuthSessionStore
    {
        const string k_FolderName = "auth";
        const string k_FileName = "session.dat";

        // Builds an OS-conventional config path that avoids Unity's legacy "unity3d/" segment on Linux.
        // Uses companyName/productName from Player Settings so it stays in sync with the project.
        static string DirectoryPath
        {
            get
            {
                string company = string.IsNullOrEmpty(Application.companyName) ? "RemakeSoF" : Application.companyName;
                string product = string.IsNullOrEmpty(Application.productName) ? "RemakeSoF" : Application.productName;
                string baseDir = GetUserConfigBaseDir();
                return Path.Combine(baseDir, company, product, k_FolderName);
            }
        }

        static string FilePath => Path.Combine(DirectoryPath, k_FileName);

        static string GetUserConfigBaseDir()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            // %APPDATA% (Roaming)
            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support");
#else
            // Linux: XDG_CONFIG_HOME with fallback to $HOME/.config
            string xdg = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (!string.IsNullOrEmpty(xdg)) return xdg;
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
#endif
        }

        public static void Save(AuthenticationResponse response)
        {
            if (response == null) return;
            try
            {
                Directory.CreateDirectory(DirectoryPath);
                string json = JsonUtility.ToJson(response);
                string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
                File.WriteAllText(FilePath, encoded);
                TrySetOwnerOnlyPermissions(FilePath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AuthSessionStore] Save failed: {ex.Message}");
            }
        }

        public static AuthenticationResponse TryLoad()
        {
            try
            {
                if (!File.Exists(FilePath)) return null;
                string encoded = File.ReadAllText(FilePath);
                string json = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                AuthenticationResponse response = JsonUtility.FromJson<AuthenticationResponse>(json);
                if (response == null || string.IsNullOrEmpty(response.token))
                {
                    Clear();
                    return null;
                }
                return response;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AuthSessionStore] Load failed, clearing session: {ex.Message}");
                Clear();
                return null;
            }
        }

        public static void Clear()
        {
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AuthSessionStore] Clear failed: {ex.Message}");
            }
        }

        static void TrySetOwnerOnlyPermissions(string path)
        {
#if UNITY_EDITOR_LINUX || UNITY_EDITOR_OSX || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
            try
            {
                // 0o600 = owner read/write only
                Chmod(path, 0x180);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AuthSessionStore] chmod 600 failed: {ex.Message}");
            }
#endif
        }

#if UNITY_EDITOR_LINUX || UNITY_EDITOR_OSX || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
        [DllImport("libc", EntryPoint = "chmod", SetLastError = true)]
        static extern int Chmod(string pathname, uint mode);
#endif
    }
}
