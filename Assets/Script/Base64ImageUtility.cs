using System;
using UnityEngine;

public static class Base64ImageUtility
{
    /// <summary>
    /// Converts a base64 string to a Unity Sprite
    /// </summary>
    /// <param name="base64String">Base64 encoded image data (with or without data URL prefix)</param>
    /// <returns>Unity Sprite or null if conversion fails</returns>
    public static Sprite Base64ToSprite(string base64String)
    {
        if (string.IsNullOrEmpty(base64String))
        {
            Debug.LogWarning("[Base64ImageUtility] Base64 string is null or empty");
            return null;
        }

        try
        {
            // Remove data URL prefix if present (e.g., "data:image/jpeg;base64,")
            string cleanBase64 = CleanBase64String(base64String);
            
            // Convert base64 to byte array
            byte[] imageBytes = Convert.FromBase64String(cleanBase64);
            
            // Create texture from byte array
            Texture2D texture = new Texture2D(2, 2);
            if (texture.LoadImage(imageBytes))
            {
                // Create sprite from texture
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                return sprite;
            }
            else
            {
                Debug.LogError("[Base64ImageUtility] Failed to load image from byte array");
                UnityEngine.Object.DestroyImmediate(texture);
                return null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Base64ImageUtility] Error converting base64 to sprite: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Converts a base64 string to a Unity Texture2D
    /// </summary>
    /// <param name="base64String">Base64 encoded image data (with or without data URL prefix)</param>
    /// <returns>Unity Texture2D or null if conversion fails</returns>
    public static Texture2D Base64ToTexture(string base64String)
    {
        if (string.IsNullOrEmpty(base64String))
        {
            Debug.LogWarning("[Base64ImageUtility] Base64 string is null or empty");
            return null;
        }

        try
        {
            // Remove data URL prefix if present
            string cleanBase64 = CleanBase64String(base64String);
            
            // Convert base64 to byte array
            byte[] imageBytes = Convert.FromBase64String(cleanBase64);
            
            // Create texture from byte array
            Texture2D texture = new Texture2D(2, 2);
            if (texture.LoadImage(imageBytes))
            {
                return texture;
            }
            else
            {
                Debug.LogError("[Base64ImageUtility] Failed to load image from byte array");
                UnityEngine.Object.DestroyImmediate(texture);
                return null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Base64ImageUtility] Error converting base64 to texture: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Checks if a string is a valid base64 image data
    /// </summary>
    /// <param name="base64String">String to check</param>
    /// <returns>True if valid base64 image data</returns>
    public static bool IsValidBase64Image(string base64String)
    {
        if (string.IsNullOrEmpty(base64String))
            return false;

        try
        {
            string cleanBase64 = CleanBase64String(base64String);
            byte[] imageBytes = Convert.FromBase64String(cleanBase64);
            
            // Basic check: try to create a texture to see if it's valid image data
            Texture2D testTexture = new Texture2D(2, 2);
            bool isValid = testTexture.LoadImage(imageBytes);
            UnityEngine.Object.DestroyImmediate(testTexture);
            
            return isValid;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Removes data URL prefix from base64 string if present
    /// </summary>
    /// <param name="base64String">Base64 string that may contain data URL prefix</param>
    /// <returns>Clean base64 string</returns>
    private static string CleanBase64String(string base64String)
    {
        if (base64String.StartsWith("data:image/"))
        {
            int commaIndex = base64String.IndexOf(',');
            if (commaIndex >= 0 && commaIndex < base64String.Length - 1)
            {
                return base64String.Substring(commaIndex + 1);
            }
        }
        return base64String;
    }

    /// <summary>
    /// Gets the image format from a base64 data URL string
    /// </summary>
    /// <param name="base64String">Base64 string with data URL prefix</param>
    /// <returns>Image format (jpeg, png, etc.) or empty string if not found</returns>
    public static string GetImageFormat(string base64String)
    {
        if (string.IsNullOrEmpty(base64String) || !base64String.StartsWith("data:image/"))
            return string.Empty;

        try
        {
            int startIndex = "data:image/".Length;
            int endIndex = base64String.IndexOf(';', startIndex);
            if (endIndex > startIndex)
            {
                return base64String.Substring(startIndex, endIndex - startIndex);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Base64ImageUtility] Error extracting image format: {ex.Message}");
        }

        return string.Empty;
    }
}
