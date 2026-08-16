using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

public class PDFLoader : MonoBehaviour
{
    public static IEnumerator LoadPDFPages(byte[] pdfBytes, System.Action<Texture2D[]> onComplete)
    {
#if UNITY_EDITOR
        // באדיטור - נשתמש ב-external converter
        yield return LoadPDFWindows(pdfBytes, onComplete);
#elif UNITY_ANDROID
        // ב-Quest/Android - נשתמש ב-Android API
        yield return LoadPDFAndroid(pdfBytes, onComplete);
#else
        // פלטפורמות אחרות - placeholder
        onComplete?.Invoke(CreatePlaceholderPages(5));
        yield return null;
#endif
    }

#if UNITY_EDITOR
    static IEnumerator LoadPDFWindows(byte[] pdfBytes, System.Action<Texture2D[]> onComplete)
    {
        Debug.LogWarning("PDF conversion in Editor - Creating white placeholder pages");
        
        // יצירת עמודים לבנים עם טקסט (במקום אפור)
        Texture2D[] placeholderPages = new Texture2D[3];
        
        for(int i = 0; i < 3; i++)
        {
            Texture2D tex = new Texture2D(512, 720, TextureFormat.RGB24, false);
            Color[] pixels = new Color[512 * 720];
            
            // רקע לבן
            for(int p = 0; p < pixels.Length; p++)
                pixels[p] = Color.white;
            
            // "כתיבת" טקסט פשוט (קו שחור למעלה ולמטה)
            for(int x = 0; x < 512; x++)
            {
                // קו עליון
                for(int y = 680; y < 700; y++)
                    pixels[y * 512 + x] = Color.black;
                
                // קו תחתון
                for(int y = 20; y < 40; y++)
                    pixels[y * 512 + x] = Color.black;
            }
            
            tex.SetPixels(pixels);
            tex.Apply();
            
            placeholderPages[i] = tex;
        }
        
        onComplete?.Invoke(placeholderPages);
        yield return null;
    }
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
    static IEnumerator LoadPDFAndroid(byte[] pdfBytes, System.Action<Texture2D[]> onComplete)
    {
        List<Texture2D> pages = new List<Texture2D>();
        bool success = false;
        
        string tempPath = Path.Combine(Application.temporaryCachePath, "temp_" + System.DateTime.Now.Ticks + ".pdf");
        File.WriteAllBytes(tempPath, pdfBytes);
        
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        
        // יצירת אובייקטים מחוץ ל-try
        AndroidJavaObject file = new AndroidJavaObject("java.io.File", tempPath);
        AndroidJavaClass parcelFDClass = new AndroidJavaClass("android.os.ParcelFileDescriptor");
        int modeReadOnly = parcelFDClass.GetStatic<int>("MODE_READ_ONLY");
        
        AndroidJavaObject parcelFileDescriptor = null;
        AndroidJavaObject pdfRenderer = null;
        
        // ננסה לפתוח את הקובץ
        try
        {
            parcelFileDescriptor = parcelFDClass.CallStatic<AndroidJavaObject>("open", file, modeReadOnly);
            pdfRenderer = new AndroidJavaObject("android.graphics.pdf.PdfRenderer", parcelFileDescriptor);
            success = true;
        }
        catch(System.Exception e)
        {
            Debug.LogError("Failed to open PDF: " + e.Message);
            success = false;
        }
        
        // אם הצלחנו, נמשיך עם הרינדור
        if(success && pdfRenderer != null)
        {
            int pageCount = pdfRenderer.Call<int>("getPageCount");
            Debug.Log($"PDF has {pageCount} pages");
            
            for(int i = 0; i < pageCount && i < 50; i++)
            {
                AndroidJavaObject page = null;
                AndroidJavaObject bitmap = null;
                
                try
                {
                    page = pdfRenderer.Call<AndroidJavaObject>("openPage", i);
                    int pageWidth = page.Call<int>("getWidth");
                    int pageHeight = page.Call<int>("getHeight");
                    
                    int width = pageWidth * 2;
                    int height = pageHeight * 2;
                    
                    AndroidJavaClass bitmapConfig = new AndroidJavaClass("android.graphics.Bitmap$Config");
                    AndroidJavaObject argb8888 = bitmapConfig.GetStatic<AndroidJavaObject>("ARGB_8888");
                    AndroidJavaClass bitmapClass = new AndroidJavaClass("android.graphics.Bitmap");
                    bitmap = bitmapClass.CallStatic<AndroidJavaObject>("createBitmap", width, height, argb8888);
                    
                    page.Call("render", bitmap, null, null, 1);
                    
                    Texture2D texture = BitmapToTexture2D(bitmap, width, height);
                    pages.Add(texture);
                    
                    page.Call("close");
                }
                catch(System.Exception e)
                {
                    Debug.LogError($"Failed to render page {i}: " + e.Message);
                }
                
                yield return null; // עכשיו זה בחוץ מה-try
            }
            
            // סגירת המשאבים
            try
            {
                if(pdfRenderer != null) pdfRenderer.Call("close");
                if(parcelFileDescriptor != null) parcelFileDescriptor.Call("close");
            }
            catch(System.Exception e)
            {
                Debug.LogError("Error closing PDF: " + e.Message);
            }
        }
        
        // ניקוי
        if(File.Exists(tempPath))
        {
            try { File.Delete(tempPath); }
            catch { }
        }
        
        // החזרת תוצאה
        if(pages.Count == 0)
        {
            Debug.LogWarning("No pages rendered, returning placeholders");
            pages = new List<Texture2D>(CreatePlaceholderPages(3));
        }
        
        onComplete?.Invoke(pages.ToArray());
    }
    
    static Texture2D BitmapToTexture2D(AndroidJavaObject bitmap, int width, int height)
    {
        AndroidJavaClass byteBufferClass = new AndroidJavaClass("java.nio.ByteBuffer");
        int capacity = width * height * 4;
        AndroidJavaObject byteBuffer = byteBufferClass.CallStatic<AndroidJavaObject>("allocate", capacity);
        
        bitmap.Call("copyPixelsToBuffer", byteBuffer);
        byteBuffer.Call("rewind");
        
        // המרה נכונה
        AndroidJavaObject javaByteArray = byteBuffer.Call<AndroidJavaObject>("array");
        System.IntPtr arrayPtr = javaByteArray.GetRawObject();
        byte[] pixels = AndroidJNI.FromByteArray(arrayPtr);
        
        // יצירת Texture
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        
        // המרה מ-BGRA ל-RGBA
        Color32[] colors = new Color32[width * height];
        for(int i = 0; i < colors.Length; i++)
        {
            int idx = i * 4;
            colors[i] = new Color32(
                pixels[idx + 2], // R
                pixels[idx + 1], // G
                pixels[idx],     // B
                pixels[idx + 3]  // A
            );
        }
        
        texture.SetPixels32(colors);
        texture.Apply();
        
        return texture;
    }
#endif

    static Texture2D[] CreatePlaceholderPages(int count)
    {
        Texture2D[] pages = new Texture2D[count];

        for (int i = 0; i < count; i++)
        {
            Texture2D tex = new Texture2D(512, 720, TextureFormat.RGB24, false);
            Color[] pixels = new Color[512 * 720];

            // רקע לבן
            for (int p = 0; p < pixels.Length; p++)
                pixels[p] = Color.white;

            tex.SetPixels(pixels);
            tex.Apply();

            pages[i] = tex;
        }

        return pages;
    }
}